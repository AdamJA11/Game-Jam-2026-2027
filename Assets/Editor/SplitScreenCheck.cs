using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SplitScreenCheck
{
    [MenuItem("Game Jam/Check Phase 3 (while playing)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        var introduction = UnityEngine.Object.FindObjectOfType<CameraIntroduction>();
        if (introduction != null && introduction.IsRunning)
        {
            Debug.LogWarning("Wait for the Phase 3b opening to finish, then run the gameplay check before moving.");
            return;
        }
        if (UnityEngine.Object.FindObjectOfType<RandomPlayerSpawns>() != null)
            RandomSpawnsCheck.ValidateCurrent();
        var follows = UnityEngine.Object.FindObjectsOfType<LocalPlayerCameraFollow>().OrderBy(f => f.name).ToArray();
        Require(follows.Length == 3 && Camera.allCamerasCount == 3, "Exactly three active gameplay cameras.");
        Require(UnityEngine.Object.FindObjectsOfType<AudioListener>().Count(l => l.enabled) == 1, "Exactly one listener.");
        Require(GameObject.Find("ControlsPanel").GetComponent<Image>().color.a == 1f, "Controls panel is opaque.");
        Require(GameObject.Find("P3Controls").GetComponent<Text>().text.Contains("I up"), "Third player's controls are displayed.");
        var positions = follows.Select(f => f.Target.position).ToArray();
        var bounds = follows.Select(f => f.MapBounds).ToArray();
        var players = follows.Select(f => f.Target.GetComponent<TopDownPlayerMovement>()).ToArray();
        var target = GameObject.Find("SecondCharacter");
        var oldMode = Physics2D.simulationMode;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var direction = typeof(TopDownPlayerMovement).GetField("direction", flags);
        var step = typeof(TopDownPlayerMovement).GetMethod("FixedUpdate", flags);
        var combat = UnityEngine.Object.FindObjectOfType<HuntRoundManager>();
        if (combat != null && combat.Rules.State != HuntRules.Phase.Opening && combat.Rules.State != HuntRules.Phase.TriangleHunt)
            throw new InvalidOperationException("Restart the scene before running regression checks after a kill.");
        bool oldCombatSuspended = combat != null && combat.SetVerificationSuspended(true);
        try
        {
            Rect[] rects = { new Rect(0, .5f, .5f, .5f), new Rect(.5f, .5f, .5f, .5f), new Rect(0, 0, .5f, .5f) };
            for (int i = 0; i < 3; i++)
            {
                var camera = follows[i].GetComponent<Camera>();
                Require(camera.rect == rects[i] && camera.orthographic && camera.orthographicSize == 3f, "Equal configured views.");
                Require(follows[i].Target.name == (i == 0 ? "Player" : "Player" + (i + 1)), "Correct follow assignment.");
                for (int j = 0; j < 3; j++) if (j != i)
                {
                    Vector3 point = camera.WorldToViewportPoint(follows[j].Target.position);
                    Require(point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1, "Opponents start outside each local view.");
                }
                var otherPositions = follows.Select(f => f.transform.position).ToArray();
                follows[i].Target.position += new Vector3(1, 1, 0);
                follows[i].FollowNow();
                Require(Vector2.Distance(follows[i].transform.position, follows[i].Target.position) < .01f, "Independent follow.");
                for (int j = 0; j < 3; j++) if (j != i) Require(follows[j].transform.position == otherPositions[j], "Other cameras stay still.");
                foreach (float aspect in new[] { 16f / 9f, 16f / 10f })
                {
                    camera.aspect = aspect;
                    foreach (var corner in new[] { new Vector3(-100, -100), new Vector3(-100, 100), new Vector3(100, -100), new Vector3(100, 100) })
                    {
                        follows[i].Target.position = corner;
                        follows[i].FollowNow();
                        Vector3 center = follows[i].transform.position;
                        Require(center.x - 3 * aspect >= bounds[i].xMin - .01f && center.x + 3 * aspect <= bounds[i].xMax + .01f
                            && center.y - 3 >= bounds[i].yMin - .01f && center.y + 3 <= bounds[i].yMax + .01f, "Corner bounds at both aspect ratios.");
                    }
                }
                follows[i].Configure(follows[i].Target, new Rect(-1, -1, 2, 2));
                Require((Vector2)follows[i].transform.position == Vector2.zero, "Small map centers correctly.");
                follows[i].Configure(follows[i].Target, bounds[i]);
                Vector3 held = follows[i].transform.position;
                follows[i].Target.gameObject.SetActive(false);
                follows[i].Target.position = Vector3.zero;
                follows[i].FollowNow();
                Require(follows[i].transform.position == held, "Unavailable target holds camera.");
                follows[i].Target.gameObject.SetActive(true);
                follows[i].Target.position = positions[i];
                camera.ResetAspect();
                follows[i].FollowNow();
            }

            // Camera tests teleport Transforms; synchronize before testing Rigidbody movement.
            Physics2D.SyncTransforms();
            foreach (var player in players) player.enabled = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int i = 0; i < 3; i++)
            {
                var body = players[i].GetComponent<Rigidbody2D>();
                body.position = new Vector2(19, -8);
                body.velocity = Vector2.zero;
                direction.SetValue(players[i], Vector2.right);
                Physics2D.SyncTransforms();
                for (int n = 0; n < 40; n++) { step.Invoke(players[i], null); Physics2D.Simulate(Time.fixedDeltaTime); }
                Require(body.position.x > 19.5f && body.position.x < 19.8f, "Enlarged map wall for " + players[i].name + " at " + body.position);
                target.SetActive(true);
                body.position = (Vector2)target.transform.position - Vector2.one;
                body.velocity = Vector2.zero;
                direction.SetValue(players[i], Vector2.one.normalized);
                Physics2D.SyncTransforms();
                for (int n = 0; n < 25; n++) { step.Invoke(players[i], null); Physics2D.Simulate(Time.fixedDeltaTime); }
                Require(!target.activeSelf, "Every player can trigger shared disappearance.");
                body.position = positions[i];
                body.velocity = Vector2.zero;
            }
            Debug.Log("PHASE3_CHECK_OK: three viewports, labels, single listener, independent following, hidden distant opponents, bounds at 16:9/16:10, small map, unavailable target, walls and shared touch.");
        }
        finally
        {
            if (combat != null) combat.SetVerificationSuspended(oldCombatSuspended);
            Physics2D.simulationMode = oldMode;
            for (int i = 0; i < follows.Length; i++)
            {
                follows[i].Target.gameObject.SetActive(true);
                follows[i].Target.position = positions[i];
                follows[i].Configure(follows[i].Target, bounds[i]);
                follows[i].GetComponent<Camera>().ResetAspect();
                players[i].enabled = true;
            }
            target.SetActive(true);
            EditorApplication.isPlaying = false;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("PHASE3_CHECK_FAILED: " + message);
    }
}
