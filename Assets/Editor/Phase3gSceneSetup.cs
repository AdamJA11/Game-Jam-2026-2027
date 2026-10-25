using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Phase3gSceneSetup
{
    [MenuItem("Game Jam/Set Up Phase 3g Death Camera Shake")]
    public static void Setup()
    {
        Phase3cSceneSetup.RequireEditMode();
        var scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save the gameplay scene before setup.");
        var manager = Phase3cSceneSetup.InScene<HuntRoundManager>();
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LocalPlayerCameraFollow>(true)).ToArray();
        var follows = new LocalPlayerCameraFollow[3];
        for (int i = 0; i < 3; i++)
        {
            var matches = all.Where(follow => follow.Target == manager.GetPlayer(i).transform).ToArray();
            if (matches.Length != 1 || !matches[0].enabled || !matches[0].gameObject.activeInHierarchy)
                throw new InvalidOperationException("Each player needs one active, enabled follow camera.");
            follows[i] = matches[0];
            var camera = follows[i].GetComponent<Camera>();
            if (!camera.enabled || !camera.orthographic || !follows[i].ValidateDeathShake(out error))
                throw new InvalidOperationException("Player " + (i + 1) + " needs an enabled orthographic camera and valid shake settings. " + error);
        }
        Undo.RecordObjects(follows.Cast<UnityEngine.Object>().ToArray(), "Configure death camera shake");
        foreach (var follow in follows) { follow.ConfigureDeathShake(); EditorUtility.SetDirty(follow); }
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("PHASE3G_SETUP_OK: victim-only death shake configured on three existing cameras. Ctrl+S, then repeat on the other map. Tuned settings preserved; see docs/3g steps.md.");
    }

    [MenuItem("Game Jam/Check Phase 3g Shake Rules")]
    public static void Check()
    { Debug.Log("PHASE3G_RULES_OK: " + Phase3gTests.Run() + " assertions. Live camera ownership, shake comfort, and UI behavior need Play-mode checks."); }
}
