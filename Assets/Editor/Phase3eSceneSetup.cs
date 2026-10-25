using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Phase3eSceneSetup
{
    [MenuItem("Game Jam/Set Up Phase 3e Escape Ending")]
    public static void Setup()
    {
        Phase3cSceneSetup.RequireEditMode();
        var scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Open and save a configured gameplay scene first.");
        if (scene.path != MapRotation.MapAPath) throw new InvalidOperationException("Open Map A (SplitScreenTest), which now generates each new round with WFC.");
        var manager = Phase3cSceneSetup.InScene<HuntRoundManager>();
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        var overlay = Phase3cSceneSetup.InScene<RoundEndOverlay>();
        var canvas = overlay.GetComponent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            throw new InvalidOperationException("Keep the existing RoundEndUI screen-space Canvas and result controller together.");
        Sprite car = Import("Assets/Art/story/ambulance.png");
        var people = new Sprite[3];
        for (int i = 0; i < 3; i++) people[i] = Import("Assets/Art/story/citizen" + (i + 1) + ".png");
        Sprite monster = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/apex/apex.png");
        if (monster == null) throw new InvalidOperationException("Run Set Up Player and Apex Sprites first; Apex artwork must be a Sprite.");

        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Set Up Phase 3e Escape Ending");
        try
        {
            var panel = Rect(overlay.transform, "EscapeStory", Vector2.zero, Vector2.one);
            panel.SetAsLastSibling();
            var background = Add<Image>(panel.gameObject); Undo.RecordObject(background, "Story backdrop");
            background.color = Color.black; background.raycastTarget = true;
            var stage = Rect(panel, "Stage", Vector2.zero, Vector2.one);
            var fitter = Add<AspectRatioFitter>(stage.gameObject); Undo.RecordObject(fitter, "Fit story stage");
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = 16f / 9;
            var fade = Add<CanvasGroup>(stage.gameObject); Undo.RecordObject(fade, "Story fade");
            fade.alpha = 1; fade.interactable = true; fade.blocksRaycasts = true;
            var yard = Graphic(stage, "Yard", null, new Color(.12f,.20f,.17f), Vector2.zero, Vector2.one);
            var road = Graphic(stage, "Road", null, new Color(.23f,.25f,.27f), new Vector2(0,.19f),new Vector2(1,.44f));
            Graphic(stage, "RoadMark", null, new Color(.72f,.7f,.52f), new Vector2(0,.22f),new Vector2(1,.225f));
            Graphic(stage, "Wall", null, new Color(.35f,.37f,.42f), new Vector2(.04f,.48f),new Vector2(.09f,.86f));
            Graphic(stage, "OpenDoor", null, new Color(.03f,.04f,.05f), new Vector2(.035f,.59f),new Vector2(.095f,.72f));
            Image ambulance = Actor(stage, "Ambulance", car);
            Image identity = Actor(stage, "SurvivorCarMarker", null);
            var citizens = new Image[6];
            for (int i = 0; i < 6; i++) citizens[i] = Actor(stage, "Citizen" + (i + 1), people[i % 3]);
            Image survivor = Actor(stage, "Survivor", null);
            Image apex = Actor(stage, "Apex", monster);
            var fragments = new Image[12];
            for (int i = 0; i < 12; i++) { fragments[i] = Actor(stage, "Fragment" + i, null); fragments[i].enabled = false; }
            Text caption = Label(stage, "Caption", new Vector2(.12f,.87f),new Vector2(.88f,.97f), "THE STREET OUTSIDE", 46);
            var skipRect = Rect(stage, "Skip", new Vector2(.82f,.035f),new Vector2(.97f,.115f));
            Image buttonImage = Add<Image>(skipRect.gameObject); Undo.RecordObject(buttonImage,"Skip background");
            buttonImage.color = new Color(.12f,.14f,.18f,.95f); buttonImage.raycastTarget = true;
            Button skip = Add<Button>(skipRect.gameObject); Undo.RecordObject(skip, "Skip button");
            skip.targetGraphic = buttonImage;
            var navigation = skip.navigation; navigation.mode = Navigation.Mode.None; skip.navigation = navigation;
            Label(skipRect, "Label", Vector2.zero, Vector2.one, "SKIP", 30);
            var presenter = Add<EscapeStoryPresenter>(overlay.gameObject);
            Undo.RecordObject(presenter,"Assign escape story references");
            presenter.Configure(panel.gameObject, stage, fade, survivor, apex, ambulance, identity, citizens, fragments, caption, skip, monster);
            Button continueButton = SetUpContinue(overlay.transform);
            Undo.RecordObject(overlay,"Connect Continue to escape story");
            overlay.ConfigureEscapeStory(presenter); overlay.ConfigureContinue(continueButton);
            if (!presenter.enabled || !presenter.ValidateConfiguration(out error)) throw new InvalidOperationException(error ?? "Enable EscapeStoryPresenter.");
            Undo.RecordObject(panel.gameObject,"Hide story until Continue"); panel.gameObject.SetActive(false);
            EditorUtility.SetDirty(presenter); EditorUtility.SetDirty(overlay);
            Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("PHASE3E_SETUP_OK: Continue plays the story, then shows Restart/Main Menu. Restart reloads WFC Map A. Ctrl+S; no Map B setup needed. See docs/3e steps.md.");
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }

    private static Button SetUpContinue(Transform root)
    {
        Transform card = root.Find("DimBackground/CenteredResult");
        if (card == null) throw new InvalidOperationException("Keep the Phase 5 CenteredResult UI before running story setup.");
        Transform restart = card.Find("RestartButton");
        Transform menu = card.Find("MainMenuButton");
        if (restart == null || restart.GetComponent<Button>() == null || menu == null)
            throw new InvalidOperationException("Keep the existing Restart and Main Menu buttons.");
        Transform next = card.Find("ContinueButton");
        if (next == null)
        {
            var obj = UnityEngine.Object.Instantiate(restart.gameObject, card);
            Undo.RegisterCreatedObjectUndo(obj,"Create story Continue button");
            obj.name = "ContinueButton"; next = obj.transform;
            obj.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
        }
        PlaceButton(next, new Vector2(.5f,.47f));
        PlaceButton(restart, new Vector2(.28f,.22f));
        PlaceButton(menu, new Vector2(.72f,.22f));
        var label = next.GetComponentInChildren<Text>(true);
        if (label != null) { Undo.RecordObject(label,"Story Continue label"); label.text = "CONTINUE"; }
        var button = next.GetComponent<Button>();
        if (button == null) throw new InvalidOperationException("ContinueButton needs its Button component.");
        Undo.RecordObject(button,"Mouse-only Continue");
        var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
        return button;
    }

    private static void PlaceButton(Transform button, Vector2 anchor)
    {
        var rect = button as RectTransform;
        if (rect == null) throw new InvalidOperationException("Result buttons need RectTransforms.");
        Undo.RecordObject(rect,"Layout ending actions");
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(320,80);
    }

    private static Sprite Import(string path)
    {
        // Refresh only these user-supplied files, not the open scene or entire project.
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing story image: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center; settings.spritePivot = Vector2.one * .5f; settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Could not import story Sprite: " + path);
        return sprite;
    }

    private static T Add<T>(GameObject obj) where T : Component
        => obj.GetComponent<T>() ?? Undo.AddComponent<T>(obj);

    private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        Transform found = parent.Find(name);
        RectTransform rect;
        if (found != null)
        {
            rect = found as RectTransform;
            if (rect == null) throw new InvalidOperationException("Rename unrelated non-UI object: " + name);
        }
        else
        {
            var obj = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(obj,"Create escape story UI");
            obj.transform.SetParent(parent,false); rect = obj.GetComponent<RectTransform>();
        }
        Undo.RecordObject(rect,"Layout escape story");
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = Vector2.one * .5f;
        rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
        return rect;
    }
    private static Image Graphic(Transform parent, string name, Sprite sprite, Color color, Vector2 min, Vector2 max)
    {
        var rect = Rect(parent,name,min,max); var image = Add<Image>(rect.gameObject);
        Undo.RecordObject(image,"Assign story image"); image.sprite = sprite; image.color = color;
        image.raycastTarget = false; image.preserveAspect = sprite != null; image.enabled = true;
        return image;
    }
    private static Image Actor(Transform parent, string name, Sprite sprite)
        => Graphic(parent,name,sprite,Color.white,Vector2.one * .5f,Vector2.one * .5f);
    private static Text Label(Transform parent, string name, Vector2 min, Vector2 max, string value, int size)
    {
        var label = Add<Text>(Rect(parent,name,min,max).gameObject); Undo.RecordObject(label,"Story label");
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); label.text = value; label.fontSize = size;
        label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = size;
        label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        return label;
    }

    [MenuItem("Game Jam/Check Phase 3e Story Rules")]
    public static void Check() { Debug.Log("PHASE3E_RULES_OK: " + Phase3eTests.Run() + " assertions. Playback and scene-load checks are manual."); }
}
