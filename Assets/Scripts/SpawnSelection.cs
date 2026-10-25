using System;
using System.Collections.Generic;

public static class SpawnSelection
{
    public static List<int[]> Groups(int count, Func<int, int, bool> compatible)
    {
        var groups = new List<int[]>();
        for (int a = 0; a < count; a++) for (int b = a + 1; b < count; b++) for (int c = b + 1; c < count; c++)
            if (compatible(a, b) && compatible(a, c) && compatible(b, c)) groups.Add(new[] { a, b, c });
        return groups;
    }

    public static int[] Choose(List<int[]> groups, Func<int, int> draw)
    {
        if (groups == null || groups.Count == 0) throw new InvalidOperationException("No valid three-point spawn group.");
        int[] result = (int[])groups[Draw(draw, groups.Count)].Clone();
        for (int i = 2; i > 0; i--)
        {
            int j = Draw(draw, i + 1), old = result[i]; result[i] = result[j]; result[j] = old;
        }
        return result;
    }

    private static int Draw(Func<int, int> draw, int maximum)
    {
        if (draw == null) throw new ArgumentNullException(nameof(draw));
        int value = draw(maximum);
        if (value < 0 || value >= maximum) throw new ArgumentOutOfRangeException(nameof(draw));
        return value;
    }
}
