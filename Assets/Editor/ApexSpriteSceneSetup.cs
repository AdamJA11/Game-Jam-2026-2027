using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApexSpriteSceneSetup
{
    private const string SpritePath = "Assets/Art/apex/apex.png";

    [MenuItem("Game Jam/Prepare Apex Pixel Sprite")]
    public static void PrepareSprite()
    {
        Phase3cSceneSetup.RequireEditMode();
        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Apex image not found at " + SpritePath);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 8; importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center; settings.spritePivot = Vector2.one * .5f; settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
        Debug.Log("APEX_PIXEL_SPRITE_READY: Single/Point/uncompressed, centered pivot, 8 PPU. Existing asset GUID preserved.");
    }

    [MenuItem("Game Jam/Set Up Player and Apex Sprites")]
    public static void Setup()
    {
        Phase3cSceneSetup.RequireEditMode();
        var manager = Phase3cSceneSetup.InScene<HuntRoundManager>();
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) throw new InvalidOperationException("Save the gameplay scene first.");
        PrepareSprite();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null) throw new InvalidOperationException("Import Apex as a Sprite before setup.");
        Phase4bSceneSetup.Setup(); // Reuses existing bars/ring and fixes the bars' primitive sprite.
        for (int i = 0; i < 3; i++)
        {
            var facing = manager.GetPlayer(i).GetComponent<PlayerVisualFacing>();
            if (facing == null) facing = Undo.AddComponent<PlayerVisualFacing>(manager.GetPlayer(i).gameObject);
            if (!facing.ValidateConfiguration(out error)) throw new InvalidOperationException(error);
        }
        var presentation = manager.GetComponent<ApexTransformationPresentation>();
        Undo.RecordObject(presentation, "Assign Apex artwork"); presentation.ConfigureApexSprite(sprite); EditorUtility.SetDirty(presentation);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("APEX_SPRITE_SETUP_OK: native-color Apex artwork, visual-only facing, rectangular bars. Ctrl+S, repeat on Map B; see docs/apex sprite steps.md.");
    }

    [MenuItem("Game Jam/Check Sprite Facing Rules")]
    public static void Check() { Debug.Log("SPRITE_FACING_RULES_OK: " + SpriteFacingTests.Run() + " assertions. Actual sprite direction, sizing, bars, and collisions need Play-mode checks."); }
}
