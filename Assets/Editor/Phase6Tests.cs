using System;

public static class Phase6Tests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (value, message) => { count++; if (!value) throw new Exception(message); };
        var timer = new RoundEndReveal();
        check(!timer.Observe(false, 10, .6f), "No result during the hunt.");
        check(!timer.Observe(true, 20, .6f), "RoundOver starts a delay, not an immediate overlay.");
        check(!timer.Observe(true, 20.3, .6f), "Repeated observations cannot skip the delay.");
        check(!timer.Observe(true, 20.59, .6f), "Hidden immediately before the deadline.");
        check(timer.Observe(true, 20.61, .6f), "Repeated observations did not postpone the deadline.");
        check(!timer.Observe(true, 100, .6f), "Reveal happens only once.");
        var resumed = new RoundEndReveal();
        check(!resumed.Observe(true, 0, .6f), "Begin a pending reveal.");
        // No calls while disabled: re-enable after the original deadline.
        check(resumed.Observe(true, 5, .6f), "Re-enable preserves the elapsed deadline.");
        check(new RoundEndReveal().Observe(true, 5, 0), "Zero delay remains supported.");
        check(!new RoundEndReveal().Observe(false, 100, .6f), "Fresh scene has no old pending reveal.");
        var state = new RoundEndState();
        var delayed = new RoundEndReveal();
        delayed.Observe(true, 0, .6f);
        check(!state.TryBeginReload(true, true), "No navigation before the visible result.");
        if (delayed.Observe(true, 1, .6f)) state.Observe(HuntRules.Phase.RoundOver);
        check(state.TryBeginReload(true, true), "Navigation works after reveal.");
        check(!state.TryBeginReload(true, true), "Both buttons retain the duplicate load gate.");
        check(DeathBurstTiming.PieceCount * 3 == 27, "Fixed fragment budget for three players.");
        check(DeathBurstTiming.Travel(0) == 0, "Pieces start exactly at their cells.");
        check(DeathBurstTiming.Travel(.9f) > DeathBurstTiming.Travel(.3f), "Pieces continue outward.");
        check(DeathBurstTiming.Travel(.6f) - DeathBurstTiming.Travel(.3f)
            < DeathBurstTiming.Travel(.3f), "Outward movement slows over equal intervals.");
        check(DeathBurstTiming.Opacity(0, .9f, .5f) == 1, "Starts opaque.");
        check(DeathBurstTiming.Opacity(.3f, .9f, .5f) == 1, "No premature fade.");
        check(Math.Abs(DeathBurstTiming.Opacity(.65f, .9f, .5f) - .5f) < .00001f, "Mid-fade alpha.");
        check(DeathBurstTiming.Opacity(.9f, .9f, .5f) == 0, "Invisible at completion.");
        check(DeathBurstTiming.Opacity(5, .9f, .5f) == 0, "Long frames cannot leave negative alpha.");
        check(DeathBurstTiming.Opacity(.8f, .9f, 0) == 1, "Optional zero fade avoids division by zero.");
        check(!DeathBurstTiming.Complete(.89f, .9f), "Effect active before lifetime.");
        check(DeathBurstTiming.Complete(.9f, .9f), "Effect completes at lifetime.");
        check(DeathBurstTiming.Complete(5, .9f), "Long frame still completes the effect.");
        return count;
    }
}
