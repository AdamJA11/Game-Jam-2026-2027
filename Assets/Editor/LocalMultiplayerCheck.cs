using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class LocalMultiplayerCheck
{
    [MenuItem("Game Jam/Check Local Two Players (while playing)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        var first = GameObject.Find("Player").GetComponent<TopDownPlayerMovement>();
        var second = GameObject.Find("Player2").GetComponent<TopDownPlayerMovement>();
        var target = GameObject.Find("SecondCharacter");
        var bodies = new[] { first.GetComponent<Rigidbody2D>(), second.GetComponent<Rigidbody2D>() };
        var players = new[] { first, second };
        var positions = new[] { bodies[0].position, bodies[1].position };
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var controls = typeof(TopDownPlayerMovement).GetField("controls", flags);
        var direction = typeof(TopDownPlayerMovement).GetField("direction", flags);
        var step = typeof(TopDownPlayerMovement).GetMethod("FixedUpdate", flags);
        var oldMode = Physics2D.simulationMode;
        try
        {
            Require((TopDownPlayerMovement.ControlScheme)controls.GetValue(first) == TopDownPlayerMovement.ControlScheme.WASD, "Player one uses WASD.");
            Require((TopDownPlayerMovement.ControlScheme)controls.GetValue(second) == TopDownPlayerMovement.ControlScheme.ArrowKeys, "Player two uses arrows.");
            first.enabled = second.enabled = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int active = 0; active < 2; active++)
            {
                bodies[0].position = new Vector2(-3, 0);
                bodies[1].position = new Vector2(-3, -2);
                bodies[0].velocity = bodies[1].velocity = Vector2.zero;
                Physics2D.SyncTransforms();
                Vector2 idleStart = bodies[1 - active].position;
                direction.SetValue(players[active], Vector2.one.normalized);
                for (int i = 0; i < 10; i++) { step.Invoke(players[active], null); Physics2D.Simulate(Time.fixedDeltaTime); }
                Require((bodies[1 - active].position - idleStart).sqrMagnitude < 0.0001f, "Moving one player leaves the other stationary.");
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
            Debug.Log("LOCAL_TWO_PLAYER_CHECK_OK: distinct controls, independent bodies, diagonal speed, walls, and both players trigger disappearance.");
        }
        finally
        {
            Physics2D.simulationMode = oldMode;
            for (int i = 0; i < 2; i++) { bodies[i].position = positions[i]; bodies[i].velocity = Vector2.zero; players[i].enabled = true; }
            target.SetActive(true);
            EditorApplication.isPlaying = false;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("LOCAL_TWO_PLAYER_CHECK_FAILED: " + message);
    }
}
