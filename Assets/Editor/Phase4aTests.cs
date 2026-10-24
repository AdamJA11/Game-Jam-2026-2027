using System;

public static class Phase4aTests
{
    public static int Run()
    {
        int checks = 0;
        Action<bool, string> assert = (value, message) => { checks++; if (!value) throw new Exception(message); };
        var speed = new MovementSpeedModifier();
        assert(speed.Multiplier == 1f && speed.EffectiveSpeed(4) == 4, "Normal initial speed.");
        for (int i = 0; i < 500; i++) speed.Set(1.35f);
        assert(Math.Abs(speed.EffectiveSpeed(4) - 5.4f) < .00001f, "Repeated calls cannot compound.");
        assert(Math.Abs(speed.EffectiveSpeed(6) - 8.1f) < .00001f, "Multiplier respects authored base speed.");
        speed.Set(1);
        assert(speed.EffectiveSpeed(4) == 4, "Explicit reset restores base speed.");
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { speed.Set(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            assert(rejected && speed.Multiplier == 1f, "Invalid modifier rejected without changing speed.");
        }
        for (int alive = 0; alive <= 1; alive++) for (int moving = 0; moving <= 1; moving++) for (int apex = 0; apex <= 1; apex++)
        {
            float multiplier = MovementSpeedModifier.ForCombatState(alive == 1, moving == 1, apex == 1, 1.35f);
            assert(multiplier == (alive == 1 && moving == 1 && apex == 1 ? 1.35f : 1f), "Only living movement-enabled Apex gets boost.");
        }
        for (int hunter = 0; hunter < 3; hunter++)
        {
            int prey = (hunter + 1) % 3, escapee = (hunter + 2) % 3;
            var rules = new HuntRules();
            string initial = HuntPresentation.Status(rules, hunter);
            assert(initial == "HUNT " + HuntPresentation.Identity(prey) + "\nAVOID " + HuntPresentation.Identity(escapee), "Original hunt/avoid identities.");
            assert(initial.Contains(HuntPresentation.ColorName(prey)) && initial.Contains(HuntPresentation.ColorName(escapee)), "Colors named explicitly.");
            string[] colors = { "112233", "445566", "778899" };
            assert(HuntPresentation.InitialLabel(hunter, colors).Contains("<color=#" + colors[prey] + ">"), "Tint comes from actual identity renderer.");
            rules.Begin(); rules.ResolveContacts(HuntRules.ContactBit(hunter, prey));
            assert(HuntPresentation.Status(rules, hunter).Contains("NEXT HUNT " + HuntPresentation.Identity(escapee)), "Transforming Apex knows its new prey.");
            assert(HuntPresentation.Status(rules, escapee).Contains("ESCAPE " + HuntPresentation.Identity(hunter)), "Escapee sees dangerous identity/color.");
            assert(HuntPresentation.Status(rules, prey).Contains("CAUGHT BY " + HuntPresentation.Identity(hunter)), "Elimination identifies killer/color.");
            speed.Set(MovementSpeedModifier.ForCombatState(true, false, true, 1.35f));
            assert(speed.Multiplier == 1f, "Transformation has no movement boost.");
            rules.AdvanceTransformation(1f);
            speed.Set(MovementSpeedModifier.ForCombatState(true, true, true, 1.35f));
            assert(Math.Abs(speed.EffectiveSpeed(4) - 5.4f) < .00001f, "Boost begins after transformation.");
            assert(HuntPresentation.Status(rules, hunter).Contains("HUNT " + HuntPresentation.Identity(escapee))
                && HuntPresentation.Status(rules, hunter).EndsWith("AVOID: NONE"), "Apex has correct final prey and no hunter.");
            assert(HuntPresentation.Status(rules, escapee) == "AVOID APEX " + HuntPresentation.Identity(hunter) + "\nHUNT: NONE", "Former predator no longer has a prey.");
            rules.ResolveContacts(HuntRules.ContactBit(hunter, escapee));
            speed.Set(MovementSpeedModifier.ForCombatState(true, false, true, 1.35f));
            assert(speed.Multiplier == 1f, "Round end removes modifier.");
            assert(HuntPresentation.Status(rules, hunter).Contains("YOU WIN")
                && HuntPresentation.Status(rules, escapee).Contains(HuntPresentation.Identity(hunter)), "Result identifies winner/color.");
        }
        return checks;
    }
}
