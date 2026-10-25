using System;

public static class HuntAudioTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        for (int apex = 0; apex < 3; apex++)
        {
            var rules = new HuntRules(); var events = new HuntAudioEvents();
            events.Observe(rules, out int kills, out bool transform);
            check(kills == 0 && !transform, "No startup sounds.");
            rules.Begin(); rules.ResolveContacts(HuntRules.ContactBit(apex, (apex + 1) % 3));
            events.Observe(rules, out kills, out transform);
            check(kills == 1 && transform, "First kill and transformation each trigger once.");
            events.Observe(rules, out kills, out transform);
            check(kills == 0 && !transform, "Repeated state cannot replay either clip.");
            rules.AdvanceTransformation(1); events.Observe(rules, out kills, out transform);
            check(kills == 0 && !transform, "Movement release does not start another audio event.");
            rules.ResolveContacts(HuntRules.ContactBit(apex, (apex + 2) % 3));
            events.Observe(rules, out kills, out transform);
            check(kills == 1 && !transform, "Final capture plays only the new kill.");
            events.Observe(rules, out kills, out transform);
            check(kills == 0 && !transform, "RoundOver does not repeat sounds.");
        }
        var draw = new HuntRules(); draw.Begin();
        draw.ResolveContacts(HuntRules.ContactBit(0, 1) | HuntRules.ContactBit(1, 2) | HuntRules.ContactBit(0, 2));
        new HuntAudioEvents().Observe(draw, out int drawKills, out bool drawTransform);
        check(drawKills == 3 && !drawTransform, "Simultaneous draw has three real eliminations and no fake transformation.");
        var instantEscape = new HuntRules(); instantEscape.Begin();
        instantEscape.ResolveStep(HuntRules.ContactBit(0, 1), 1 << 2, .02f);
        new HuntAudioEvents().Observe(instantEscape, out int earlyKills, out bool earlyTransform);
        check(earlyKills == 1 && !earlyTransform, "Same-step door victory does not start an already-cancelled transformation sound.");
        return count;
    }
}
