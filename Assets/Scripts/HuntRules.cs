using System;

// Pure rules: no Unity callbacks or scene objects. Player IDs are 0, 1, 2.
public sealed class HuntRules
{
    public enum Phase { Opening, TriangleHunt, Transforming, FinalChase, RoundOver }
    public Phase State { get; private set; } = Phase.Opening;
    public int AliveMask { get; private set; } = 7;
    public int ApexId { get; private set; } = -1;
    public int WinnerId { get; private set; } = -1;
    public float TransformRemaining { get; private set; }
    private readonly int[] killedBy = { -1, -1, -1 };

    public bool IsAlive(int id) => id >= 0 && id < 3 && (AliveMask & (1 << id)) != 0;
    public int KilledBy(int id) => killedBy[id];
    public void Begin() { if (State == Phase.Opening) State = Phase.TriangleHunt; }

    public static int ContactBit(int a, int b)
    {
        if (a < 0 || a > 2 || b < 0 || b > 2 || a == b) return 0;
        if (a > b) { int old = a; a = b; b = old; }
        return 1 << (a * 3 + b);
    }

    public void ResolveContacts(int contacts)
    {
        if (State != Phase.TriangleHunt && State != Phase.FinalChase) return;
        int victims = 0;
        int firstKiller = -1;
        for (int a = 0; a < 3; a++) for (int b = a + 1; b < 3; b++)
        {
            if ((contacts & ContactBit(a, b)) == 0 || !IsAlive(a) || !IsAlive(b)) continue;
            int killer;
            if (State == Phase.FinalChase) killer = ApexId;
            else killer = (a + 1) % 3 == b ? a : b;
            if (killer != a && killer != b) continue;
            int victim = killer == a ? b : a;
            victims |= 1 << victim;
            killedBy[victim] = killer;
            firstKiller = killer;
        }
        if (victims == 0) return;
        AliveMask &= ~victims;
        int count = 0;
        for (int i = 0; i < 3; i++) if (IsAlive(i)) count++;
        if (count <= 1)
        {
            State = Phase.RoundOver;
            for (int i = 0; i < 3; i++) if (IsAlive(i)) WinnerId = i;
            return;
        }
        ApexId = firstKiller;
        TransformRemaining = 1f;
        State = Phase.Transforming;
    }

    public void AdvanceTransformation(float seconds)
    {
        if (seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (State != Phase.Transforming) return;
        TransformRemaining = Math.Max(0f, TransformRemaining - seconds);
        if (TransformRemaining <= .000001f) { TransformRemaining = 0f; State = Phase.FinalChase; }
    }
}
