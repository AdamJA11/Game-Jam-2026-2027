using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class LocalMultiplayerCheck
{
    [MenuItem("Game Jam/Check Local Two Players (while playing)")]
    [MenuItem("Game Jam/Check Local Three Players (while playing)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        var first = GameObject.Find("Player").GetComponent<TopDownPlayerMovement>();
        var second = GameObject.Find("Player2").GetComponent<TopDownPlayerMovement>();
        var third = GameObject.Find("Player3").GetComponent<TopDownPlayerMovement>();
        var target = GameObject.Find("SecondCharacter");
        var bodies = new[] { first.GetComponent<Rigidbody2D>(), second.GetComponent<Rigidbody2D>(), third.GetComponent<Rigidbody2D>() };
        var players = new[] { first, second, third };
        var positions = new[] { bodies[0].position, bodies[1].position, bodies[2].position };
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var controls = typeof(TopDownPlayerMovement).GetField("controls", flags);
        var direction = typeof(TopDownPlayerMovement).GetField("direction", flags);
        var step = typeof(TopDownPlayerMovement).GetMethod("FixedUpdate", flags);
        var oldMode = Physics2D.simulationMode;
        try
        {
            Require((TopDownPlayerMovement.ControlScheme)controls.GetValue(first) == TopDownPlayerMovement.ControlScheme.WASD, "Player one uses WASD.");
            Require((TopDownPlayerMovement.ControlScheme)controls.GetValue(second) == TopDownPlayerMovement.ControlScheme.ArrowKeys, "Player two uses arrows.");
            Require((TopDownPlayerMovement.ControlScheme)controls.GetValue(third) == TopDownPlayerMovement.ControlScheme.IJKL, "Player three uses IJKL.");
            first.enabled = second.enabled = third.enabled = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int active = 0; active < 3; active++)
            {
                bodies[0].position = new Vector2(-3, 0);
                bodies[1].position = new Vector2(-3, -2);
                bodies[2].position = new Vector2(0, -2);
                bodies[0].velocity = bodies[1].velocity = bodies[2].velocity = Vector2.zero;
                Physics2D.SyncTransforms();
                var idleStarts = new[] { bodies[0].position, bodies[1].position, bodies[2].position };
                direction.SetValue(players[active], Vector2.one.normalized);
                for (int i = 0; i < 10; i++) { step.Invoke(players[active], null); Physics2D.Simulate(Time.fixedDeltaTime); }
                for (int idle = 0; idle < 3; idle++) if (idle != active) Require((bodies[idle].position - idleStarts[idle]).sqrMagnitude < 0.0001f, "Moving one player leaves the others stationary.");
                Require(bodies[active].velocity.x > 0 && bodies[active].velocity.y > 0 && Mathf.Abs(bodies[active].velocity.magnitude - 4) < 0.01f, "Each player has normalized diagonal movement.");
                bodies[active].position = new Vector2(6.5f, -2);
                bodies[active].velocity = Vector2.zero;
                direction.SetValue(players[active], Vector2.right);
                Physics2D.SyncTransforms();
                for (int i = 0; i < 50; i++) { step.Invoke(players[active], null); Physics2D.Simulate(Time.fixedDeltaTime); }
                Require(bodies[active].position.x > 7 && bodies[active].position.x < 7.25f, "Each player collides with the walls.");
                target.SetActive(true);
                bodies[active].position = (Vector2)target.transform.position - Vector2.one;
                bodies[active].velocity = Vector2.zero;
                direction.SetValue(players[active], Vector2.one.normalized);
                Physics2D.SyncTransforms();
                for (int i = 0; i < 25; i++) { step.Invoke(players[active], null); Physics2D.Simulate(Time.fixedDeltaTime); }
                Require(!target.activeSelf, "Either player can touch the cyan target.");
            }
            Debug.Log("LOCAL_THREE_PLAYER_CHECK_OK: distinct controls, independent bodies, diagonal speed, walls, and all three players trigger disappearance.");
        }
        finally
        {
            Physics2D.simulationMode = oldMode;
            for (int i = 0; i < 3; i++) { bodies[i].position = positions[i]; bodies[i].velocity = Vector2.zero; players[i].enabled = true; }
            target.SetActive(true);
            EditorApplication.isPlaying = false;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("LOCAL_THREE_PLAYER_CHECK_FAILED: " + message);
    }
}
