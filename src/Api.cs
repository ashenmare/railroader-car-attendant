using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CarAttendant
{
    // Car Attendant's public feed, in the ashenmare feed shape (release/FEEDS.md, shipped in the zip): other mods read
    // it by reflection, so neither depends on the other.
    //  - Version (const int): the schema version. Within a version fields are only added; a rename, retype or removal
    //    raises it.
    //  - Feed (const string): the stable feed id, "car-attendant".
    //  - Read(): { "api": 1, "feed": "car-attendant", "enabled": bool, "thresholdPct": int, "hooked": bool }.
    //    enabled and thresholdPct are the settings in use; hooked says the rating hook is in. Plain BCL data only, a
    //    new dictionary on every call. Never throws: on a failure it answers { "api": 1, "feed": "car-attendant",
    //    "error": "..." }. Read only, and cheap: it reads the mod's own settings, never game state.
    //
    // Read names only BCL types, PathClean (BCL only) and this class. The settings live on Main, whose fields name
    // the mod loader's and Harmony's types; on Mono, compiling a method that names Main loads Main's field types, and a
    // failure there throws at the CALLER of that method, before any try inside it runs. So Read reaches Main only
    // through ReadFeed and LogOnce, NoInlining methods called inside Read's own try (the house class-load rule).
    public static class Api
    {
        public const int Version = 1;
        public const string Feed = Ids.Feed;

        // One log line per load; reset by Main.OnModLoad (statics survive a mod-set change).
        private static bool _logged;

        // Built at load, so the answer when even a new error dictionary can't be built is still a dictionary, never null.
        private static readonly Dictionary<string, object> Fallback =
            new Dictionary<string, object> { { "api", Version }, { "feed", Feed }, { "error", "the feed failed" } };

        internal static void Reset() => _logged = false;

        public static object Read()
        {
            try { return ReadFeed(); }
            catch (Exception e)
            {
                string why;
                try { why = PathClean.CleanText(e.GetType().Name + ": " + e.Message); }
                catch (Exception) { why = "the feed failed"; }
                if (!_logged)
                {
                    _logged = true;
                    try { LogOnce(why); } catch (Exception) { }
                }
                try { return new Dictionary<string, object> { { "api", Version }, { "feed", Feed }, { "error", why } }; }
                catch (Exception) { return Fallback; }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object ReadFeed() => new Dictionary<string, object>
        {
            { "api", Version },
            { "feed", Feed },
            { "enabled", Main.Enabled },
            { "thresholdPct", Main.ThresholdPct },
            { "hooked", Main.Hooked },
        };

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LogOnce(string why) => Main.Log?.Warning(Ids.Name + ": the feed couldn't answer: " + why);
    }
}
