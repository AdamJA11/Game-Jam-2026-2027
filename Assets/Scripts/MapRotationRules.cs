public static class MapRotationRules
{
    public static string Next(string current, string first, string second)
    {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second) || first == second) return null;
        return current == first ? second : current == second ? first : null;
    }
}
