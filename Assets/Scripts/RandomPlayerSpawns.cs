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

    public bool HasSpawned => assignment != null;
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

        int[] selected = CreateAssignment(max => UnityEngine.Random.Range(0, max));
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
        if (players == null || players.Length != 3 || spawnPoints == null || spawnPoints.Length != 3
            || cameras == null || cameras.Length != 3)
            return Invalid("Assign exactly three players, points, and cameras.", out error);
        if (playableBounds.width <= 0 || playableBounds.height <= 0)
            return Invalid("Playable bounds must have positive dimensions.", out error);
        for (int i = 0; i < 3; i++)
        {
            if (players[i] == null || spawnPoints[i] == null || cameras[i] == null)
                return Invalid("Player, spawn, or camera reference " + i + " is missing.", out error);
            if (!players[i].gameObject.activeInHierarchy || players[i].GetComponent<Rigidbody2D>() == null
                || players[i].GetComponent<BoxCollider2D>() == null || !players[i].GetComponent<BoxCollider2D>().enabled)
                return Invalid("Each player needs an active object, Rigidbody2D, and enabled BoxCollider2D.", out error);
            if (cameras[i].Target != players[i].transform)
                return Invalid("Each camera must follow its corresponding player.", out error);
            Vector3 position = spawnPoints[i].position;
            if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z) || Mathf.Abs(position.z) > .001f)
                return Invalid("Spawn positions must be finite and at Z = 0.", out error);
            for (int j = 0; j < i; j++)
                if (players[i] == players[j] || cameras[i] == cameras[j] || spawnPoints[i] == spawnPoints[j]
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
            for (int s = 0; s < 3; s++)
            {
                Vector2 center = (Vector2)spawnPoints[s].position + offset;
                if (center.x - extent.x < playableBounds.xMin || center.x + extent.x > playableBounds.xMax
                    || center.y - extent.y < playableBounds.yMin || center.y + extent.y > playableBounds.yMax)
                    return Invalid("Spawn " + spawnPoints[s].name + " does not fit inside the playable bounds.", out error);
                foreach (var hit in Physics2D.OverlapBoxAll(center, size, angle))
                    if (!IsPlayerCollider(hit))
                        return Invalid("Spawn " + spawnPoints[s].name + " overlaps " + hit.name + ".", out error);
                // Conservative footprint separation works for all player sizes and permutations.
                for (int other = 0; other < 3; other++) if (other != s)
                    for (int q = 0; q < 3; q++) if (q != p)
                    {
                        Bounds otherBounds = players[q].GetComponent<BoxCollider2D>().bounds;
                        Vector2 otherOffset = (Vector2)otherBounds.center - (Vector2)players[q].transform.position;
                        Vector2 delta = center - ((Vector2)spawnPoints[other].position + otherOffset);
                        if (Mathf.Abs(delta.x) < extent.x + otherBounds.extents.x
                            && Mathf.Abs(delta.y) < extent.y + otherBounds.extents.y)
                            return Invalid("Spawn footprints overlap.", out error);
                    }
            }
        }
        return true;
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
