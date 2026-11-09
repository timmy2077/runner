using System;

// Pure ordering rule: each prefab repeats appearanceCount times, groups advance in order,
// and the final nonempty group loops. No random selection or boss-prefab filtering.
public static class JourneyPrefabPlan
{
    public static bool Resolve(int index, int[] groupSizes, int[] repeats, out int group, out int item)
    {
        group = item = -1;
        if (index < 0 || groupSizes == null || repeats == null || groupSizes.Length != repeats.Length) return false;
        int last = -1;
        for (int i = 0; i < groupSizes.Length; i++) if (groupSizes[i] > 0) last = i;
        if (last < 0) return false;
        long remaining = index;
        for (int i = 0; i <= last; i++)
        {
            if (groupSizes[i] <= 0) continue;
            int repeat = Math.Max(1, repeats[i]);
            long span = (long)groupSizes[i] * repeat;
            if (remaining < span || i == last)
            {
                group = i;
                item = (int)((remaining % span) / repeat);
                return true;
            }
            remaining -= span;
        }
        return false;
    }
}
