using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class SplitScreenSceneBuilder
{
    [MenuItem("Game Jam/Create Phase 3 Split Screen Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/SplitScreenTest.unity");
        if (!AssetDatabase.CopyAsset("Assets/Scenes/RealGame.unity", path))
            throw new InvalidOperationException("Could not copy RealGame.");
        var scene = EditorSceneManager.OpenScene(path);
        foreach (var camera in UnityEngine.Object.FindObjectsOfType<Camera>()) camera.gameObject.SetActive(false);
        foreach (var listener in UnityEngine.Object.FindObjectsOfType<AudioListener>()) listener.enabled = false;

        var players = new[] { GameObject.Find("Player"), GameObject.Find("Player2"), GameObject.Find("Player3") };
        Vector2[] spawns = { new Vector2(-12, 6), new Vector2(12, 6), new Vector2(-12, -6) };
        Rect[] views = { new Rect(0, .5f, .5f, .5f), new Rect(.5f, .5f, .5f, .5f), new Rect(0, 0, .5f, .5f) };
        for (int i = 0; i < 3; i++)
        {
            players[i].transform.position = spawns[i];
            var cameraObject = new GameObject("Player" + (i + 1) + "Camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
            if (i == 0) { cameraObject.tag = "MainCamera"; cameraObject.AddComponent<AudioListener>(); }
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.rect = views[i];
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.045f, .055f, .08f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<LocalPlayerCameraFollow>().Configure(players[i].transform, new Rect(-21, -13, 42, 26));
        }

        SetWall("TopWall", new Vector2(0, 12.25f), new Vector2(41, .5f));
        SetWall("BottomWall", new Vector2(0, -12.25f), new Vector2(41, .5f));
        SetWall("LeftWall", new Vector2(-20.25f, 0), new Vector2(.5f, 25));
        SetWall("RightWall", new Vector2(20.25f, 0), new Vector2(.5f, 25));
        GameObject.Find("SecondCharacter").transform.position = Vector3.zero;
        Sprite square = players[0].GetComponent<SpriteRenderer>().sprite;
        var map = new GameObject("MapLandmarks");
        for (int x = -19; x < 20; x += 2)
            for (int y = -11; y < 12; y += 2)
                Tile("Floor", new Vector2(x, y), new Vector2(2, 2),
                    ((x + y) / 2) % 2 == 0 ? new Color(.1f, .13f, .18f) : new Color(.12f, .15f, .2f), square, map.transform, -10, false);
        Tile("WestBlock", new Vector2(-6, 0), new Vector2(1, 5), new Color(.4f, .45f, .55f), square, map.transform, 0, true);
        Tile("EastBlock", new Vector2(6, 0), new Vector2(1, 5), new Color(.4f, .45f, .55f), square, map.transform, 0, true);
        Tile("NorthBlock", new Vector2(0, 5), new Vector2(4, 1), new Color(.4f, .45f, .55f), square, map.transform, 0, true);
        for (int i = 0; i < 3; i++)
            Tile("SpawnMarker" + (i + 1), spawns[i], new Vector2(2, 2),
                players[i].GetComponent<SpriteRenderer>().color * new Color(.4f, .4f, .4f, 1), square, map.transform, -5, false);

        CreateUI();
        ConfigureRandomSpawns();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("PHASE3_BUILD_OK: " + path);
    }

    [MenuItem("Game Jam/Set Up Phase 3a Random Spawns")]
    public static void UpgradeRandomSpawns()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SplitScreenTest")
            throw new InvalidOperationException("Open SplitScreenTest before configuring random spawns.");
        ConfigureRandomSpawns();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("PHASE3A_SETUP_OK: named spawn points and one-time shuffle configured.");
    }

    private static void ConfigureRandomSpawns()
    {
        var root = GameObject.Find("SpawnPoints");
        if (root == null) root = new GameObject("SpawnPoints");
        Vector2[] defaults = { new Vector2(-12, 6), new Vector2(12, 6), new Vector2(-12, -6) };
        var points = new Transform[3];
        var players = new TopDownPlayerMovement[3];
        var cameras = new LocalPlayerCameraFollow[3];
        for (int i = 0; i < 3; i++)
        {
            string name = "Spawn" + (char)('A' + i);
            points[i] = root.transform.Find(name);
            if (points[i] == null)
            {
                points[i] = new GameObject(name).transform;
                points[i].SetParent(root.transform);
                points[i].position = defaults[i];
            }
            players[i] = GameObject.Find(i == 0 ? "Player" : "Player" + (i + 1)).GetComponent<TopDownPlayerMovement>();
            cameras[i] = GameObject.Find("Player" + (i + 1) + "Camera").GetComponent<LocalPlayerCameraFollow>();
            var marker = GameObject.Find("SpawnMarker" + (i + 1));
            if (marker != null)
            {
                marker.GetComponent<SpriteRenderer>().color = new Color(.25f, .3f, .36f, 1);
                marker.transform.SetParent(points[i], true);
                marker.transform.localPosition = Vector3.zero;
            }
        }
        var spawner = root.GetComponent<RandomPlayerSpawns>();
        if (spawner == null) spawner = root.AddComponent<RandomPlayerSpawns>();
        spawner.Configure(players, points, cameras);
        EditorUtility.SetDirty(spawner);
    }

    private static void SetWall(string name, Vector2 position, Vector2 size)
    {
        var wall = GameObject.Find(name);
        wall.transform.position = position;
        wall.transform.localScale = new Vector3(size.x, size.y, 1);
    }

    private static void Tile(string name, Vector2 position, Vector2 size, Color color, Sprite sprite, Transform parent, int order, bool solid)
    {
        var obj = new GameObject(name, typeof(SpriteRenderer));
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (solid) obj.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    private static RectTransform UIObject(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void TextLabel(string name, Transform parent, Vector2 min, Vector2 max, string value, Color color, int size)
    {
        var rect = UIObject(name, parent, min, max);
        rect.offsetMin = new Vector2(24, 12);
        rect.offsetMax = new Vector2(-24, -12);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.text = value;
        label.color = color;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    private static void CreateUI()
    {
        var canvasObject = new GameObject("SplitScreenUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var panel = UIObject("ControlsPanel", canvasObject.transform, new Vector2(.5f, 0), new Vector2(1, .5f));
        panel.gameObject.AddComponent<Image>().color = new Color(.045f, .055f, .08f, 1);
        TextLabel("ControlsHeading", panel, new Vector2(0, .78f), Vector2.one, "CONTROLS", Color.white, 36);
        TextLabel("P1Controls", panel, new Vector2(0, .55f), new Vector2(1, .78f), "P1  /  YELLOW\nW up    A left    S down    D right", Color.yellow, 28);
        TextLabel("P2Controls", panel, new Vector2(0, .32f), new Vector2(1, .55f), "P2  /  MAGENTA\nArrow keys: up, left, down, right", new Color(1, .25f, .8f), 28);
        TextLabel("P3Controls", panel, new Vector2(0, .09f), new Vector2(1, .32f), "P3  /  GREEN\nI up    J left    K down    L right", new Color(.3f, 1, .3f), 28);
        TextLabel("P1Label", canvasObject.transform, new Vector2(0, .93f), new Vector2(.5f, 1), "P1 / WASD", Color.yellow, 26);
        TextLabel("P2Label", canvasObject.transform, new Vector2(.5f, .93f), Vector2.one, "P2 / ARROWS", new Color(1, .25f, .8f), 26);
        TextLabel("P3Label", canvasObject.transform, new Vector2(0, .43f), new Vector2(.5f, .5f), "P3 / IJKL", new Color(.3f, 1, .3f), 26);
        var vertical = UIObject("VerticalDivider", canvasObject.transform, new Vector2(.5f, 0), new Vector2(.5f, 1));
        vertical.sizeDelta = new Vector2(4, 0);
        vertical.gameObject.AddComponent<Image>().color = Color.black;
        var horizontal = UIObject("HorizontalDivider", canvasObject.transform, new Vector2(0, .5f), new Vector2(1, .5f));
        horizontal.sizeDelta = new Vector2(0, 4);
        horizontal.gameObject.AddComponent<Image>().color = Color.black;
    }
}
