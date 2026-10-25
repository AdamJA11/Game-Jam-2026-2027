using System;

public enum KillClawRole { None, Killer, Victim }

public struct KillClawBatch
{
    public int Killers;
    public int Victims;
    public KillClawRole ForPlayer(int id)
        => (Victims & (1 << id)) != 0 ? KillClawRole.Victim : (Killers & (1 << id)) != 0 ? KillClawRole.Killer : KillClawRole.None;

    public int VictimForPlayer(HuntRules rules, int id)
    {
        if (ForPlayer(id) == KillClawRole.Victim) return id;
        // One graphic per camera: use the first victim if a batch contains multiple kills.
        for (int victim = 0; victim < 3; victim++)
            if ((Victims & (1 << victim)) != 0 && rules.KilledBy(victim) == id) return victim;
        return -1;
    }
}

public static class KillClawPositioning
{
    // Camera-local normalized coordinates; intentionally allow positions outside the viewport
    // so the UI mask clips the graphic without moving its center away from the death location.
    public static void Project(float worldX, float worldY, float cameraX, float cameraY,
        float halfHeight, float aspect, out float x, out float y)
    {
        x = .5f + (worldX - cameraX) / (2 * halfHeight * aspect);
        y = .5f + (worldY - cameraY) / (2 * halfHeight);
    }
}

public sealed class KillClawEvents
{
    private int previousAlive = 7;
    public KillClawBatch Observe(HuntRules rules)
    {
        var batch = new KillClawBatch { Victims = previousAlive & ~rules.AliveMask };
        previousAlive = rules.AliveMask;
        for (int id = 0; id < 3; id++) if ((batch.Victims & (1 << id)) != 0)
        {
            int killer = rules.KilledBy(id);
            if (killer >= 0 && killer < 3) batch.Killers |= 1 << killer;
        }
        return batch;
    }
}

public sealed class KillClawAnimation
{
    private double start;
    private float duration;
    public KillClawRole Role { get; private set; }
    public void Start(KillClawRole role, double now, float seconds)
    {
        if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        Role = role; start = now; duration = seconds;
    }
    public bool Active(double now) => Role != KillClawRole.None && now >= start && now - start < duration;
    public float Progress(double now) => duration > 0 ? (float)Math.Max(0, Math.Min(1, (now - start) / duration)) : 1;
    public float Alpha(double now, float maximum)
    {
        if (!Active(now) || maximum <= 0) return 0;
        float progress = Progress(now), envelope;
        if (progress < 1f / 7) envelope = Smooth(progress * 7);
        else if (progress <= 2f / 7) envelope = 1;
        else envelope = 1 - Smooth((progress - 2f / 7) * 7 / 5);
        return Math.Max(0, Math.Min(1, maximum)) * envelope;
    }
    private static float Smooth(float t) => t * t * (3 - 2 * t);
    public void Cancel() { Role = KillClawRole.None; }
}
