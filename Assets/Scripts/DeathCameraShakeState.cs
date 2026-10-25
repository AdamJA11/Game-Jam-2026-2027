using System;

// Per-camera presentation state. No Unity/global RNG calls or per-sample allocations.
public sealed class DeathCameraShakeState
{
    private bool started, cancelled;
    private double startTime;
    private float duration, amplitude, frequency;
    private int seed;

    public bool TryStart(double now, int cameraSeed, float seconds, float strength, float hertz)
    {
        if (started) return false;
        if (double.IsNaN(now) || double.IsInfinity(now) || !Finite(seconds) || seconds <= 0 || !Finite(strength) || strength < 0
            || !Finite(hertz) || hertz <= 0 || (double)seconds * hertz > int.MaxValue - 2)
            throw new ArgumentOutOfRangeException("shake settings");
        started = true; cancelled = false; startTime = now; seed = cameraSeed;
        duration = seconds; amplitude = strength; frequency = hertz;
        return true;
    }

    public bool Active(double now) => started && !cancelled && amplitude > 0 && now >= startTime && now - startTime < duration;

    public void Sample(double now, out float x, out float y)
    {
        x = y = 0;
        if (!Active(now)) return;
        double age = now - startTime, phase = age * frequency;
        int index = (int)Math.Floor(phase);
        double blend = phase - index; blend = blend * blend * (3 - 2 * blend);
        double a = Angle(seed, index), b = Angle(seed, index + 1);
        double nx = Math.Cos(a) * (1 - blend) + Math.Cos(b) * blend;
        double ny = Math.Sin(a) * (1 - blend) + Math.Sin(b) * blend;
        double length = Math.Sqrt(nx * nx + ny * ny);
        if (length > 1) { nx /= length; ny /= length; }
        double remaining = 1 - age / duration, strength = amplitude * remaining * remaining;
        x = (float)(nx * strength); y = (float)(ny * strength);
    }

    public void Cancel() { cancelled = true; }

    public static float ClampAxis(float center, float minimum, float maximum, float halfView)
        => maximum - minimum <= halfView * 2 ? (minimum + maximum) * .5f : Math.Max(minimum + halfView, Math.Min(maximum - halfView, center));

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static double Angle(int cameraSeed, int index)
    {
        uint hash;
        unchecked
        {
            hash = ((uint)cameraSeed + 1) * 0x9e3779b9u ^ (uint)index * 0x85ebca6bu;
            hash ^= hash >> 16; hash *= 0x7feb352du; hash ^= hash >> 15; hash *= 0x846ca68bu; hash ^= hash >> 16;
        }
        return hash / (double)uint.MaxValue * Math.PI * 2;
    }
}
