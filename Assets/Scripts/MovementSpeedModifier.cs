using System;

// An absolute modifier: repeated combat-state updates never compound the base speed.
public sealed class MovementSpeedModifier
{
    public float Multiplier { get; private set; } = 1f;

    public void Set(float multiplier)
    {
        if (multiplier <= 0 || float.IsNaN(multiplier) || float.IsInfinity(multiplier))
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Speed multiplier must be finite and positive.");
        Multiplier = multiplier;
    }

    public float EffectiveSpeed(float baseSpeed) => baseSpeed * Multiplier;
    public static float ForCombatState(bool alive, bool allowMovement, bool apex, float apexMultiplier)
        => alive && allowMovement && apex ? apexMultiplier : 1f;
}
