using System;

public static class Phase4bTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (value, message) => { count++; if (!value) throw new Exception(message); };
        check(HuntRules.TransformationDuration == 1, "Existing transformation stays one second.");
        check(ApexTransformationMath.Progress(1) == 0, "Bar starts empty.");
        check(ApexTransformationMath.Progress(.5f) == .5f, "Bar uses authoritative remaining time.");
        check(ApexTransformationMath.Progress(0) == 1, "Bar completes with transformation.");
        check(ApexTransformationMath.Progress(-1) == 1 && ApexTransformationMath.Progress(2) == 0, "Progress clamps safely.");
        check(ApexTransformationMath.CountdownTenths(1) == 10, "Initial countdown.");
        check(ApexTransformationMath.CountdownTenths(.98f) == 10, "Same visible tenth can reuse label.");
        check(ApexTransformationMath.CountdownTenths(.9f) == 9, "Countdown changes at a tenth boundary.");
        var timeline = new ApexWarningTimeline();
        check(!timeline.TryStart(HuntRules.Phase.Opening, 0), "Opening cannot start a wave.");
        check(!timeline.TryStart(HuntRules.Phase.TriangleHunt, 0), "Ordinary hunt cannot start a wave.");
        check(timeline.TryStart(HuntRules.Phase.Transforming, 10), "Transformation starts one wave.");
        check(!timeline.TryStart(HuntRules.Phase.Transforming, 10.5), "Repeated state does not restart wave.");
        check(timeline.Active(HuntRules.Phase.Transforming, 10.5, 2), "Wave active while transforming.");
        check(timeline.Active(HuntRules.Phase.FinalChase, 11.5, 2), "Wave can finish after movement releases.");
        check(!timeline.Active(HuntRules.Phase.FinalChase, 12, 2), "Wave ends at its original deadline.");
        check(!timeline.Active(HuntRules.Phase.RoundOver, 11, 2), "RoundOver hides wave immediately.");
        timeline.Cancel();
        check(!timeline.Active(HuntRules.Phase.Transforming, 11, 2), "Disable cancels a pending wave.");
        check(!timeline.TryStart(HuntRules.Phase.Transforming, 13), "Disable/re-enable does not generate another pulse.");
        check(new ApexWarningTimeline().TryStart(HuntRules.Phase.Transforming, 0), "Fresh round can start its own wave.");
        var notStarted = new ApexWarningTimeline(); notStarted.Cancel();
        check(notStarted.TryStart(HuntRules.Phase.Transforming, 0), "Disabling before combat does not consume a future transformation.");
        foreach (var origin in new[] { new[] { -20f, -12f }, new[] { 0f, 0f }, new[] { 20f, 12f } })
        {
            float radius = ApexTransformationMath.RequiredRadius(origin[0], origin[1], -20, -12, 20, 12, 10);
            foreach (var corner in new[] { new[] { -20f, -12f }, new[] { -20f, 12f }, new[] { 20f, -12f }, new[] { 20f, 12f } })
            {
                double dx = origin[0] - corner[0], dy = origin[1] - corner[1];
                check(radius >= Math.Sqrt(dx * dx + dy * dy) + 9.999, "Ring covers farthest corner plus survivor travel padding.");
            }
        }
        string[] identity = { "FFEB04", "FF40CC", "4DFF4D" };
        for (int apex = 0; apex < 3; apex++)
        {
            var rules = new HuntRules(); rules.Begin();
            int victim = (apex + 1) % 3, survivor = (apex + 2) % 3;
            rules.ResolveContacts(HuntRules.ContactBit(apex, victim));
            check(HuntPresentation.Status(rules, survivor, identity, true).Contains("RED WAVE FROM APEX"), "Survivor sees wave message.");
            check(HuntPresentation.Status(rules, victim, identity, true).StartsWith("ELIMINATED"), "Dead player retains elimination message.");
            check(HuntPresentation.Status(rules, survivor, identity, true).Contains(identity[apex]), "Avoid label uses saved identity tint.");
            rules.AdvanceTransformation(1);
            check(HuntPresentation.Status(rules, survivor, identity, true).Contains("AVOID APEX"), "Wave never replaces threat identity.");
            check(HuntPresentation.Status(rules, survivor, identity).EndsWith("HUNT: NONE"), "Usual label returns after wave.");
            check(!HuntPresentation.Status(rules, apex, identity, true).Contains("RED WAVE"), "Apex retains hunt label.");
            rules.ResolveContacts(HuntRules.ContactBit(apex, survivor));
            check(!HuntPresentation.Status(rules, apex, identity, true).Contains("RED WAVE"), "Result overrides wave presentation.");
        }
        return count;
    }
}
