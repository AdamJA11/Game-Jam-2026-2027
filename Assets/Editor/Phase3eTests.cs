using System;

public static class Phase3eTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool,string> check = (ok,message) => { count++; if (!ok) throw new Exception(message); };
        foreach (HuntRules.Result outcome in Enum.GetValues(typeof(HuntRules.Result)))
        {
            var gate = new EscapeStoryState();
            check(gate.CanBegin(outcome,0,1) == (outcome == HuntRules.Result.EscapedThroughDoor), "Continue is offered only for the supported escape story.");
            check(gate.TryBegin(outcome,0,1) == (outcome == HuntRules.Result.EscapedThroughDoor), "Only committed escape selects the car story, without requiring another map.");
        }
        for (int winner = 0; winner < 3; winner++) for (int apex = 0; apex < 3; apex++)
        {
            var gate = new EscapeStoryState();
            check(gate.TryBegin(HuntRules.Result.EscapedThroughDoor,winner,apex) == (winner != apex), "Validate distinct player identities.");
            if (winner == apex) continue;
            check(gate.Winner == winner && gate.Apex == apex && gate.Playing, "Snapshot preserves actual winner and Apex.");
            check(!gate.TryBegin(HuntRules.Result.EscapedThroughDoor,winner,apex), "Repeated Continue cannot start another film.");
            check(gate.Finish() && gate.Completed && !gate.Playing, "Completion/Skip enters the ending-actions state.");
            check(!gate.Finish(), "Completion, Skip, timeout converge on one finish.");
            check(!gate.CanBegin(HuntRules.Result.EscapedThroughDoor,winner,apex), "Ending actions hide Continue.");
            check(!gate.TryBegin(HuntRules.Result.EscapedThroughDoor,winner,apex), "Ending screen cannot replay the film.");
            var load = new RoundEndState(); load.Observe(HuntRules.Phase.RoundOver);
            check(load.Visible && !load.IsReloading, "Ending state keeps result actions available without requesting a load.");
            check(load.TryBeginReload(true,true), "Explicit Restart/Main Menu can still use the existing load gate.");
            check(!load.TryBeginReload(true,true), "Scene load gate still rejects double requests.");
            load.ReloadFailed();
            check(load.TryBeginReload(true,true) && gate.Completed, "Restart/menu failure can retry without replaying the film.");
        }
        var invalid = new EscapeStoryState();
        check(!invalid.TryBegin(HuntRules.Result.EscapedThroughDoor,1,1) && !invalid.Playing, "Invalid outcome snapshot does not hide results for a film.");
        check(!invalid.TryBegin(HuntRules.Result.EscapedThroughDoor,-1,1), "Missing winner rejected.");
        check(!invalid.TryBegin(HuntRules.Result.EscapedThroughDoor,0,3), "Missing Apex rejected.");
        check(!invalid.Finish(), "Idle presenter callbacks cannot request a load.");
        check(EscapeStoryState.Segment(1,2,4) == 0 && EscapeStoryState.Segment(5,2,4) == 1,
            "Animation path is bounded before and after a beat, including skipped frames.");
        check(EscapeStoryState.Segment(3,2,4) == .5f, "Beat midpoint is stable.");
        var fresh = new EscapeStoryState();
        check(!fresh.Playing && !fresh.Completed && fresh.TryBegin(HuntRules.Result.EscapedThroughDoor,1,2), "Fresh randomized round can play another eligible story.");
        return count;
    }
}
