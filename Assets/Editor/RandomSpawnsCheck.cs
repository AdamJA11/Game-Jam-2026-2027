using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RandomSpawnsCheck
{
    [MenuItem("Game Jam/Check Phase 3a (while playing)")]
    public static void Run() { SplitScreenCheck.Run(); }

    public static void ValidateCurrent(bool requireCloseCamera = true)
    {
        var spawner = UnityEngine.Object.FindObjectOfType<RandomPlayerSpawns>();
        Require(spawner != null && spawner.HasSpawned, "Startup shuffle completed.");
        var permutations = new HashSet<string>();
        for (int first = 0; first < 3; first++) for (int second = 0; second < 2; second++)
        {
            int call = 0;
            var result = RandomPlayerSpawns.CreateAssignment(max => call++ == 0 ? first : second);
            Require(result.Distinct().Count() == 3 && result.All(i => i >= 0 && i < 3), "Permutation is a bijection.");
            permutations.Add(string.Join(",", result));
        }
        Require(permutations.Count == 6, "All six equally weighted draw combinations produce different assignments.");
        var players = new[] { GameObject.Find("Player").GetComponent<TopDownPlayerMovement>(), GameObject.Find("Player2").GetComponent<TopDownPlayerMovement>(), GameObject.Find("Player3").GetComponent<TopDownPlayerMovement>() };
        var cameras = Enumerable.Range(1, 3).Select(i => GameObject.Find("Player" + i + "Camera").GetComponent<LocalPlayerCameraFollow>()).ToArray();
        var points = Enumerable.Range(0, 3).Select(spawner.GetSpawnPoint).ToArray();
        var assignments = Enumerable.Range(0, 3).Select(spawner.GetAssignedSpawn).ToArray();
        Require(assignments.Distinct().Count() == 3, "Each spawn is used once.");
        for (int i = 0; i < 3; i++)
        {
            Require(Vector2.Distance(players[i].transform.position, points[assignments[i]].position) < .01f, "Player starts at assigned point.");
            Require(players[i].GetComponent<Rigidbody2D>().velocity.sqrMagnitude < .0001f, "Startup velocity is zero.");
            if (requireCloseCamera)
                Require(Vector2.Distance(cameras[i].transform.position, players[i].transform.position) < .01f, "Camera is already at assigned player.");
        }
        Require(spawner.ValidateConfiguration(out string error), error);
        Require(spawner.TrySpawn(out error), error);
        spawner.enabled = false; spawner.enabled = true;
        if (requireCloseCamera) { cameras[0].enabled = false; cameras[0].enabled = true; }
        Require(Enumerable.Range(0, 3).All(i => spawner.GetAssignedSpawn(i) == assignments[i]
            && Vector2.Distance(players[i].transform.position, points[assignments[i]].position) < .01f), "Repeated calls and re-enabling do not reshuffle.");

        // Inactive validation fixture: Awake never runs and no live scene configuration is changed.
        var fixture = new GameObject("SpawnValidationFixture");
        fixture.SetActive(false);
        var samplePoint = new GameObject("BlockedTestPoint");
        var validation = fixture.AddComponent<RandomPlayerSpawns>();
        try
        {
            var bad = (Transform[])points.Clone(); bad[0] = null;
            validation.Configure(players, bad, cameras);
            Require(!validation.TrySpawn(out error), "Missing point rejected before teleport.");
            bad[0] = points[1];
            validation.Configure(players, bad, cameras);
            Require(!validation.ValidateConfiguration(out error), "Duplicate point rejected.");
            var badPlayers = (TopDownPlayerMovement[])players.Clone(); badPlayers[0] = badPlayers[1];
            validation.Configure(badPlayers, points, cameras);
            Require(!validation.ValidateConfiguration(out error), "Duplicate player rejected.");
            samplePoint.transform.position = new Vector3(-6, 0, 0);
            bad[0] = samplePoint.transform;
            validation.Configure(players, bad, cameras);
            Require(!validation.ValidateConfiguration(out error), "Obstacle overlap rejected using collider footprint.");
            samplePoint.transform.position = new Vector3(19.9f, -8, 0);
            Require(!validation.ValidateConfiguration(out error), "Footprint outside playable bounds rejected.");
            samplePoint.transform.position = points[1].position + new Vector3(.1f, 0, 0);
            Require(!validation.ValidateConfiguration(out error), "Overlapping spawn footprints rejected.");
            Require(Enumerable.Range(0, 3).All(i => Vector2.Distance(players[i].transform.position, points[assignments[i]].position) < .01f), "Invalid configurations do not move players.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fixture);
            UnityEngine.Object.DestroyImmediate(samplePoint);
        }
        Debug.Log("PHASE3A_CHECK_OK: six permutations, unique startup positions, zero velocity, camera snap, idempotence, invalid configuration and footprint checks.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("PHASE3A_CHECK_FAILED: " + message);
    }
}
