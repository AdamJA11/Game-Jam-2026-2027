// One-shot event detection; independent of visual setup, frame count, or audio clip length.
public sealed class HuntAudioEvents
{
    private int previousAliveMask = 7;
    private bool transformationPlayed;

    public void Observe(HuntRules rules, out int kills, out bool transformationStarted)
    {
        int eliminated = previousAliveMask & ~rules.AliveMask;
        previousAliveMask = rules.AliveMask;
        kills = 0;
        for (int i = 0; i < 3; i++) if ((eliminated & (1 << i)) != 0) kills++;
        transformationStarted = !transformationPlayed && rules.ApexId >= 0 && rules.State == HuntRules.Phase.Transforming;
        if (transformationStarted) transformationPlayed = true;
    }
}
