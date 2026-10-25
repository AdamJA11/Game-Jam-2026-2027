using System;

public static class SpriteFacingTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        check(SpriteFacingRules.Heading(0, 1, 17) == 0, "Native up artwork faces up at zero angle.");
        check(SpriteFacingRules.Heading(1, 0, 17) == -90, "Right input rotates visuals clockwise.");
        check(SpriteFacingRules.Heading(-1, 0, 17) == 90, "Left input rotates visuals counter-clockwise.");
        check(SpriteFacingRules.Heading(0, 0, 17) == 17, "Stopped/blocked input keeps previous facing.");
        check(Math.Abs(SpriteFacingRules.Heading(1, 1, 0) + 45) < .0001f, "Diagonal facing is supported.");
        for (int angle = 0; angle < 360; angle += 45)
        {
            float x = (float)Math.Cos(angle * Math.PI / 180), y = (float)Math.Sin(angle * Math.PI / 180);
            float heading = SpriteFacingRules.Heading(x, y, 0);
            check(Math.Abs(Math.Cos((heading + 90) * Math.PI / 180) - x) < .00001f
                && Math.Abs(Math.Sin((heading + 90) * Math.PI / 180) - y) < .00001f, "All eight inputs preserve movement-facing direction.");
        }
        check(SpriteFacingRules.FitHeight(1, 1, 1) == 1, "Matching sprite heights keep visual scale.");
        check(Math.Abs(SpriteFacingRules.FitHeight(.08f, 1, 1) - .08f) < .000001f, "Different PPU does not make the Apex enormous.");
        check(SpriteFacingRules.FitHeight(2, 1, 1.25f) == 2.5f, "Optional size multiplier is visual-only math.");
        check(SpriteFacingRules.Blend(-1) == 0 && SpriteFacingRules.Blend(2) == 1, "Transition alpha clamps.");
        check(SpriteFacingRules.Blend(.5f) == .5f, "Halfway transform crossfades equally.");
        bool rejected = false;
        try { SpriteFacingRules.FitHeight(1, 0, 1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected, "Invalid sprite size is rejected.");
        RandomItemRules.Rotate(1, 0, 45, out float dx, out float dy);
        check(Math.Abs(SpriteFacingRules.Heading(dx, dy, -90) + 45) < .00001f, "Rotated-input decoy faces its actual intended diagonal.");
        return count;
    }
}
