using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Phase3dSceneSetup
{
    [MenuItem("Game Jam/Set Up Phase 3d Escape Door")]
    public static void Setup()
    {
        Phase3cSceneSetup.RequireEditMode();
        var manager = Phase3cSceneSetup.InScene<HuntRoundManager>();
        if (!manager.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        var source = manager.GetPlayer(0).GetComponent<SpriteRenderer>();
        var found = manager.transform.Find("EscapeDoor");
        GameObject obj;
        if (found == null)
        {
            obj = new GameObject("EscapeDoor", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(EscapeDoor));
            Undo.RegisterCreatedObjectUndo(obj, "Create escape door"); obj.transform.SetParent(manager.transform, false);
            obj.transform.position = new Vector3(-18.5f, 0, 0); obj.transform.localScale = new Vector3(1.4f, 1.4f, 1);
            obj.GetComponent<BoxCollider2D>().isTrigger = true;
            var border = obj.GetComponent<SpriteRenderer>(); border.sprite = source.sprite; border.sharedMaterial = source.sharedMaterial; border.sortingOrder = source.sortingOrder + 2;
        }
        else obj = found.gameObject;
        if (obj.GetComponent<SpriteRenderer>() == null || obj.GetComponent<BoxCollider2D>() == null || obj.GetComponent<EscapeDoor>() == null)
            throw new InvalidOperationException("Existing EscapeDoor has unexpected components. Rename an unrelated object before setup.");
        if (!obj.GetComponent<BoxCollider2D>().enabled || !obj.GetComponent<BoxCollider2D>().isTrigger)
            throw new InvalidOperationException("EscapeDoor's BoxCollider2D must be enabled and Is Trigger must be checked.");
        var inside = obj.transform.Find("Opening");
        if (inside == null)
        {
            var interior = new GameObject("Opening", typeof(SpriteRenderer)); Undo.RegisterCreatedObjectUndo(interior, "Create door opening");
            inside = interior.transform; inside.SetParent(obj.transform, false); inside.localScale = new Vector3(.75f, .8f, 1);
            var sprite = interior.GetComponent<SpriteRenderer>(); sprite.sprite = source.sprite; sprite.sharedMaterial = source.sharedMaterial; sprite.sortingOrder = source.sortingOrder + 3;
        }
        if (inside.GetComponent<SpriteRenderer>() == null) throw new InvalidOperationException("EscapeDoor/Opening needs its SpriteRenderer.");
        var door = obj.GetComponent<EscapeDoor>();
        Undo.RecordObjects(new UnityEngine.Object[] { manager, door, obj.GetComponent<SpriteRenderer>(), inside.GetComponent<SpriteRenderer>() }, "Configure escape door");
        door.Configure(manager, obj.GetComponent<SpriteRenderer>(), inside.GetComponent<SpriteRenderer>());
        obj.GetComponent<SpriteRenderer>().enabled = inside.GetComponent<SpriteRenderer>().enabled = false;
        manager.ConfigureDoor(door);
        EditorUtility.SetDirty(manager); EditorUtility.SetDirty(door); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("PHASE3D_SETUP_OK: survivor-only door appears at transformation and opening immediately wins. Ctrl+S; see docs/3d steps.md.");
    }
}
