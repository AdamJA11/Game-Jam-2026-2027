using System;

public static class Phase4cTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        for (int killer = 0; killer < 3; killer++)
        {
            int victim = (killer + 1) % 3, survivor = (killer + 2) % 3;
            var rules = new HuntRules(); var events = new KillClawEvents();
            check(events.Observe(rules).Killers == 0, "No startup claws.");
            rules.Begin(); rules.ResolveContacts(HuntRules.ContactBit(killer, victim));
            var first = events.Observe(rules);
            check(first.ForPlayer(killer) == KillClawRole.Killer, "Initial killer routed by identity.");
            check(first.ForPlayer(victim) == KillClawRole.Victim, "Initial victim routed by identity.");
            check(first.ForPlayer(survivor) == KillClawRole.None, "Uninvolved viewport unaffected.");
            check(first.VictimForPlayer(rules, killer) == victim && first.VictimForPlayer(rules, victim) == victim,
                "Both participating cameras anchor to the same victim, not their own players.");
            check(first.VictimForPlayer(rules, survivor) == -1, "Uninvolved camera has no death anchor.");
            var repeated = events.Observe(rules);
            check(repeated.Killers == 0 && repeated.Victims == 0, "Repeated ApplyState/contact cannot replay.");
            rules.AdvanceTransformation(1); rules.ResolveContacts(HuntRules.ContactBit(killer, survivor));
            var final = events.Observe(rules);
            check(final.ForPlayer(killer) == KillClawRole.Killer && final.ForPlayer(survivor) == KillClawRole.Victim, "Apex final capture routes to both participants.");
            check(final.ForPlayer(victim) == KillClawRole.None, "Previously dead viewport not retriggered.");
        }
        var draw = new HuntRules(); draw.Begin(); draw.ResolveContacts(HuntRules.ContactBit(0, 1) | HuntRules.ContactBit(0, 2) | HuntRules.ContactBit(1, 2));
        var drawBatch = new KillClawEvents().Observe(draw);
        for (int id = 0; id < 3; id++) check(drawBatch.ForPlayer(id) == KillClawRole.Victim, "Death strength takes precedence in simultaneous draw.");
        var chain = new HuntRules(); chain.Begin(); chain.ResolveContacts(HuntRules.ContactBit(0, 1) | HuntRules.ContactBit(1, 2));
        var chainBatch = new KillClawEvents().Observe(chain);
        check(chainBatch.ForPlayer(1) == KillClawRole.Victim && chainBatch.ForPlayer(0) == KillClawRole.Killer, "Mixed kill/death does not stack roles.");
        check(chainBatch.VictimForPlayer(chain, 1) == 1 && chainBatch.VictimForPlayer(chain, 0) == 1,
            "Simultaneous killer/victim anchors to own death while its killer anchors to that victim.");
        var escape = new HuntRules(); var escapeEvents = new KillClawEvents(); escape.Begin(); escape.ResolveContacts(HuntRules.ContactBit(0, 1));
        escapeEvents.Observe(escape); escape.ResolveEscape(1 << 2);
        var escapeBatch = escapeEvents.Observe(escape);
        check(escapeBatch.Killers == 0 && escapeBatch.Victims == 0, "Door victory creates no false elimination.");

        var animation = new KillClawAnimation();
        check(!animation.Active(0) && animation.Alpha(0, .38f) == 0, "Fresh overlay hidden.");
        animation.Start(KillClawRole.Victim, 0, .35f);
        check(animation.Alpha(0, .38f) == 0, "Attack starts transparent.");
        check(Math.Abs(animation.Alpha(.075, .38f) - .38f) < .000001f, "Victim reaches configured impact hold.");
        bool bounded = true;
        for (int step = 0; step <= 400; step++) { float alpha = animation.Alpha(step / 1000.0, .38f); bounded &= alpha >= 0 && alpha <= .38f; }
        check(bounded, "Alpha bounded across entire attack/hold/fade.");
        check(animation.Alpha(.3, .38f) < animation.Alpha(.2, .38f), "Tail fades toward zero.");
        check(animation.Alpha(.351, .38f) == 0 && !animation.Active(.351), "Finishes exactly without lingering image.");
        animation.Start(KillClawRole.Killer, 1, .35f);
        check(Math.Abs(animation.Alpha(1.075, .22f) - .22f) < .000001f, "New real event restarts one reusable overlay at new role strength.");
        check(animation.Alpha(1.075, 0) == 0, "Zero role alpha disables graphic.");
        animation.Cancel(); check(animation.Role == KillClawRole.None && animation.Alpha(1.1, .38f) == 0, "Modal takeover/disable cancels completely.");

        // Regression: map bounds hold the camera back while the victim reaches a corner.
        for (int sign = -1; sign <= 1; sign += 2)
        {
            float worldX = sign * 20, worldY = sign * 12;
            float cameraX = DeathCameraShakeState.ClampAxis(worldX, -21, 21, 6);
            float cameraY = DeathCameraShakeState.ClampAxis(worldY, -13, 13, 3);
            KillClawPositioning.Project(worldX, worldY, cameraX, cameraY, 3, 2, out float x, out float y);
            check(Math.Abs(x - (.5f + sign * 5f / 12)) < .00001f && Math.Abs(y - (.5f + sign / 3f)) < .00001f,
                "Corner deaths project near the corner instead of the fixed viewport center.");
        }
        KillClawPositioning.Project(5, 2, 5, 2, 3, 2, out float centeredX, out float centeredY);
        check(centeredX == .5f && centeredY == .5f, "Unclamped player-centered death remains centered.");
        KillClawPositioning.Project(5, 2, 2, 2, 3, 2, out float killerX, out float killerY);
        check(killerX == .75f && killerY == .5f, "Same death has a different position in the killer's camera.");
        KillClawPositioning.Project(20, 12, 0, 0, 3, 2, out float outsideX, out float outsideY);
        check(outsideX > 1 && outsideY > 1, "Offscreen death is left for viewport clipping rather than pinned to a false edge position.");
        return count;
    }
}
