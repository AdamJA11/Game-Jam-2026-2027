// Continue owns the story gate separately from the existing scene-load gate.
public sealed class EscapeStoryState
{
    public bool Playing { get; private set; }
    public bool Completed { get; private set; }
    public int Winner { get; private set; } = -1;
    public int Apex { get; private set; } = -1;

    public bool CanBegin(HuntRules.Result outcome, int winner, int apex)
        => !Playing && !Completed && outcome == HuntRules.Result.EscapedThroughDoor
            && winner >= 0 && winner < 3 && apex >= 0 && apex < 3 && winner != apex;

    public bool TryBegin(HuntRules.Result outcome, int winner, int apex)
    {
        if (!CanBegin(outcome, winner, apex)) return false;
        Winner = winner; Apex = apex; Playing = true;
        return true;
    }

    public bool Finish()
    {
        if (!Playing) return false;
        Playing = false; Completed = true;
        return true;
    }

    public static float Segment(float elapsed, float start, float end)
        => elapsed <= start ? 0 : elapsed >= end ? 1 : (elapsed - start) / (end - start);
}
