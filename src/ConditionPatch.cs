using System;
using System.Globalization;
using System.Reflection;
using HarmonyLib;

namespace CarAttendant
{
    // The one hook: a prefix on the reputation tracker's private PassengerStopServed(string stopIdentifier, int offset,
    // float carCondition), which adds abs(offset) riders to the day's passenger total and abs(offset) * carCondition to
    // its condition sum. The prefix rewrites carCondition by ref through the ramp (Ramp.cs) before the game
    // adds it; the method itself, and the event that called it, run as always. Nothing else is touched: not the car,
    // its condition, repairs or fares.
    //
    // The tracker only subscribes on the host, so on a client this never runs. Found by name and exact signature, so
    // this file names no game type: on a build without the method the mod logs once and the game runs as it would.
    internal static class ConditionPatch
    {
        private const string TypeName = "Game.Reputation.ReputationTracker";
        private const string MethodName = "PassengerStopServed";
        private static readonly Type[] Signature = { typeof(string), typeof(int), typeof(float) };

        // The method to patch, or null with the reason.
        internal static MethodInfo Target(out string why)
        {
            why = null;
            Type t = AccessTools.TypeByName(TypeName);
            if (t == null) { why = "the type " + TypeName + " isn't in this game version"; return null; }
            MethodInfo m = AccessTools.Method(t, MethodName, Signature);
            if (m == null) { why = TypeName + "." + MethodName + "(string, int, float) isn't in this game version"; return null; }
            ParameterInfo[] p = m.GetParameters();
            if (p.Length != 3 || p[2].Name != "carCondition" || p[1].Name != "offset" || p[0].Name != "stopIdentifier")
            {
                why = TypeName + "." + MethodName + " has different parameter names on this game version";
                return null;
            }
            return m;
        }

        internal static bool Apply(Harmony harmony, out string why)
        {
            MethodInfo target = Target(out why);
            if (target == null) return false;
            MethodInfo prefix = typeof(ConditionPatch).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
            return true;
        }

        private static void Prefix(string stopIdentifier, int offset, ref float carCondition)
        {
            if (!Main.Enabled) return;
            float before = carCondition;
            try { carCondition = Ramp.Counted(before, Main.Threshold); }
            catch (Exception e)
            {
                carCondition = before;
                Main.WarnOnce("prefix", Ids.Name + ": the rating hook failed, so stops count as the game rates them: " + PathClean.CleanText(e.Message));
                return;
            }
            if (!Main.LogStops) return;
            try
            {
                Main.Log?.Info(Ids.Name + ": " + stopIdentifier + ", net " + Main.Num(offset) + " riders, coach at "
                               + Pct(before) + " counted as " + Pct(carCondition));
            }
            catch (Exception) { }
        }

        private static string Pct(float v) => float.IsNaN(v) ? "NaN" : (v * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
    }
}
