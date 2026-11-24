using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.Modding;

namespace CarAttendant
{
    internal sealed class Settings
    {
        // Off: every stop counts as the game rates it.
        public bool enabled = true;
        // A coach at this condition (percent) or better counts as fully fit; 100 is the game's own rating. Held 50..100.
        public int thresholdPct = Ramp.DefaultPct;
        // Write each passenger stop the hook rates to Player.log (host only).
        public bool logStops = false;

        // One bad field keeps the valid ones: each is read alone and a field that can't be read stays at its default.
        // Newtonsoft rounds 80.5 into an int and accepts "80" for one; only a value it refuses is dropped here.
        internal static Settings From(JToken token, Action<string> warn)
        {
            var s = new Settings();
            JObject o = token as JObject;
            if (o == null)
            {
                if (token != null && token.Type != JTokenType.Null) warn("the settings aren't a list of fields, using the defaults");
                return s;
            }
            Read<bool>(o, "enabled", warn, v => s.enabled = v);
            Read<int>(o, "thresholdPct", warn, v => s.thresholdPct = v);
            Read<bool>(o, "logStops", warn, v => s.logStops = v);
            return s;
        }

        private static void Read<T>(JObject o, string key, Action<string> warn, Action<T> set)
        {
            JToken t;
            if (!o.TryGetValue(key, StringComparison.Ordinal, out t) || t.Type == JTokenType.Null) return;
            try { set(t.ToObject<T>()); }
            catch (Exception) { warn(key + " can't be read, using its default"); }
        }
    }

    // Railroader 0.6 entry point. One prefix (ConditionPatch.cs) on the host's reputation tracker; the settings section
    // (SettingsSection.cs) stays off this class, behind NoInlining methods (SettingsUi). Nothing here names a game type,
    // so a game update that moves the tracker shows up as one warning, never a failed mod load.
    public sealed class Main : IModEntryPoint
    {
        internal static IModLogger Log;
        private static readonly HashSet<string> Logged = new HashSet<string>();
        private Harmony _harmony;

        // The settings in use, read by the prefix on every stop served: plain fields, set at load and after a save.
        internal static bool Enabled = true;
        internal static int ThresholdPct = Ramp.DefaultPct;
        internal static float Threshold = Ramp.Threshold(Ramp.DefaultPct);
        internal static bool LogStops;
        internal static bool Hooked;

        internal static void WarnOnce(string key, string text) { if (Logged.Add(key)) Log?.Warning(text); }

        public void OnModLoad(IModContext context)
        {
            // Assemblies never unload and statics survive a disable/enable: reset everything here.
            Log = context.Log;
            Logged.Clear();
            Hooked = false;
            Api.Reset();
            string settingsPath = Path.Combine(context.ModDirectory, "settings.json");
            Use(LoadSettings(settingsPath));
            _harmony = new Harmony(Ids.HarmonyId);
            // A load that follows a load with no unload (the loader skips the unload after a failed load) must not
            // stack a second prefix.
            try { _harmony.UnpatchAll(Ids.HarmonyId); } catch (Exception) { }
            try
            {
                Hooked = ConditionPatch.Apply(_harmony, out string why);
                if (!Hooked) Log.Warning(Ids.Name + " can't find the rating it changes, so it does nothing on this game version (" + why + ")");
            }
            catch (Exception e)
            {
                Hooked = false;
                Log.Warning(Ids.Name + " couldn't hook the riders' rating on this game version, so it does nothing: " + PathClean.CleanText(e.Message));
            }
            if (Hooked) Log.Info(Ids.Name + " loaded: " + Describe());
            // The feed goes on the shared list (ModFeeds.cs) so any mod can find and read it. A refusal (another mod holds
            // the id) is one log line.
            try { if (!PublishFeed()) Log.Warning(Ids.Name + ": another mod already publishes the feed id \"" + Api.Feed + "\", so this one isn't listed"); }
            catch (Exception) { }
            try { SettingsUi.Start(_harmony, settingsPath); }
            catch (Exception e)
            {
                StopSettings();
                Log.Warning(Ids.Name + ": the settings aren't in Preferences > Mods on this game version (the mod works without them): " + PathClean.CleanText(e.Message));
            }
        }

        public void OnModUnload()
        {
            try { ModFeeds.Withdraw(Api.Feed); } catch (Exception) { }
            Hooked = false;
            StopSettings();
            try { _harmony?.UnpatchAll(Ids.HarmonyId); } catch (Exception) { }
            _harmony = null;
        }

        // The feed's entry on the shared list: Api.Read, with a line per top-level key (release/FEEDS.md has the full schema).
        // Names only BCL types, ModFeeds and Api (the class-load rule).
        internal static bool PublishFeed()
        {
            var fields = new Dictionary<string, string>
            {
                { "enabled", "the Kinder rating setting in use: false means every stop counts as the game rates it" },
                { "thresholdPct", "the Fully fit at setting in use, percent, 50 to 100: a coach at this condition or better counts as fully fit" },
                { "hooked", "the rating hook went in when the mod loaded; false means the mod does nothing" },
            };
            return ModFeeds.Publish(Api.Feed, Api.Read, Ids.Name, Ids.Name, AssemblyVersion(), Api.Version, fields);
        }

        private static string AssemblyVersion()
        {
            System.Version v = typeof(Main).Assembly.GetName().Version;
            return v == null ? "" : v.Major + "." + v.Minor + "." + v.Build;
        }

        // Numbers in log lines read the same in every culture (no locale minus sign or digit grouping).
        internal static string Num(int n) => n.ToString(CultureInfo.InvariantCulture);

        private static string Describe() =>
            !Enabled ? "switched off in its settings, so riders rate coaches as the game does"
            : ThresholdPct >= Ramp.MaxPct ? "threshold 100%, so riders rate coaches as the game does"
            : "riders count a coach at " + Num(ThresholdPct) + "% condition or better as fully fit (host only)"
              + (LogStops ? ", logging each stop" : "");

        // The settings in use, with the threshold held inside its range.
        private static void Use(Settings s)
        {
            s = s ?? new Settings();
            int pct = Ramp.ClampPct(s.thresholdPct);
            if (pct != s.thresholdPct)
                Log?.Warning(Ids.Name + ": thresholdPct " + Num(s.thresholdPct) + " is outside " + Num(Ramp.MinPct) + " to " + Num(Ramp.MaxPct) + ", using " + Num(pct));
            ThresholdPct = pct;
            Threshold = Ramp.Threshold(pct);
            LogStops = s.logStops;
            Enabled = s.enabled;
        }

        // After a save in Preferences > Mods: the same read as at load, from the file just written.
        internal static void Applied(JObject saved)
        {
            Use(Settings.From(saved, WarnSettings));
            Log?.Info(Ids.Name + ": " + Describe() + ", from the next stop served");
        }

        private static void WarnSettings(string m) => Log?.Warning(Ids.Name + ": settings.json: " + m);

        private static Settings LoadSettings(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string text = File.ReadAllText(path);
                    return string.IsNullOrWhiteSpace(text) ? new Settings() : Settings.From(JToken.Parse(text), WarnSettings);
                }
            }
            catch (Exception e) { Log?.Warning(Ids.Name + ": couldn't read settings.json, using the defaults (on, 80%): " + PathClean.CleanText(e.Message)); return new Settings(); }
            // No file yet: write one with the defaults, so players can find it. Never in the zip, so an update can't
            // overwrite someone's choice; an existing file (even an unreadable one) is never touched.
            Settings d = new Settings();
            try
            {
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(d, Formatting.Indented));
                File.Move(tmp, path);
                Log?.Info(Ids.Name + ": wrote settings.json with the defaults (on, 80%)");
            }
            catch (Exception e)
            {
                Log?.Warning(Ids.Name + ": couldn't write a default settings.json (the defaults apply anyway): " + PathClean.CleanText(e.Message));
                try { File.Delete(path + ".tmp"); } catch (Exception) { }
            }
            return d;
        }

        private static void StopSettings()
        {
            try { SettingsUi.Stop(); } catch (Exception) { }
        }
    }

    // Everything typed by SettingsSection.cs (and so by the game's UI) lives here, off Main, behind NoInlining methods
    // called inside a try, so a game update that breaks the section can't take the hook with it (the house pattern).
    internal static class SettingsUi
    {
        private static object _section;   // a SettingsSection

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Start(Harmony harmony, string settingsPath)
        {
            Stop();
            var fields = new[]
            {
                SettingField.Bool("enabled", "Kinder rating", true, true,
                    "Off: riders rate coaches as the game does."),
                SettingField.Int("thresholdPct", "Fully fit at (%)", Ramp.DefaultPct, Ramp.MinPct, Ramp.MaxPct, 5, true,
                    "A coach at this condition or better counts as 100%."),
                SettingField.Bool("logStops", "Log each stop", false, true,
                    "Write a Player.log line for each stop rated."),
            };
            _section = new SettingsSection(Ids.Name, settingsPath, fields, Main.Applied, m => Main.Log?.Warning(Ids.Name + ": " + m),
                "The host's settings count.");
            PreferencesModsTab.Add(harmony, b => (_section as SettingsSection)?.Build(b, true), m => Main.Log?.Warning(Ids.Name + ": " + m));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Stop()
        {
            PreferencesModsTab.Remove();
            _section = null;
        }
    }
}
