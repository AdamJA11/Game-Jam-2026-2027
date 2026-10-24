using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Phase1SceneBuilder
{
    // Explicit menu action only: importing this script does not modify open scenes.
    [MenuItem("Game Jam/Create Phase 1 Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Art");
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Phase1.unity");
        Scene scene;
        if (!Application.isBatchMode)
        {
            // Copy the current scene, preserving the user's milestone-one work.
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path, true);
            scene = EditorSceneManager.OpenScene(path);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        var cameraObject = GameObject.Find("Main Camera");
        if (cameraObject == null) cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.transform.rotation = Quaternion.identity;
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.08f, 0.13f);

        Sprite sprite = CreateSquareSprite();
        string materialPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Art/PlayerFriction.physicsMaterial2D");
        var friction = new PhysicsMaterial2D("PlayerFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(friction, materialPath);

        GameObject player = Square("Player", new Vector2(-3f, 0f), new Vector2(0.7f, 0.7f), Color.yellow, sprite);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body == null) body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        player.GetComponent<BoxCollider2D>().sharedMaterial = friction;
        player.AddComponent<TopDownPlayerMovement>();

        GameObject target = Square("SecondCharacter", new Vector2(3f, 0f), new Vector2(0.7f, 0.7f), Color.cyan, sprite);
        target.GetComponent<BoxCollider2D>().isTrigger = true;
        target.AddComponent<DisappearOnPlayerTouch2D>();

        var walls = new GameObject("Walls");
        Wall("TopWall", new Vector2(0f, 4.5f), new Vector2(16f, 0.5f), sprite, walls.transform);
        Wall("BottomWall", new Vector2(0f, -4.5f), new Vector2(16f, 0.5f), sprite, walls.transform);
        Wall("LeftWall", new Vector2(-7.75f, 0f), new Vector2(0.5f, 9f), sprite, walls.transform);
        Wall("RightWall", new Vector2(7.75f, 0f), new Vector2(0.5f, 9f), sprite, walls.transform);

        PrefabUtility.SaveAsPrefabAssetAndConnect(player,
            AssetDatabase.GenerateUniqueAssetPath("Assets/Prefabs/Player.prefab"), InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(target,
            AssetDatabase.GenerateUniqueAssetPath("Assets/Prefabs/SecondCharacter.prefab"), InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.SaveAssets();
        Debug.Log("PHASE1_BUILD_OK: " + path);
    }

    private static Sprite CreateSquareSprite()
    {
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Art/PrototypeSquare.png");
        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        var pixels = new Color[256];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject Square(string name, Vector2 position, Vector2 size, Color color, Sprite sprite)
    {
        var obj = GameObject.Find(name);
        if (obj == null) obj = new GameObject(name);
        if (obj.GetComponent<SpriteRenderer>() == null) obj.AddComponent<SpriteRenderer>();
        if (obj.GetComponent<BoxCollider2D>() == null) obj.AddComponent<BoxCollider2D>();
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        obj.GetComponent<BoxCollider2D>().size = Vector2.one;
        return obj;
    }

    private static void Wall(string name, Vector2 position, Vector2 size, Sprite sprite, Transform parent)
    {
        GameObject wall = Square(name, position, size, new Color(0.4f, 0.48f, 0.6f), sprite);
        wall.transform.SetParent(parent, true);
    }
}
