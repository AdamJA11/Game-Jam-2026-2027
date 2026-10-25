using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Phase5SceneSetup
{
    [MenuItem("Game Jam/Set Up Phase 5 Restart UI")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        ConfigureCurrentScene();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("PHASE5_SETUP_OK: Restart and Main Menu overlay configured; both scenes enabled in Build Settings.");
    }

    public static void ConfigureCurrentScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var manager = UnityEngine.Object.FindObjectOfType<HuntRoundManager>();
        if (manager == null || string.IsNullOrEmpty(scene.path))
            throw new InvalidOperationException("Open a saved gameplay scene with Phase 4 configured first.");
        const string menuPath = "Assets/Scenes/Menu_scene.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(menuPath) == null)
            throw new InvalidOperationException("Menu scene not found at " + menuPath);
        var eventSystems = UnityEngine.Object.FindObjectsOfType<EventSystem>();
        if (eventSystems.Length > 1) throw new InvalidOperationException("More than one active EventSystem exists. Keep only one before setup.");

        var root = GameObject.Find("RoundEndUI");
        if (root == null) root = new GameObject("RoundEndUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = Component<Canvas>(root);
        Component<GraphicRaycaster>(root);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = Component<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        var panel = RectChild(root.transform, "DimBackground", Vector2.zero, Vector2.one);
        var background = Component<Image>(panel.gameObject);
        background.color = new Color(0, 0, 0, .65f);
        background.raycastTarget = true;
        var card = RectChild(panel, "CenteredResult", new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        card.sizeDelta = new Vector2(800, 400);

        var heading = Label(card, "Result", new Vector2(.03f, .6f), new Vector2(.97f, 1), "ROUND OVER", 52);
        heading.resizeTextForBestFit = true;
        heading.resizeTextMinSize = 28;
        heading.resizeTextMaxSize = 52;
        var buttonRect = RectChild(card, "RestartButton", new Vector2(.28f, .37f), new Vector2(.28f, .37f));
        buttonRect.sizeDelta = new Vector2(320, 80);
        var image = Component<Image>(buttonRect.gameObject);
        image.color = new Color(.9f, .93f, 1f, 1);
        image.raycastTarget = true;
        var button = Component<Button>(buttonRect.gameObject);
        button.targetGraphic = image;
        button.interactable = true;
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None; // Arrow keys remain movement controls, not a new restart shortcut.
        button.navigation = navigation;
        var colors = button.colors;
        colors.highlightedColor = new Color(.75f, .85f, 1, 1);
        colors.pressedColor = new Color(.5f, .65f, .9f, 1);
        button.colors = colors;
        var buttonText = Label(buttonRect, "Label", Vector2.zero, Vector2.one, "RESTART GAME", 30);
        buttonText.color = new Color(.04f, .05f, .08f, 1);
        var menuRect = RectChild(card, "MainMenuButton", new Vector2(.72f, .37f), new Vector2(.72f, .37f));
        menuRect.sizeDelta = new Vector2(320, 80);
        var menuImage = Component<Image>(menuRect.gameObject);
        menuImage.color = image.color;
        menuImage.raycastTarget = true;
        var menuButton = Component<Button>(menuRect.gameObject);
        menuButton.targetGraphic = menuImage;
        menuButton.interactable = true;
        menuButton.colors = button.colors;
        menuButton.navigation = button.navigation;
        var menuLabel = Label(menuRect, "Label", Vector2.zero, Vector2.one, "MAIN MENU", 30);
        menuLabel.color = buttonText.color;
        var error = Label(card, "LoadError", new Vector2(.05f, 0), new Vector2(.95f, .2f), "", 23);
        error.color = new Color(1f, .7f, .7f, 1);
        var controller = Component<RoundEndOverlay>(root);
        controller.Configure(manager, panel.gameObject, heading, button, error, menuButton);
        EditorUtility.SetDirty(controller);
        panel.gameObject.SetActive(false);

        if (eventSystems.Length == 0)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        else if (!eventSystems[0].GetComponents<BaseInputModule>().Any(module => module.enabled))
            eventSystems[0].gameObject.AddComponent<StandaloneInputModule>();

        // Preserve ordering and all unrelated entries, including the merged menu flow.
        var scenes = EditorBuildSettings.scenes.ToList();
        int index = scenes.FindIndex(entry => entry.path == scene.path);
        if (index < 0) scenes.Add(new EditorBuildSettingsScene(scene.path, true));
        else if (!scenes[index].enabled) scenes[index] = new EditorBuildSettingsScene(scene.path, true);
        int menuIndex = scenes.FindIndex(entry => entry.path == menuPath);
        if (menuIndex < 0) scenes.Add(new EditorBuildSettingsScene(menuPath, true));
        else if (!scenes[menuIndex].enabled) scenes[menuIndex] = new EditorBuildSettingsScene(menuPath, true);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static T Component<T>(GameObject obj) where T : UnityEngine.Component
    { var result = obj.GetComponent<T>(); return result != null ? result : obj.AddComponent<T>(); }

    private static RectTransform RectChild(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var found = parent.Find(name);
        var rect = found == null ? new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>() : found.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Text Label(Transform parent, string name, Vector2 min, Vector2 max, string text, int size)
    {
        var rect = RectChild(parent, name, min, max);
        var label = Component<Text>(rect.gameObject);
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    [MenuItem("Game Jam/Check Phase 5 Restart Rules")]
    public static void Check()
    { Debug.Log("PHASE5_RULES_OK: " + RoundEndStateTests.Run() + " assertions."); }
}
