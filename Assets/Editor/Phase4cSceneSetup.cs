using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Phase4cSceneSetup
{
    private const string ClawPath = "Assets/Art/apex/claw.png";

    [MenuItem("Game Jam/Set Up Phase 4c Kill Claws")]
    public static void Setup()
    {
        Phase3cSceneSetup.RequireEditMode();
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save the gameplay scene first.");
        var manager = Phase3cSceneSetup.InScene<HuntRoundManager>();
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var canvas = objects.FirstOrDefault(obj => obj.name == "SplitScreenUI")?.GetComponent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            throw new InvalidOperationException("Keep the existing SplitScreenUI screen-space overlay Canvas.");
        var views = new Camera[3];
        for (int i = 0; i < 3; i++)
        {
            var matches = objects.Select(obj => obj.GetComponent<LocalPlayerCameraFollow>()).Where(follow => follow != null && follow.Target == manager.GetPlayer(i).transform).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Each player needs one matching gameplay camera.");
            views[i] = matches[0].GetComponent<Camera>();
        }
        var importer = AssetImporter.GetAtPath(ClawPath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Keep the claw image at " + ClawPath);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center; settings.spritePivot = Vector2.one * .5f; settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ClawPath);
        if (sprite == null) throw new InvalidOperationException("Claw could not be imported as a Sprite.");

        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Set Up Phase 4c Kill Claws");
        try
        {
            RectTransform root = RectChild(canvas.transform, "KillClawOverlays");
            Undo.RecordObject(root, "Place claws below gameplay HUD");
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero; root.SetAsFirstSibling();
            var masks = new RectTransform[3]; var images = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                masks[i] = RectChild(root, "P" + (i + 1) + "ClawViewport");
                Undo.RecordObject(masks[i], "Align claw viewport");
                masks[i].anchorMin = views[i].rect.min; masks[i].anchorMax = views[i].rect.max;
                masks[i].offsetMin = masks[i].offsetMax = Vector2.zero;
                if (masks[i].GetComponent<RectMask2D>() == null) Undo.AddComponent<RectMask2D>(masks[i].gameObject);
                RectTransform graphic = RectChild(masks[i], "Claw");
                Undo.RecordObject(graphic, "Center claw image");
                graphic.anchorMin = graphic.anchorMax = graphic.pivot = Vector2.one * .5f;
                graphic.anchoredPosition = Vector2.zero; graphic.localScale = Vector3.one;
                images[i] = graphic.GetComponent<Image>();
                if (images[i] == null) images[i] = Undo.AddComponent<Image>(graphic.gameObject);
                Undo.RecordObject(images[i], "Configure translucent claw sprite");
                images[i].sprite = sprite; images[i].type = Image.Type.Simple; images[i].preserveAspect = true;
                images[i].raycastTarget = false; images[i].maskable = true; images[i].color = new Color(1, 1, 1, 0); images[i].enabled = false;
            }
            var presenter = manager.GetComponent<KillClawOverlay>();
            if (presenter == null) presenter = Undo.AddComponent<KillClawOverlay>(manager.gameObject);
            Undo.RecordObject(presenter, "Assign kill claw references"); presenter.Configure(manager, views, masks, images, sprite);
            if (!presenter.enabled || !presenter.ValidateConfiguration(out error)) throw new InvalidOperationException(error ?? "Enable KillClawOverlay before setup.");
            EditorUtility.SetDirty(presenter);
            Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("PHASE4C_SETUP_OK: supplied claw sprite assigned to three masked viewport images. Ctrl+S, then repeat on Map B. Existing numeric tuning preserved; see docs/4c steps.md.");
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }

    private static RectTransform RectChild(Transform parent, string name)
    {
        var found = parent.Find(name);
        if (found != null)
        {
            var existing = found as RectTransform;
            if (existing == null) throw new InvalidOperationException(name + " needs a RectTransform; rename an unrelated object before setup.");
            return existing;
        }
        var obj = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(obj, "Create kill claw UI");
        obj.transform.SetParent(parent, false); return obj.GetComponent<RectTransform>();
    }

    [MenuItem("Game Jam/Check Phase 4c Claw Rules")]
    public static void Check() { Debug.Log("PHASE4C_RULES_OK: " + Phase4cTests.Run() + " assertions. Art, masking, HUD layering, and click-through need Play-mode checks."); }
}
