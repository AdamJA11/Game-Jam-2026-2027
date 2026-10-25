using System;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class RandomPlayerSpawns : MonoBehaviour
{
    [SerializeField] private TopDownPlayerMovement[] players = new TopDownPlayerMovement[3];
    [SerializeField] private Transform[] spawnPoints = new Transform[3];
    [SerializeField] private LocalPlayerCameraFollow[] cameras = new LocalPlayerCameraFollow[3];
    [SerializeField] private Rect playableBounds = new Rect(-20, -12, 40, 24);
    private int[] assignment;
    private System.Collections.Generic.List<int[]> validGroups;
    private float closeViewSize = 3f;

    public bool HasSpawned => assignment != null;
    public int SpawnPointCount => spawnPoints == null ? 0 : spawnPoints.Length;
    public Rect PlayableBounds => playableBounds;
    public int GetAssignedSpawn(int playerIndex) => assignment[playerIndex];
    public Transform GetSpawnPoint(int index) => spawnPoints[index];

    public void Configure(TopDownPlayerMovement[] playerList, Transform[] points, LocalPlayerCameraFollow[] views)
    {
        players = playerList;
        spawnPoints = points;
        cameras = views;
    }

    private void Awake()
    {
        if (TrySpawn(out string error)) return;
        Debug.LogError("RandomPlayerSpawns: " + error, this);
        enabled = false;
    }

    public bool TrySpawn(out string error)
    {
        error = null;
        if (HasSpawned) return true;
        if (!ValidateConfiguration(out error)) return false;

        int[] selected = SpawnSelection.Choose(validGroups, max => UnityEngine.Random.Range(0, max));
        for (int i = 0; i < 3; i++)
        {
            Rigidbody2D body = players[i].GetComponent<Rigidbody2D>();
            var interpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation2D.None;
            Vector3 position = spawnPoints[selected[i]].position;
            position.z = 0;
            players[i].transform.position = position;
            body.position = position;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            body.interpolation = interpolation;
        }
        Physics2D.SyncTransforms();
        assignment = selected;
        foreach (var camera in cameras) camera.FollowNow();
        return true;
    }

    // Fisher–Yates, with an injectable exclusive-upper-bound draw for exhaustive checks.
    public static int[] CreateAssignment(Func<int, int> draw)
    {
        if (draw == null) throw new ArgumentNullException(nameof(draw));
        int[] result = { 0, 1, 2 };
        for (int i = 2; i > 0; i--)
        {
            int j = draw(i + 1);
            if (j < 0 || j > i) throw new ArgumentOutOfRangeException(nameof(draw));
            int old = result[i]; result[i] = result[j]; result[j] = old;
        }
        return result;
    }

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (players == null || players.Length != 3 || spawnPoints == null || spawnPoints.Length < 3
            || cameras == null || cameras.Length != 3)
            return Invalid("Assign three players/cameras and at least three spawn points.", out error);
        if (!Finite(playableBounds.x) || !Finite(playableBounds.y) || !Finite(playableBounds.width) || !Finite(playableBounds.height)
            || playableBounds.width <= 0 || playableBounds.height <= 0)
            return Invalid("Playable bounds must have positive dimensions.", out error);
        for (int i = 0; i < 3; i++)
        {
            if (players[i] == null || cameras[i] == null)
                return Invalid("Player, spawn, or camera reference " + i + " is missing.", out error);
            if (!players[i].gameObject.activeInHierarchy || players[i].GetComponent<Rigidbody2D>() == null
                || players[i].GetComponent<BoxCollider2D>() == null || !players[i].GetComponent<BoxCollider2D>().enabled)
                return Invalid("Each player needs an active object, Rigidbody2D, and enabled BoxCollider2D.", out error);
            if (cameras[i].Target != players[i].transform)
                return Invalid("Each camera must follow its corresponding player.", out error);
            for (int j = 0; j < i; j++) if (players[i] == players[j] || cameras[i] == cameras[j])
                return Invalid("Players and cameras must be distinct.", out error);
        }
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) return Invalid("Spawn reference " + i + " is missing.", out error);
            Vector3 position = spawnPoints[i].position;
            if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z) || Mathf.Abs(position.z) > .001f)
                return Invalid("Spawn positions must be finite and at Z = 0.", out error);
            for (int j = 0; j < i; j++)
                if (spawnPoints[i] == spawnPoints[j]
                    || Vector2.Distance(position, spawnPoints[j].position) < .001f)
                    return Invalid("Players, cameras, and spawn positions must be distinct.", out error);
        }

        Physics2D.SyncTransforms();
        // Every player can occupy every point: validate all possible assignments before teleporting.
        for (int p = 0; p < 3; p++)
        {
            var collider = players[p].GetComponent<BoxCollider2D>();
            Vector2 size = Vector2.Scale(collider.size, new Vector2(Mathf.Abs(collider.transform.lossyScale.x), Mathf.Abs(collider.transform.lossyScale.y)));
            float angle = collider.transform.eulerAngles.z;
            Vector2 offset = collider.transform.TransformVector(collider.offset);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 extent = new Vector2(Mathf.Abs(Mathf.Cos(rad)) * size.x + Mathf.Abs(Mathf.Sin(rad)) * size.y,
                Mathf.Abs(Mathf.Sin(rad)) * size.x + Mathf.Abs(Mathf.Cos(rad)) * size.y) * .5f;
            for (int s = 0; s < spawnPoints.Length; s++)
            {
                Vector2 center = (Vector2)spawnPoints[s].position + offset;
                if (center.x - extent.x < playableBounds.xMin || center.x + extent.x > playableBounds.xMax
                    || center.y - extent.y < playableBounds.yMin || center.y + extent.y > playableBounds.yMax)
                    return Invalid("Spawn " + spawnPoints[s].name + " does not fit inside the playable bounds.", out error);
                foreach (var hit in Physics2D.OverlapBoxAll(center, size, angle))
                    if (!IsPlayerCollider(hit))
                        return Invalid("Spawn " + spawnPoints[s].name + " overlaps " + hit.name + ".", out error);
            }
        }
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var intro = root.GetComponentInChildren<CameraIntroduction>(true);
            if (intro != null) { closeViewSize = intro.NormalSize; break; }
        }
        validGroups = SpawnSelection.Groups(spawnPoints.Length, Compatible);
        if (validGroups.Count == 0) return Invalid("No three-point group has clear footprints and separate initial camera views.", out error);
        return true;
    }

    private bool Compatible(int a, int b)
    {
        // Conservative largest-footprint margin works for every identity assignment.
        float margin = 0;
        foreach (var player in players) margin = Mathf.Max(margin, player.GetComponent<BoxCollider2D>().bounds.extents.magnitude);
        Vector2 first = spawnPoints[a].position, second = spawnPoints[b].position;
        if (Vector2.Distance(first, second) <= margin * 2) return false;
        for (int i = 0; i < 3; i++)
        {
            var view = cameras[i].GetComponent<Camera>();
            foreach (float aspect in new[] { view.aspect, 16f / 9, 16f / 10 })
            {
                float halfY = closeViewSize;
                float halfX = halfY * aspect;
                Rect bounds = cameras[i].MapBounds;
                if (Visible(first, second, bounds, halfX, halfY, margin)
                    || Visible(second, first, bounds, halfX, halfY, margin)) return false;
            }
        }
        return true;
    }

    private static bool Visible(Vector2 target, Vector2 other, Rect bounds, float halfX, float halfY, float margin)
    {
        float x = bounds.width <= halfX * 2 ? bounds.center.x : Mathf.Clamp(target.x, bounds.xMin + halfX, bounds.xMax - halfX);
        float y = bounds.height <= halfY * 2 ? bounds.center.y : Mathf.Clamp(target.y, bounds.yMin + halfY, bounds.yMax - halfY);
        return Mathf.Abs(other.x - x) <= halfX + margin && Mathf.Abs(other.y - y) <= halfY + margin;
    }

    private bool IsPlayerCollider(Collider2D hit)
    {
        foreach (var player in players)
            if (hit.transform == player.transform || hit.transform.IsChildOf(player.transform)) return true;
        return false;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Invalid(string message, out string error) { error = message; return false; }
}
