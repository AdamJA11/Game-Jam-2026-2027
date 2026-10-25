using System;
using System.Collections.Generic;

public static class Phase3cTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        var groups = SpawnSelection.Groups(6, (a, b) => true);
        check(groups.Count == 20, "Six points have twenty three-point groups.");
        var outcomes = new HashSet<string>();
        for (int g = 0; g < 20; g++) for (int a = 0; a < 3; a++) for (int b = 0; b < 2; b++)
        {
            int call = 0;
            int[] assignment = SpawnSelection.Choose(groups, max => call++ == 0 ? g : call == 2 ? a : b);
            check(assignment[0] != assignment[1] && assignment[1] != assignment[2] && assignment[0] != assignment[2], "Distinct players use distinct points.");
            outcomes.Add(string.Join(",", assignment));
        }
        check(outcomes.Count == 120, "All 120 ordered assignments reachable.");
        check(SpawnSelection.Groups(6, (a, b) => !(a == 0 && b == 1)).Count == 16, "Incompatible pair excludes only its four groups.");
        check(SpawnSelection.Groups(6, (a, b) => false).Count == 0, "No valid triple is detectable.");
        bool rejected = false;
        try { SpawnSelection.Choose(groups, max => max); } catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected, "Invalid random draw rejected before placement.");
        check(MapRotationRules.Next("A", "A", "B") == "B", "A continues to B.");
        check(MapRotationRules.Next("B", "A", "B") == "A", "B continues to A.");
        check(MapRotationRules.Next("menu", "A", "B") == null, "Unknown current map is not guessed.");
        check(MapRotationRules.Next("A", "A", "A") == null, "Duplicate routes rejected.");
        return count;
    }
}
