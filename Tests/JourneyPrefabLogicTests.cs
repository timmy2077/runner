using System;

public static class JourneyPrefabLogicTests
{
    public static int Run()
    {
        int assertions = 0;
        Action<bool, string> check = (condition, message) =>
        {
            assertions++;
            if (!condition) throw new Exception("Prefab order: " + message);
        };
        int group, item;
        int[] groups = { 2, 1, 2 };
        int[] repeats = { 2, 3, 1 };
        int[] expectedGroups = { 0, 0, 0, 0, 1, 1, 1, 2, 2, 2, 2 };
        int[] expectedItems = { 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 1 };
        for (int i = 0; i < expectedGroups.Length; i++)
        {
            check(JourneyPrefabPlan.Resolve(i, groups, repeats, out group, out item), "legal catalogue did not resolve");
            check(group == expectedGroups[i] && item == expectedItems[i], "authored order / repeat count changed at " + i);
        }
        check(!JourneyPrefabPlan.Resolve(-1, groups, repeats, out group, out item), "negative index accepted");
        check(!JourneyPrefabPlan.Resolve(0, null, repeats, out group, out item), "null catalogue accepted");
        check(!JourneyPrefabPlan.Resolve(0, new[] { 0, 0 }, new[] { 1, 1 }, out group, out item), "empty catalogue accepted");
        check(!JourneyPrefabPlan.Resolve(0, new[] { 1 }, new[] { 1, 2 }, out group, out item), "mismatched configuration accepted");
        check(JourneyPrefabPlan.Resolve(8, new[] { 0, 1, 0 }, new[] { 1, 0, 1 }, out group, out item)
            && group == 1 && item == 0, "empty trailing groups or zero repeats broke final loop");
        check(JourneyPrefabPlan.Resolve(int.MaxValue, new[] { int.MaxValue, 1 }, new[] { int.MaxValue, 1 }, out group, out item)
            && group == 0 && item == 1, "large authored counts overflowed");
        for (int i = 0; i < 10; i++)
            check(JourneyPrefabPlan.Resolve(i, new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, out group, out item)
                && group == Math.Min(i, 2) && item == 0, "original three-group scene changed order");
        return assertions;
    }
}
