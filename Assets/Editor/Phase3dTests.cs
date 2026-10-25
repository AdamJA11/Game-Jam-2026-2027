using System;

public static class Phase3dTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        for (int apex = 0; apex < 3; apex++)
        {
            int victim = (apex + 1) % 3, survivor = (apex + 2) % 3;
            var rules = new HuntRules();
            check(!rules.ResolveEscape(7), "Opening cannot escape."); rules.Begin();
            check(!rules.ResolveEscape(7), "Triangle hunt cannot escape.");
            rules.ResolveContacts(HuntRules.ContactBit(apex, victim));
            check(!rules.ResolveEscape(1 << apex) && !rules.ResolveEscape(1 << victim), "Only living survivor opens.");
            rules.ResolveStep(HuntRules.ContactBit(apex, survivor), 1 << survivor, .1f);
            check(rules.WinnerId == survivor && rules.Outcome == HuntRules.Result.EscapedThroughDoor, "Transformation immunity allows early door victory.");
            check(rules.IsAlive(apex) && rules.IsAlive(survivor), "Escape does not fake a death.");
            rules.ResolveStep(7, 7, 5);
            check(rules.WinnerId == survivor, "Committed escape locked.");
            var capture = new HuntRules(); capture.Begin(); capture.ResolveContacts(HuntRules.ContactBit(apex, victim));
            capture.ResolveStep(HuntRules.ContactBit(apex, survivor), 1 << survivor, 1);
            check(capture.WinnerId == apex && capture.Outcome == HuntRules.Result.Captured, "Capture wins at completed-transformation boundary.");
            check(RoundEndState.ResultText(survivor, HuntRules.Result.EscapedThroughDoor).Contains("ESCAPED!"), "Escape result accurate.");
            check(!RoundEndState.ResultText(survivor, HuntRules.Result.EscapedThroughDoor).Contains("LAST PLAYER ALIVE"), "No false alive-count claim.");
            var chase = new HuntRules(); chase.Begin(); chase.ResolveContacts(HuntRules.ContactBit(apex, victim));
            check(!chase.ResolveEscape(0), "Empty door cannot end transformation.");
            chase.AdvanceTransformation(1); chase.ResolveStep(0, 1 << survivor, .02f);
            check(chase.WinnerId == survivor && chase.Outcome == HuntRules.Result.EscapedThroughDoor, "Uncaptured final-chase survivor escapes.");
        }
        var draw = new HuntRules(); draw.Begin();
        draw.ResolveStep(HuntRules.ContactBit(0, 1) | HuntRules.ContactBit(1, 2) | HuntRules.ContactBit(0, 2), 7, .02f);
        check(draw.Outcome == HuntRules.Result.Draw && draw.WinnerId == -1, "Initial draw takes priority; door does not create a survivor.");
        return count;
    }
}
