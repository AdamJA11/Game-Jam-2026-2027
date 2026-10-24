using System;

// Identity names remain fixed; tint may use the actual player renderer's color.
public static class HuntPresentation
{
    public static string ColorName(int id)
    {
        switch (id) { case 0: return "YELLOW"; case 1: return "MAGENTA"; case 2: return "GREEN"; default: throw new ArgumentOutOfRangeException(nameof(id)); }
    }

    public static string DefaultHex(int id)
    {
        switch (id) { case 0: return "FFFF00"; case 1: return "FF40CC"; case 2: return "4DFF4D"; default: throw new ArgumentOutOfRangeException(nameof(id)); }
    }

    public static string Identity(int id, string[] colors = null)
    {
        string hex = colors == null ? DefaultHex(id) : colors[id];
        return "<color=#" + hex + ">P" + (id + 1) + " " + ColorName(id) + "</color>";
    }

    public static string InitialLabel(int id, string[] colors = null)
        => "HUNT " + Identity((id + 1) % 3, colors) + "\nAVOID " + Identity((id + 2) % 3, colors);

    public static string Status(HuntRules rules, int viewer, string[] colors = null)
    {
        if (rules.State == HuntRules.Phase.RoundOver)
        {
            if (rules.WinnerId < 0) return "DRAW - NO SURVIVORS";
            if (rules.WinnerId == viewer) return "YOU WIN\n" + Identity(viewer, colors);
            return "ROUND OVER - " + Identity(rules.WinnerId, colors) + " WINS";
        }
        if (!rules.IsAlive(viewer)) return "ELIMINATED\nCAUGHT BY " + Identity(rules.KilledBy(viewer), colors);
        if (rules.State == HuntRules.Phase.Transforming)
            return viewer == rules.ApexId
                ? "TRANSFORMING " + rules.TransformRemaining.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s\nNEXT HUNT " + Identity(RemainingOpponent(rules, viewer), colors)
                : "ESCAPE " + Identity(rules.ApexId, colors) + "\nTRANSFORMING - GET AWAY!";
        if (rules.State == HuntRules.Phase.FinalChase)
            return viewer == rules.ApexId
                ? "APEX - HUNT " + Identity(RemainingOpponent(rules, viewer), colors) + "\nAVOID: NONE"
                : "AVOID APEX " + Identity(rules.ApexId, colors) + "\nHUNT: NONE";
        return InitialLabel(viewer, colors);
    }

    private static int RemainingOpponent(HuntRules rules, int id)
    { for (int i = 0; i < 3; i++) if (i != id && rules.IsAlive(i)) return i; throw new InvalidOperationException("No living opponent."); }
}
