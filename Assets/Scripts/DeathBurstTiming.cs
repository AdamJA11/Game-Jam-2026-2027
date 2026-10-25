using System;

// Shared animation math, independent of frame rate and Unity callbacks.
public static class DeathBurstTiming
{
    public const int PieceCount = 9;
    public static bool Complete(float age, float lifetime) => age >= lifetime;
    public static float Travel(float age) => (float)((1.0 - Math.Exp(-3.0 * Math.Max(0, age))) / 3.0);
    public static float Opacity(float age, float lifetime, float fadeDuration)
    {
        if (Complete(age, lifetime)) return 0;
        if (fadeDuration <= 0) return 1;
        return Math.Min(1, Math.Max(0, (lifetime - age) / fadeDuration));
    }
}
