using System;

public static class RoundEndStateTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> assert = (value, message) => { count++; if (!value) throw new Exception(message); };
        var fresh = new RoundEndState();
        assert(!fresh.Visible && !fresh.IsReloading, "Starts hidden and ready.");
        assert(!fresh.TryBeginReload(true, true), "Hidden UI cannot reload.");
        foreach (var phase in new[] { HuntRules.Phase.Opening, HuntRules.Phase.TriangleHunt, HuntRules.Phase.Transforming, HuntRules.Phase.FinalChase })
            assert(!fresh.Observe(phase) && !fresh.Visible, "No overlay before final round result.");
        assert(fresh.Observe(HuntRules.Phase.RoundOver) && fresh.Visible, "RoundOver opens the overlay.");
        assert(!fresh.Observe(HuntRules.Phase.RoundOver), "Repeated result cannot create another overlay.");
        assert(!fresh.TryBeginReload(false, true) && !fresh.IsReloading, "Cannot reload outside RoundOver.");
        assert(!fresh.TryBeginReload(true, false) && !fresh.IsReloading, "Unregistered scene leaves retry possible.");
        assert(fresh.TryBeginReload(true, true) && fresh.IsReloading, "Valid click starts loading.");
        assert(!fresh.TryBeginReload(true, true), "Double-click suppressed.");
        fresh.ReloadFailed();
        assert(fresh.Visible && !fresh.IsReloading, "Failure keeps overlay and releases loading gate.");
        assert(fresh.TryBeginReload(true, true), "Retry works after a failure.");
        var reloaded = new RoundEndState();
        assert(!reloaded.Visible && !reloaded.IsReloading, "Fresh scene clears overlay/loading state.");
        assert(RoundEndState.ResultText(-1) == "DRAW\nNO SURVIVORS", "Draw text.");
        for (int winner = 0; winner < 3; winner++)
        {
            string text = RoundEndState.ResultText(winner);
            assert(text.Contains("P" + (winner + 1) + " WINS!") && text.Contains(HuntPresentation.ColorName(winner)), "Winner ID and color.");
            var rules = new HuntRules(); rules.Begin();
            rules.ResolveContacts(HuntRules.ContactBit(winner, (winner + 1) % 3));
            var overlay = new RoundEndState();
            assert(!overlay.Observe(rules.State), "First kill does not show restart.");
            rules.AdvanceTransformation(1);
            assert(!overlay.Observe(rules.State), "Final chase does not show restart.");
            rules.ResolveContacts(HuntRules.ContactBit(winner, (winner + 2) % 3));
            assert(overlay.Observe(rules.State), "Final elimination shows restart.");
        }
        var simultaneous = new HuntRules(); simultaneous.Begin();
        simultaneous.ResolveContacts(HuntRules.ContactBit(0, 1) | HuntRules.ContactBit(1, 2) | HuntRules.ContactBit(2, 0));
        var drawOverlay = new RoundEndState();
        assert(drawOverlay.Observe(simultaneous.State) && simultaneous.WinnerId == -1, "Simultaneous draw still offers restart.");
        var sharedButtons = new RoundEndState(); sharedButtons.Observe(HuntRules.Phase.RoundOver);
        assert(sharedButtons.TryBeginReload(true, true), "Main Menu can use the same loading gate.");
        assert(!sharedButtons.TryBeginReload(true, true), "Restart cannot race a pending Main Menu request.");
        sharedButtons.ReloadFailed();
        assert(sharedButtons.TryBeginReload(true, true), "Either destination can retry after a failed load.");
        return count;
    }
}
