using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Phase3fSceneSetup
{
    [MenuItem("Game Jam/Set Up Phase 3f Random Items")]
    public static void Setup()
    {
        Phase3cSceneSetup.RequireEditMode();
        var scene = SceneManager.GetActiveScene();
        var manager = Phase3cSceneSetup.InScene<HuntRoundManager>();
        var spawner = Phase3cSceneSetup.InScene<RandomPlayerSpawns>();
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        var allObjects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var blue = allObjects.FirstOrDefault(obj => obj.name == "SecondCharacter");
        var canvas = allObjects.FirstOrDefault(obj => obj.name == "SplitScreenUI");
        if (blue == null || canvas == null || blue.GetComponent<SpriteRenderer>() == null || blue.GetComponent<BoxCollider2D>() == null || !blue.GetComponent<BoxCollider2D>().isTrigger)
            throw new InvalidOperationException("Keep the cyan SecondCharacter with its BoxCollider2D trigger and SplitScreenUI in this scene.");
        var views = new Camera[3];
        for (int i = 0; i < 3; i++)
            views[i] = allObjects.Select(obj => obj.GetComponent<LocalPlayerCameraFollow>()).Where(follow => follow != null).Single(follow => follow.Target == manager.GetPlayer(i).transform).GetComponent<Camera>();
        var usedLayers = new HashSet<int>(allObjects.Select(obj => obj.gameObject.layer));
        var freeLayers = Enumerable.Range(8, 24).Reverse().Where(layer => !usedLayers.Contains(layer) && string.IsNullOrEmpty(LayerMask.LayerToName(layer))).Take(3).ToArray();
        if (freeLayers.Length != 3) throw new InvalidOperationException("Three unused unnamed layers are needed for private trails. Setup does not change Project Settings.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Set Up Phase 3f Random Items");
        try
        {
            var pickup = blue.GetComponent<RandomItemPickup>();
            if (pickup == null) pickup = Undo.AddComponent<RandomItemPickup>(blue.gameObject);
            var legacy = blue.GetComponent<DisappearOnPlayerTouch2D>();
            if (legacy != null) { Undo.RecordObject(legacy, "Replace disappearance-only pickup behavior"); legacy.enabled = false; }
            var trails = manager.GetComponent<TrackerTrails>();
            if (trails == null) trails = Undo.AddComponent<TrackerTrails>(manager.gameObject);
            if (!trails.ValidateLayers(out error)) { Undo.RecordObject(trails, "Assign unused trail layers"); trails.ConfigureLayers(freeLayers); }
            var labels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                string name = "P" + (i + 1) + "Items";
                var obj = canvas.Find(name);
                if (obj == null)
                {
                    var created = new GameObject(name, typeof(RectTransform), typeof(Text)); Undo.RegisterCreatedObjectUndo(created, "Create local item feedback");
                    created.transform.SetParent(canvas, false); obj = created.transform;
                    var rect = created.GetComponent<RectTransform>();
                    float x = i == 1 ? .5f : 0, y = i == 2 ? 0 : .5f;
                    rect.anchorMin = new Vector2(x + .01f, y + .235f); rect.anchorMax = new Vector2(x + .49f, y + .325f); rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var text = created.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); text.fontSize = 22;
                    text.color = new Color(.65f, .95f, 1, 1); text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false; text.text = string.Empty;
                }
                labels[i] = obj.GetComponent<Text>();
                if (labels[i] == null) throw new InvalidOperationException(name + " must have a Text component.");
            }
            var coordinator = manager.GetComponent<RandomItemCoordinator>();
            if (coordinator == null) coordinator = Undo.AddComponent<RandomItemCoordinator>(manager.gameObject);
            var pickups = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RandomItemPickup>(true)).ToArray();
            Undo.RecordObject(coordinator, "Configure random items");
            coordinator.Configure(manager, pickups, views, labels, trails, spawner.PlayableBounds);
            if (!coordinator.enabled || !coordinator.ValidateConfiguration(out error)) throw new InvalidOperationException(error ?? "Enable the item coordinator and retry.");
            EditorUtility.SetDirty(coordinator); EditorUtility.SetDirty(trails);
            Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("PHASE3F_SETUP_OK: cyan square now grants Speed / Tracker / moving Decoy. Ctrl+S. Private trail layers allocated without changing Project Settings. See docs/3f steps.md.");
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }
}
