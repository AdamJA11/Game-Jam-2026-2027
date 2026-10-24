using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Explicit Play-mode smoke check. Stop Play afterwards to reset the scene.
public static class Phase1PlayCheck
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Game Jam/Check Phase 2 (while playing)")]
    [MenuItem("Game Jam/Check Phase 1 (while playing)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        var movement = UnityEngine.Object.FindObjectOfType<TopDownPlayerMovement>();
        var target = GameObject.Find("SecondCharacter");
        if (movement == null || target == null)
            throw new InvalidOperationException("Start Phase2 with both characters active first.");

        var body = movement.GetComponent<Rigidbody2D>();
        var oldMode = Physics2D.simulationMode;
        bool wasEnabled = movement.enabled;
        var directionField = typeof(TopDownPlayerMovement).GetField("direction", InstancePrivate);
        var stepMethod = typeof(TopDownPlayerMovement).GetMethod("FixedUpdate", InstancePrivate);
        var inputMethod = typeof(TopDownPlayerMovement).GetMethod("ReadDirection", BindingFlags.Static | BindingFlags.NonPublic);
        var initialPosition = body.position;
        bool targetWasActive = target.activeSelf;
        try
        {
            movement.enabled = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
            body.position = Vector2.zero;
            body.velocity = Vector2.zero;
            Physics2D.SyncTransforms();

            Action<Vector2, int> step = (input, count) =>
            {
                directionField.SetValue(movement, (Vector2)inputMethod.Invoke(null, new object[] { input.x, input.y }));
                for (int i = 0; i < count; i++)
                {
                    stepMethod.Invoke(movement, null);
                    Physics2D.Simulate(Time.fixedDeltaTime);
                }
            };
            Action<Vector2> reset = position =>
            {
                body.position = position;
                body.velocity = Vector2.zero;
                Physics2D.SyncTransforms();
            };

            step(Vector2.right, 1);
            Require(body.velocity.x > 0f && body.velocity.x < 4f, "Acceleration is gradual.");
            step(Vector2.right, 10);
            float straightSpeed = body.velocity.magnitude;
            Require(Mathf.Abs(straightSpeed - 4f) < 0.01f, "Full straight speed is 4.");
            foreach (var diagonal in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
            {
                reset(Vector2.zero);
                step(diagonal, 10);
                Require(Mathf.Sign(body.velocity.x) == diagonal.x && Mathf.Sign(body.velocity.y) == diagonal.y, "Both diagonal axes respond.");
                Require(Mathf.Abs(body.velocity.magnitude - straightSpeed) < 0.01f, "Diagonal speed equals straight speed.");
            }
            step(Vector2.up, 10);
            Require(Mathf.Abs(body.velocity.x) < 0.01f && Mathf.Abs(body.velocity.y - 4f) < 0.01f, "Releasing horizontal input turns upward.");
            step(Vector2.zero, 1);
            Require(body.velocity.magnitude > 0f && body.velocity.magnitude < 4f, "Deceleration is gradual.");
            step(Vector2.zero, 4);
            Require(body.velocity.sqrMagnitude < 0.0001f, "Player stops after release.");
            step(Vector2.right, 10);
            step(Vector2.left, 1);
            Require(body.velocity.x > 0f && body.velocity.x < 4f, "Reversal does not instantly flip velocity.");
            step(Vector2.left, 12);
            Require(Mathf.Abs(body.velocity.x + 4f) < 0.01f, "Reversal reaches full speed.");

            reset(new Vector2(6.5f, 0f));
            step(new Vector2(1, 1), 65);
            Require(body.position.x > 7f && body.position.x < 7.25f && body.position.y > 2f, "Diagonal movement slides along right wall.");
            step(new Vector2(1, 1), 80);
            Require(body.position.x < 7.25f && body.position.y > 3.75f && body.position.y < 4.05f, "Corner contains the player.");
            step(Vector2.left, 220);
            Require(body.position.x < -7f && body.position.x > -7.25f, "Left wall blocks movement.");
            step(Vector2.down, 140);
            Require(body.position.y < -3.75f && body.position.y > -4.05f, "Bottom wall blocks movement.");

            reset((Vector2)target.transform.position - Vector2.one);
            step(Vector2.one, 25);
            Require(!target.activeSelf, "Diagonal contact hides the second character.");
            Debug.Log("PHASE2_PLAY_CHECK_OK: acceleration, four diagonals, equal speed, turning, deceleration, reversal, walls, sliding, corner, touch.");
        }
        finally
        {
            Physics2D.simulationMode = oldMode;
            body.position = initialPosition;
            body.velocity = Vector2.zero;
            target.SetActive(targetWasActive);
            movement.enabled = wasEnabled;
            EditorApplication.isPlaying = false;
        }
    }

    private static void Require(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException("PHASE2_PLAY_CHECK_FAILED: " + message);
    }
}
