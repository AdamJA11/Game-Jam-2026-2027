// A disabled presenter can resume using the original deadline. A fresh scene gets a fresh instance.
public sealed class RoundEndReveal
{
    private bool started;
    private bool revealed;
    private double deadline;

    public bool Observe(bool roundOver, double now, float delay)
    {
        if (!roundOver || revealed) return false;
        if (!started) { started = true; deadline = now + delay; }
        if (now < deadline) return false;
        revealed = true;
        return true;
    }
}
