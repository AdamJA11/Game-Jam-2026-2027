using System;

public enum RandomItemKind { Speed, Tracker, Decoy }

public static class RandomItemRules
{
    public static RandomItemKind Choose(bool hasTracker, int speed, int tracker, int decoy, Func<int, int> draw)
    {
        if (speed < 0 || tracker < 0 || decoy < 0) throw new ArgumentOutOfRangeException("weights");
        int tracking = hasTracker ? 0 : tracker;
        long totalLong = (long)speed + tracking + decoy;
        if (totalLong <= 0 || totalLong > int.MaxValue || draw == null) throw new ArgumentException("No valid item draw.");
        int total = (int)totalLong, value = draw(total);
        if (value < 0 || value >= total) throw new ArgumentOutOfRangeException(nameof(draw));
        return value < speed ? RandomItemKind.Speed : value < speed + tracking ? RandomItemKind.Tracker : RandomItemKind.Decoy;
    }

    public static int ChoosePlayer(int mask, Func<int, int> draw)
    {
        int count = 0; for (int i = 0; i < 3; i++) if ((mask & (1 << i)) != 0) count++;
        if (count == 0) return -1;
        int choice = draw(count);
        if (choice < 0 || choice >= count) throw new ArgumentOutOfRangeException(nameof(draw));
        for (int i = 0; i < 3; i++) if ((mask & (1 << i)) != 0 && choice-- == 0) return i;
        return -1;
    }

    public static void Rotate(float x, float y, float degrees, out float rotatedX, out float rotatedY)
    {
        double radians = degrees * Math.PI / 180;
        rotatedX = (float)(x * Math.Cos(radians) - y * Math.Sin(radians));
        rotatedY = (float)(x * Math.Sin(radians) + y * Math.Cos(radians));
    }
}

public sealed class PlayerItemState
{
    public bool Tracker { get; private set; }
    public double SpeedUntil { get; private set; } = double.NegativeInfinity;
    public void Give(RandomItemKind kind, double now, float speedDuration)
    {
        if (kind == RandomItemKind.Speed) SpeedUntil = now + speedDuration;
        if (kind == RandomItemKind.Tracker) Tracker = true;
    }
    public bool SpeedActive(double now) => now < SpeedUntil;
    public int SpeedSeconds(double now) => SpeedActive(now) ? (int)Math.Ceiling(SpeedUntil - now) : 0;
    public void Clear() { Tracker = false; SpeedUntil = double.NegativeInfinity; }
}

// Fixed storage: tracks merge by target ID, never age out, and do not grow with round length.
public sealed class PersistentTrailGrid
{
    private readonly byte[] cells;
    public int Width { get; }
    public int Height { get; }
    public int Capacity => cells.Length;
    public PersistentTrailGrid(int width, int height)
    { if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(); Width = width; Height = height; cells = new byte[checked(width * height)]; }
    public byte At(int x, int y) => cells[y * Width + x];
    public bool Stamp(int x, int y, int player)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || player < 0 || player >= 3) return false;
        int index = y * Width + x; byte value = (byte)(cells[index] | (1 << player));
        if (value == cells[index]) return false;
        cells[index] = value; return true;
    }
    public void Clear() { Array.Clear(cells, 0, cells.Length); }
}
