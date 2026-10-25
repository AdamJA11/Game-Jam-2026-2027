using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Phase3cSceneSetup
{
    public static T InScene<T>() where T : Component
    {
        var matches = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one " + typeof(T).Name + " in this scene.");
        return matches[0];
    }

    [MenuItem("Game Jam/Set Up Phase 3c Map Rotation")]
    public static void Setup()
    {
        RequireEditMode(); ConfigureCurrent();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("PHASE3C_SETUP_OK: six spawn points and Continue configured. Ctrl+S. Create the second map after configuring the other requested phases; see docs/3c steps.md.");
    }

    public static void RequireEditMode()
    { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before scene setup."); }

    public static void ConfigureCurrent()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != MapRotation.MapAPath && scene.path != MapRotation.MapBPath)
            throw new InvalidOperationException("Open SplitScreenTest or SplitScreenMapB.");
        var manager = InScene<HuntRoundManager>();
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        var spawner = InScene<RandomPlayerSpawns>();
        var overlay = InScene<RoundEndOverlay>();
        var players = Enumerable.Range(0, 3).Select(i => manager.GetPlayer(i).GetComponent<TopDownPlayerMovement>()).ToArray();
        var follows = new LocalPlayerCameraFollow[3];
        for (int i = 0; i < 3; i++)
            follows[i] = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LocalPlayerCameraFollow>(true)).Single(f => f.Target == players[i].transform);
        Vector2[] defaults = { new Vector2(-12, 6), new Vector2(12, 6), new Vector2(-12, -6), new Vector2(12, -6), new Vector2(0, 9), new Vector2(0, -9) };
        var points = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            string name = "Spawn" + (char)('A' + i);
            points[i] = spawner.transform.Find(name);
            if (points[i] == null)
            {
                var point = new GameObject(name); Undo.RegisterCreatedObjectUndo(point, "Add spawn point");
                points[i] = point.transform; points[i].SetParent(spawner.transform, false); points[i].position = defaults[i];
            }
        }
        // Retain any additional authored points beyond the initial six.
        var extra = Enumerable.Range(0, spawner.SpawnPointCount).Select(spawner.GetSpawnPoint).Where(point => point != null && !points.Contains(point));
        Undo.RecordObject(spawner, "Configure larger spawn pool");
        spawner.Configure(players, points.Concat(extra).Distinct().ToArray(), follows);
        var card = overlay.transform.Find("DimBackground/CenteredResult");
        if (card == null) throw new InvalidOperationException("Run Phase 5 setup first; CenteredResult was not found.");
        var restart = card.Find("RestartButton").GetComponent<Button>();
        var next = card.Find("ContinueButton");
        if (next == null)
        {
            var obj = UnityEngine.Object.Instantiate(restart.gameObject, card);
            Undo.RegisterCreatedObjectUndo(obj, "Add Continue button");
            obj.name = "ContinueButton"; next = obj.transform;
            obj.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            obj.GetComponentInChildren<Text>().text = "CONTINUE";
        }
        PlaceButton(next.GetComponent<RectTransform>(), new Vector2(.5f, .47f));
        PlaceButton(restart.GetComponent<RectTransform>(), new Vector2(.28f, .22f));
        var menu = card.Find("MainMenuButton");
        if (menu != null) PlaceButton(menu.GetComponent<RectTransform>(), new Vector2(.72f, .22f));
        var loadError = card.Find("LoadError") as RectTransform;
        if (loadError != null) { loadError.anchorMin = new Vector2(.02f, -.12f); loadError.anchorMax = new Vector2(.98f, .08f); }
        const string configPath = "Assets/MapRotation.asset";
        var rotation = AssetDatabase.LoadAssetAtPath<MapRotation>(configPath);
        if (rotation == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(configPath) != null) throw new InvalidOperationException("MapRotation.asset exists with another type.");
            rotation = ScriptableObject.CreateInstance<MapRotation>(); AssetDatabase.CreateAsset(rotation, configPath);
        }
        Undo.RecordObject(overlay, "Configure map rotation");
        overlay.ConfigureRotation(rotation, next.GetComponent<Button>());
        EditorUtility.SetDirty(overlay); EditorUtility.SetDirty(spawner);
        var build = EditorBuildSettings.scenes.ToList();
        foreach (string path in new[] { rotation.FirstMap, rotation.SecondMap })
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            int index = build.FindIndex(entry => entry.path == path);
            if (index < 0) build.Add(new EditorBuildSettingsScene(path, true));
            else if (!build[index].enabled) build[index] = new EditorBuildSettingsScene(path, true);
        }
        EditorBuildSettings.scenes = build.ToArray();
    }

    private static void PlaceButton(RectTransform rect, Vector2 anchor)
    {
        Undo.RecordObject(rect, "Arrange result buttons");
        rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(320, 64);
    }

    [MenuItem("Game Jam/Create Phase 3c Second Map")]
    public static void CreateSecondMap()
    {
        RequireEditMode();
        if (SceneManager.GetActiveScene().path != MapRotation.MapAPath) throw new InvalidOperationException("Open Map A (SplitScreenTest) first.");
        if (AssetDatabase.LoadMainAssetAtPath(MapRotation.MapBPath) != null) throw new InvalidOperationException("Map B already exists. It will not be overwritten.");
        if (!SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Any(obj => obj.name == "MapLandmarks"))
            throw new InvalidOperationException("MapLandmarks was not found. This generator expects the prototype layout; create/edit the second map manually for a custom layout.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save Map A before copying it.");
        if (!AssetDatabase.CopyAsset(MapRotation.MapAPath, MapRotation.MapBPath)) throw new InvalidOperationException("Could not copy Map A.");
        var scene = EditorSceneManager.OpenScene(MapRotation.MapBPath);
        ConfigureCurrent();
        var spawner = InScene<RandomPlayerSpawns>();
        Vector2[] positions = { new Vector2(-14, 8), new Vector2(14, 8), new Vector2(-14, -8), new Vector2(14, -8), new Vector2(0, 9), new Vector2(0, -9) };
        for (int i = 0; i < 6; i++) spawner.GetSpawnPoint(i).position = positions[i];
        var landmarks = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "MapLandmarks");
        if (landmarks != null)
        {
            foreach (string name in new[] { "WestBlock", "EastBlock", "NorthBlock" })
            { var block = landmarks.Find(name); if (block != null) UnityEngine.Object.DestroyImmediate(block.gameObject); }
            foreach (var floor in landmarks.GetComponentsInChildren<SpriteRenderer>())
                if (floor.name == "Floor") floor.color = new Color(.1f, .19f, .18f);
            var source = InScene<HuntRoundManager>().GetPlayer(0).GetComponent<SpriteRenderer>();
            foreach (Vector2 location in new[] { new Vector2(-8, 4), new Vector2(8, 4), new Vector2(-8, -4), new Vector2(8, -4) })
            {
                var pillar = new GameObject("MapBPillar", typeof(SpriteRenderer), typeof(BoxCollider2D));
                pillar.transform.SetParent(landmarks, false); pillar.transform.position = location; pillar.transform.localScale = new Vector3(2, 4, 1);
                var sprite = pillar.GetComponent<SpriteRenderer>(); sprite.sprite = source.sprite; sprite.sharedMaterial = source.sharedMaterial; sprite.color = new Color(.35f, .5f, .5f);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("PHASE3C_MAP_B_CREATED: configured copy of A with six starts and four pillars. Original A preserved. Test both maps using docs/3c steps.md.");
    }
}
