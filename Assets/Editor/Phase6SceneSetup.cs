using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Phase6SceneSetup
{
    [MenuItem("Game Jam/Set Up Phase 6 Kill Effects")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before Phase 6 setup.");
        Scene scene = SceneManager.GetActiveScene();
        if ((scene.path != MapRotation.MapAPath && scene.path != MapRotation.MapBPath) || string.IsNullOrEmpty(scene.path))
            throw new InvalidOperationException("Open the saved SplitScreenTest or SplitScreenMapB scene first.");
        var roots = scene.GetRootGameObjects();
        var managers = roots.SelectMany(root => root.GetComponentsInChildren<HuntRoundManager>(true)).ToArray();
        var overlays = roots.SelectMany(root => root.GetComponentsInChildren<RoundEndOverlay>(true)).ToArray();
        if (managers.Length != 1 || overlays.Length != 1)
            throw new InvalidOperationException("Configure Phase 4 and Phase 5 first: exactly one round manager and result overlay are required.");
        var manager = managers[0];
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        var properties = new SerializedObject(manager);
        var playerRefs = properties.FindProperty("players");
        var players = new PlayerCombatant[3];
        var overlayProperties = new SerializedObject(overlays[0]);
        if (overlayProperties.FindProperty("roundManager").objectReferenceValue != manager)
            throw new InvalidOperationException("The result overlay must reference this scene's round manager.");
        float delay = overlays[0].ResultRevealDelay;
        if (float.IsNaN(delay) || float.IsInfinity(delay) || delay < 0)
            throw new InvalidOperationException("Set Result Reveal Delay to a finite non-negative value (default 0.6).");
        // Validate everything before adding components. Keep all existing tuning and scene content.
        for (int i = 0; i < players.Length; i++)
        {
            players[i] = (PlayerCombatant)playerRefs.GetArrayElementAtIndex(i).objectReferenceValue;
            if (players[i].gameObject.scene != scene)
                throw new InvalidOperationException("All three combatants must belong to SplitScreenTest.");
            var renderer = players[i].GetComponent<SpriteRenderer>();
            if (renderer.sprite == null || renderer.drawMode != SpriteDrawMode.Simple)
                throw new InvalidOperationException("Each player needs its square sprite in Simple draw mode.");
            var effect = players[i].GetComponent<PlayerDeathBurst>();
            if (effect != null && (!effect.enabled || !effect.ValidateConfiguration(out error)))
                throw new InvalidOperationException("Enable and configure the existing burst on " + players[i].name + ". " + error);
            if (delay > (effect != null ? effect.Lifetime : .9f))
                throw new InvalidOperationException("Result Reveal Delay must be no greater than each burst lifetime.");
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Set Up Phase 6 Kill Effects");
        foreach (var player in players)
            if (player.GetComponent<PlayerDeathBurst>() == null) Undo.AddComponent<PlayerDeathBurst>(player.gameObject);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("PHASE6_SETUP_OK: three death bursts configured. Save the scene (Ctrl+S), then follow docs/6 steps.md. Existing effect tuning and result delay preserved.");
    }

    [MenuItem("Game Jam/Check Phase 6 Effect Rules")]
    public static void Check()
    { Debug.Log("PHASE6_RULES_OK: " + Phase6Tests.Run() + " assertions. Visual/physics checks still require Play mode."); }
}
