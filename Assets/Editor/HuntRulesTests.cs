using System;

// Runs both from the editor menu and a standalone console harness without Unity.
public static class HuntRulesTests
{
    public static int Run()
    {
        int checks = 0;
        Action<bool, string> assert = (value, message) => { checks++; if (!value) throw new Exception(message); };
        int all = HuntRules.ContactBit(0, 1) | HuntRules.ContactBit(0, 2) | HuntRules.ContactBit(1, 2);
        var opening = new HuntRules();
        opening.ResolveContacts(all);
        assert(opening.State == HuntRules.Phase.Opening && opening.AliveMask == 7, "Opening is non-lethal.");
        for (int hunter = 0; hunter < 3; hunter++)
        {
            int prey = (hunter + 1) % 3, escapee = (hunter + 2) % 3;
            var rules = new HuntRules(); rules.Begin();
            int bit = HuntRules.ContactBit(hunter, prey);
            assert(bit == HuntRules.ContactBit(prey, hunter), "Contact order canonicalized.");
            rules.ResolveContacts(bit | bit);
            assert(!rules.IsAlive(prey) && rules.IsAlive(hunter) && rules.IsAlive(escapee), "Only correct prey dies.");
            assert(rules.KilledBy(prey) == hunter && rules.ApexId == hunter, "Correct killer becomes Apex.");
            assert(rules.State == HuntRules.Phase.Transforming && rules.WinnerId == -1, "First kill does not end round.");
            assert(rules.TransformRemaining == 1f, "Transformation starts at one second.");
            rules.ResolveContacts(all);
            assert(rules.TransformRemaining == 1f && rules.IsAlive(hunter) && rules.IsAlive(escapee), "Grace contact cannot kill or reset timer.");
            rules.AdvanceTransformation(.4f); rules.ResolveContacts(all);
            assert(Math.Abs(rules.TransformRemaining - .6f) < .00001f, "Contacts do not extend transformation.");
            rules.AdvanceTransformation(.599f);
            assert(rules.State == HuntRules.Phase.Transforming, "Cannot transform early.");
            rules.AdvanceTransformation(.001f);
            assert(rules.State == HuntRules.Phase.FinalChase, "Final chase begins after one second.");
            rules.ResolveContacts(HuntRules.ContactBit(escapee, hunter));
            assert(!rules.IsAlive(escapee) && rules.IsAlive(hunter), "Former predator dies to Apex.");
            assert(rules.State == HuntRules.Phase.RoundOver && rules.WinnerId == hunter, "Final kill ends round.");
            rules.ResolveContacts(all); rules.AdvanceTransformation(10); rules.Begin();
            assert(rules.WinnerId == hunter && rules.AliveMask == (1 << hunter), "Terminal result cannot be overwritten.");
        }
        for (int missingPair = 0; missingPair < 3; missingPair++)
        {
            int[] bits = { HuntRules.ContactBit(0, 1), HuntRules.ContactBit(1, 2), HuntRules.ContactBit(2, 0) };
            int mask = all & ~bits[missingPair];
            var rules = new HuntRules(); rules.Begin(); rules.ResolveContacts(mask);
            assert(rules.State == HuntRules.Phase.RoundOver && rules.WinnerId >= 0, "Two simultaneous kills leave a winner.");
            int reversed = 0; for (int i = 2; i >= 0; i--) if (i != missingPair) reversed |= bits[i];
            var other = new HuntRules(); other.Begin(); other.ResolveContacts(reversed);
            assert(other.AliveMask == rules.AliveMask && other.WinnerId == rules.WinnerId, "Callback order cannot change result.");
        }
        var draw = new HuntRules(); draw.Begin(); draw.ResolveContacts(all);
        assert(draw.AliveMask == 0 && draw.WinnerId == -1 && draw.State == HuntRules.Phase.RoundOver, "Three simultaneous pairs draw.");
        var empty = new HuntRules(); empty.Begin(); empty.ResolveContacts(0);
        assert(empty.AliveMask == 7 && empty.State == HuntRules.Phase.TriangleHunt, "No contact leaves state unchanged.");
        assert(HuntRules.ContactBit(0, 0) == 0 && HuntRules.ContactBit(-1, 2) == 0, "Invalid pairs ignored.");
        return checks;
    }
}
