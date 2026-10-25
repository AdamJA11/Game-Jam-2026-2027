using System;

public static class ApexTransformationMath
{
    public static float Progress(float remaining)
        => Math.Max(0, Math.Min(1, 1 - remaining / HuntRules.TransformationDuration));

    public static int CountdownTenths(float remaining)
        => Math.Max(0, (int)Math.Ceiling(remaining * 10 - .00001f));

    public static float RequiredRadius(float x, float y, float xMin, float yMin, float xMax, float yMax, float padding)
    {
        float dx = Math.Max(Math.Abs(x - xMin), Math.Abs(x - xMax));
        float dy = Math.Max(Math.Abs(y - yMin), Math.Abs(y - yMax));
        return (float)Math.Sqrt(dx * dx + dy * dy) + padding;
    }
}

public sealed class ApexWarningTimeline
{
    private bool started;
    private bool cancelled;
    private double startTime;

    public bool TryStart(HuntRules.Phase phase, double now)
    {
        if (started || cancelled || phase != HuntRules.Phase.Transforming) return false;
        started = true; startTime = now;
        return true;
    }

    public float Age(double now) => (float)Math.Max(0, now - startTime);
    public bool Active(HuntRules.Phase phase, double now, float duration)
        => started && !cancelled && (phase == HuntRules.Phase.Transforming || phase == HuntRules.Phase.FinalChase)
            && Age(now) < duration;
    public void Cancel() { if (started) cancelled = true; }
}
