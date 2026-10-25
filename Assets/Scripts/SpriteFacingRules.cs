using System;

public static class SpriteFacingRules
{
    // Native art points up. Zero input preserves the last world-facing angle.
    public static float Heading(float x, float y, float previous)
        => x * x + y * y < .000001f ? previous : (float)(Math.Atan2(y, x) * 180 / Math.PI - 90);

    public static float FitHeight(float normalHeight, float apexHeight, float multiplier)
    {
        if (normalHeight <= 0 || apexHeight <= 0 || multiplier <= 0
            || float.IsNaN(normalHeight) || float.IsInfinity(normalHeight) || float.IsNaN(apexHeight) || float.IsInfinity(apexHeight)
            || float.IsNaN(multiplier) || float.IsInfinity(multiplier)) throw new ArgumentOutOfRangeException("visual size");
        return normalHeight / apexHeight * multiplier;
    }

    public static float Blend(float progress) => Math.Max(0, Math.Min(1, progress));
}
