using System;

namespace CarAttendant
{
    // The rating ramp, with no game types so tests/RampCheck can compile it alone.
    //
    // A coach at or above the threshold counts as fully fit; below it the score falls off in proportion:
    //   counted = min(1, condition / threshold), held inside 0..1.
    // With the 80% default: 80% and up count as 100%, 79% as about 99%, 40% as 50%, 0% as 0%.
    // A threshold of 100% (or the mod switched off) passes the condition through untouched, exactly the game's own
    // rating; so does NaN, whatever the threshold.
    internal static class Ramp
    {
        internal const int MinPct = 50, MaxPct = 100, DefaultPct = 80;

        // A threshold from settings.json, held inside MinPct..MaxPct, so a hand edit of 0 or a negative never reaches
        // the division.
        internal static int ClampPct(int pct) => Math.Max(MinPct, Math.Min(MaxPct, pct));

        internal static float Threshold(int pct) => ClampPct(pct) / 100f;

        // threshold: a value from Threshold(), so 0.5..1. Anything at or above 1 (or not a number) is vanilla.
        internal static float Counted(float condition, float threshold)
        {
            if (float.IsNaN(condition)) return condition;
            if (!(threshold > 0f) || threshold >= 1f) return condition;
            float c = condition / threshold;
            if (c > 1f) return 1f;
            if (c < 0f) return 0f;
            return c;
        }
    }
}
