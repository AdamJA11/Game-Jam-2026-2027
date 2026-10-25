using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Phase4bSceneSetup
{
    [MenuItem("Game Jam/Set Up Phase 4b Apex Transformation")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before Phase 4b setup.");
        Scene scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var managers = roots.SelectMany(root => root.GetComponentsInChildren<HuntRoundManager>(true)).ToArray();
        var spawners = roots.SelectMany(root => root.GetComponentsInChildren<RandomPlayerSpawns>(true)).ToArray();
        if (string.IsNullOrEmpty(scene.path) || managers.Length != 1 || spawners.Length != 1)
            throw new InvalidOperationException("Open a saved gameplay scene with exactly one configured hunt manager and spawn controller.");
        var manager = managers[0];
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        var players = new PlayerCombatant[3];
        var outlines = new SpriteRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            players[i] = manager.GetPlayer(i);
            var playerProperties = new SerializedObject(players[i]);
            var aura = (GameObject)playerProperties.FindProperty("apexAura").objectReferenceValue;
            outlines[i] = aura != null ? aura.GetComponent<SpriteRenderer>() : null;
            var body = players[i].GetComponent<SpriteRenderer>();
            if (players[i].gameObject.scene != scene || outlines[i] == null || body.sprite == null || body.sharedMaterial == null
                || body.drawMode != SpriteDrawMode.Simple || body.sprite.bounds.center.sqrMagnitude > .00001f)
                throw new InvalidOperationException("Each player needs its existing aura and a centered Simple square sprite with a shared material.");
            var existingBar = players[i].transform.Find("ApexTransformBar");
            if (existingBar != null && (existingBar.GetComponent<SpriteRenderer>() == null
                || (existingBar.Find("Fill") != null && existingBar.Find("Fill").GetComponent<SpriteRenderer>() == null)))
                throw new InvalidOperationException("Existing ApexTransformBar/Fill objects need SpriteRenderers. Rename unrelated objects before setup.");
        }
        var existingRing = manager.transform.Find("ApexWarningRing");
        if (existingRing != null && existingRing.GetComponent<LineRenderer>() == null)
            throw new InvalidOperationException("Existing ApexWarningRing needs a LineRenderer; rename an unrelated object before setup.");
        Rect bounds = new SerializedObject(spawners[0]).FindProperty("playableBounds").rectValue;
        if (float.IsNaN(bounds.x) || float.IsNaN(bounds.y) || float.IsInfinity(bounds.x) || float.IsInfinity(bounds.y)
            || float.IsNaN(bounds.width) || float.IsNaN(bounds.height) || float.IsInfinity(bounds.width) || float.IsInfinity(bounds.height)
            || bounds.width <= 0 || bounds.height <= 0)
            throw new InvalidOperationException("Set finite positive playable bounds on the spawn controller first.");

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Set Up Phase 4b Apex Transformation");
        var backgrounds = new SpriteRenderer[3];
        var fills = new SpriteRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            var source = players[i].GetComponent<SpriteRenderer>();
            backgrounds[i] = SpriteChild(players[i].transform, "ApexTransformBar", source, new Color(.18f, .18f, .22f, 1));
            fills[i] = SpriteChild(backgrounds[i].transform, "Fill", source, new Color(1, .188f, .188f, 1));
            Undo.RecordObjects(new UnityEngine.Object[] { backgrounds[i], fills[i] }, "Hide transformation bars");
            backgrounds[i].enabled = fills[i].enabled = false;
        }
        LineRenderer ring;
        if (existingRing == null)
        {
            var obj = new GameObject("ApexWarningRing");
            Undo.RegisterCreatedObjectUndo(obj, "Create warning ring");
            obj.transform.SetParent(manager.transform, false);
            obj.layer = players[0].gameObject.layer;
            ring = Undo.AddComponent<LineRenderer>(obj);
            ring.sharedMaterial = players[0].GetComponent<SpriteRenderer>().sharedMaterial;
            ring.widthCurve = AnimationCurve.Constant(0, 1, 1);
            ring.alignment = LineAlignment.View;
            ring.textureMode = LineTextureMode.Stretch;
            ring.sortingLayerID = players[0].GetComponent<SpriteRenderer>().sortingLayerID;
            ring.sortingOrder = players.Max(player => player.GetComponent<SpriteRenderer>().sortingOrder) + 1;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.enabled = false;
        }
        else
        {
            ring = existingRing.GetComponent<LineRenderer>();
            Undo.RecordObject(ring, "Configure warning ring");
            if (ring.sharedMaterial == null) ring.sharedMaterial = players[0].GetComponent<SpriteRenderer>().sharedMaterial;
            ring.enabled = false;
        }
        var presentation = manager.GetComponent<ApexTransformationPresentation>();
        if (presentation == null) presentation = Undo.AddComponent<ApexTransformationPresentation>(manager.gameObject);
        Undo.RecordObject(presentation, "Configure transformation presentation");
        presentation.Configure(manager, players, outlines, backgrounds, fills, ring, bounds);
        if (!presentation.enabled || !presentation.ValidateConfiguration(out error))
        {
            Undo.RevertAllDownToGroup(group);
            throw new InvalidOperationException("Phase 4b setup rolled back. Enable/configure the presentation and retry. " + error);
        }
        EditorUtility.SetDirty(presentation);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("PHASE4B_SETUP_OK: black/red Apex, overhead progress bars, and one expanding warning ring configured. Ctrl+S, then follow docs/4b steps.md. Existing timing and tuning preserved.");
    }

    private static SpriteRenderer SpriteChild(Transform parent, string name, SpriteRenderer source, Color color)
    {
        var child = parent.Find(name);
        if (child != null) return child.GetComponent<SpriteRenderer>();
        var obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Create transformation bar");
        obj.transform.SetParent(parent, false);
        obj.layer = source.gameObject.layer;
        var renderer = Undo.AddComponent<SpriteRenderer>(obj);
        renderer.sprite = source.sprite;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.color = color;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + (name == "Fill" ? 11 : 10);
        renderer.enabled = false;
        return renderer;
    }

    [MenuItem("Game Jam/Check Phase 4b Transformation Rules")]
    public static void Check()
    { Debug.Log("PHASE4B_RULES_OK: " + Phase4bTests.Run() + " assertions. Appearance and wave direction need user-run Play-mode checks."); }
}
