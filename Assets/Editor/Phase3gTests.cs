using System;

public static class Phase3gTests
{
    public static int Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        var shake = new DeathCameraShakeState();
        shake.Sample(0, out float x, out float y);
        check(x == 0 && y == 0, "Fresh camera has no offset.");
        check(shake.TryStart(10, 0, .25f, .12f, 25), "Death starts one presentation.");
        check(!shake.TryStart(10.1, 0, 5, 2, 25), "Repeated state cannot restart or amplify it.");
        shake.Sample(9, out x, out y);
        check(x == 0 && y == 0, "No offset before start time.");
        shake.Sample(10, out x, out y);
        check(Math.Abs(Math.Sqrt(x * x + y * y) - .12) < .000001, "Initial impact uses configured vector magnitude.");
        bool bounded = true;
        for (int step = 0; step <= 250; step++)
        {
            double age = step / 1000.0;
            shake.Sample(10 + age, out x, out y);
            double envelope = .12 * Math.Pow(Math.Max(0, 1 - age / .25), 2);
            bounded &= Math.Sqrt(x * x + y * y) <= envelope + .000001;
        }
        check(bounded, "Entire curve bounded by decaying total magnitude, including diagonals.");
        shake.Sample(10.125, out x, out y);
        check(Math.Sqrt(x * x + y * y) <= .030001, "Halfway envelope has faded quadratically.");
        shake.Sample(10.25, out x, out y);
        check(x == 0 && y == 0 && !shake.Active(10.25), "Exact zero at completion.");
        check(!shake.TryStart(50, 0, .25f, .12f, 25), "Completed death never starts another shake in this scene.");
        var first = new DeathCameraShakeState(); var repeat = new DeathCameraShakeState(); var other = new DeathCameraShakeState();
        first.TryStart(0, 1, .25f, .12f, 25); repeat.TryStart(0, 1, .25f, .12f, 25); other.TryStart(0, 2, .25f, .12f, 25);
        first.Sample(.05, out float fx, out float fy); repeat.Sample(.05, out float rx, out float ry); other.Sample(.05, out float ox, out float oy);
        check(fx == rx && fy == ry, "Local seed reproduces noise without a shared RNG.");
        check(fx != ox || fy != oy, "Different player seeds have independent noise.");
        bool stable = true;
        for (int i = 0; i < 100; i++) { first.Sample(.05, out x, out y); stable &= x == fx && y == fy; }
        check(stable, "Repeated samples do not integrate drift or advance random state.");
        first.Cancel(); first.Sample(.06, out x, out y);
        check(x == 0 && y == 0, "Cancel immediately clears offset.");
        check(!first.TryStart(.07, 1, .25f, .12f, 25), "Disable/re-enable cannot replay the same death.");
        var beforeDeath = new DeathCameraShakeState(); beforeDeath.Cancel();
        check(beforeDeath.TryStart(0, 0, .25f, .12f, 25), "Introduction cancellation does not consume a future death.");
        var zero = new DeathCameraShakeState(); zero.TryStart(0, 0, .25f, 0, 25); zero.Sample(.05, out x, out y);
        check(x == 0 && y == 0 && !zero.Active(.05), "Zero amplitude preserves stable view.");
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { new DeathCameraShakeState().TryStart(0, 0, invalid, .12f, 25); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            check(rejected, "Invalid duration rejected.");
        }
        check(DeathCameraShakeState.ClampAxis(2.12f, -21, 21, 6) == 2.12f, "Interior composition preserves offset.");
        check(DeathCameraShakeState.ClampAxis(15.12f, -21, 21, 6) == 15, "Outward edge offset clamps safely.");
        check(DeathCameraShakeState.ClampAxis(14.88f, -21, 21, 6) == 14.88f, "Inward edge offset remains visible.");
        check(DeathCameraShakeState.ClampAxis(-15.12f, -21, 21, 6) == -15, "Opposite edge clamps safely.");
        check(DeathCameraShakeState.ClampAxis(15, -21, 21, 8) == 13, "Resized view re-clamps held composition.");
        check(DeathCameraShakeState.ClampAxis(100, -1, 1, 3) == 0, "Small-map axis stays centered.");
        check(new DeathCameraShakeState().TryStart(0, 0, .25f, .12f, 25), "Fresh scene can play a fresh death.");
        return count;
    }
}
