using System;

public static class Phase3fTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        int[] distribution = new int[3];
        for (int value = 0; value < 100; value++) distribution[(int)RandomItemRules.Choose(false, 40, 40, 20, max => value)]++;
        check(distribution[0] == 40 && distribution[1] == 40 && distribution[2] == 20, "Weighted pool matches authored probabilities.");
        distribution = new int[3];
        for (int value = 0; value < 60; value++) distribution[(int)RandomItemRules.Choose(true, 40, 40, 20, max => value)]++;
        check(distribution[0] == 40 && distribution[1] == 0 && distribution[2] == 20, "Owned Tracker excluded and remaining weights renormalized.");
        check(RandomItemRules.ChoosePlayer(5, max => 0) == 0 && RandomItemRules.ChoosePlayer(5, max => 1) == 2, "Distinct simultaneous occupants have equal selection slots.");
        check(RandomItemRules.ChoosePlayer(0, max => 0) == -1, "Empty pickup has no winner.");
        bool rejected = false;
        try { RandomItemRules.Choose(false, 0, 0, 0, max => 0); } catch (ArgumentException) { rejected = true; }
        check(rejected, "Empty reward pool rejected without consumption.");
        rejected = false;
        try { RandomItemRules.Choose(false, -1, 40, 20, max => 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected, "Negative weight rejected.");
        rejected = false;
        try { RandomItemRules.Choose(false, 40, 40, 20, max => max); } catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected, "Invalid draw rejected.");
        rejected = false;
        try { RandomItemRules.Choose(true, 0, 40, 0, max => 0); } catch (ArgumentException) { rejected = true; }
        check(rejected, "Duplicate-Tracker-only pool cannot silently grant nothing.");
        var effect = new PlayerItemState(); effect.Give(RandomItemKind.Speed, 0, 10);
        check(effect.SpeedActive(9.99) && !effect.SpeedActive(10), "Exact ten-second deadline.");
        effect.Give(RandomItemKind.Speed, 5, 10);
        check(effect.SpeedUntil == 15, "Repeat refreshes rather than adds duration.");
        effect.Give(RandomItemKind.Tracker, 6, 10);
        check(effect.Tracker && effect.SpeedActive(10), "Independent effects coexist.");
        check(effect.Tracker && !effect.SpeedActive(1000), "Tracker does not expire with speed or elapsed time.");
        effect.Clear(); check(!effect.Tracker && !effect.SpeedActive(0), "Death/round cleanup clears ownership and deadline.");
        var combat = new MovementSpeedModifier(); var item = new MovementSpeedModifier(); combat.Set(1.35f); item.Set(1.5f);
        check(Math.Abs(item.EffectiveSpeed(combat.EffectiveSpeed(4)) - 8.1f) < .00001f, "Apex and item modifiers combine absolutely.");
        item.Set(1); check(Math.Abs(item.EffectiveSpeed(combat.EffectiveSpeed(4)) - 5.4f) < .00001f, "Item expiry retains Apex boost.");
        foreach (float offset in new[] { -45f, 45f }) for (int angle = 0; angle < 360; angle += 45)
        {
            float x = (float)Math.Cos(angle * Math.PI / 180), y = (float)Math.Sin(angle * Math.PI / 180);
            RandomItemRules.Rotate(x, y, offset, out float rx, out float ry);
            check(Math.Abs(rx * rx + ry * ry - 1) < .00001f, "Rotating input preserves speed.");
            check(Math.Abs(rx - Math.Cos((angle + offset) * Math.PI / 180)) < .00001f
                && Math.Abs(ry - Math.Sin((angle + offset) * Math.PI / 180)) < .00001f, "All directions preserve fixed offset.");
        }
        RandomItemRules.Rotate(0, 0, 45, out float zeroX, out float zeroY);
        check(zeroX == 0 && zeroY == 0, "No input does not create autonomous movement.");
        RandomItemRules.Rotate(1, 0, 45, out float dx, out float dy);
        check(Math.Abs(dx - .70710678f) < .00001f && Math.Abs(dy - dx) < .00001f, "Held D produces a 45-degree decoy.");
        var grid = new PersistentTrailGrid(8, 6);
        check(grid.Capacity == 48 && grid.At(2, 2) == 0, "Trail storage starts empty with fixed capacity.");
        check(grid.Stamp(2, 2, 0) && grid.Stamp(2, 2, 1) && grid.At(2, 2) == 3, "Crossing tracks preserve both target IDs.");
        for (int i = 0; i < 10000; i++) grid.Stamp(2, 2, 0);
        check(grid.Capacity == 48 && grid.At(2, 2) == 3, "Long repeated walking does not grow storage or age out marks.");
        check(!grid.Stamp(-1, 0, 0) && !grid.Stamp(100, 0, 0), "Out-of-bounds stamps rejected.");
        grid.Clear(); check(grid.At(2, 2) == 0, "Round cleanup clears marks.");
        return count;
    }
}
