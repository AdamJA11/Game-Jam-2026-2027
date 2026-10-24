using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Phase4SceneSetup
{
    [MenuItem("Game Jam/Set Up Phase 4 Hunting")]
    [MenuItem("Game Jam/Set Up Phase 4a Speed and Colors")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "SplitScreenTest")
            throw new InvalidOperationException("Open SplitScreenTest first.");
        ConfigureCurrentScene();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("PHASE4_SETUP_OK: hunt cycle, Apex transformation, status labels configured; existing opening timing preserved.");
    }

    public static void ConfigureCurrentScene()
    {
        var intro = UnityEngine.Object.FindObjectOfType<CameraIntroduction>();
        var canvas = GameObject.Find("SplitScreenUI");
        if (intro == null || canvas == null) throw new InvalidOperationException("Configure Phase 3b and its UI first.");
        var movements = new TopDownPlayerMovement[3];
        var follows = new LocalPlayerCameraFollow[3];
        for (int i = 0; i < 3; i++)
        {
            var obj = GameObject.Find(i == 0 ? "Player" : "Player" + (i + 1));
            var cam = GameObject.Find("Player" + (i + 1) + "Camera");
            if (obj == null || cam == null) throw new InvalidOperationException("Missing player/camera " + (i + 1));
            movements[i] = obj.GetComponent<TopDownPlayerMovement>();
            follows[i] = cam.GetComponent<LocalPlayerCameraFollow>();
            if (movements[i] == null || follows[i] == null || obj.GetComponent<SpriteRenderer>() == null)
                throw new InvalidOperationException("Missing player movement/visual/follow component.");
        }
        var root = GameObject.Find("HuntRoundManager");
        if (root == null) root = new GameObject("HuntRoundManager");
        var manager = root.GetComponent<HuntRoundManager>();
        if (manager == null) manager = root.AddComponent<HuntRoundManager>();
        var combatants = new PlayerCombatant[3];
        var labels = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var obj = movements[i].gameObject;
            var aura = obj.transform.Find("ApexAura");
            if (aura == null)
            {
                aura = new GameObject("ApexAura", typeof(SpriteRenderer)).transform;
                aura.SetParent(obj.transform, false);
                aura.localScale = new Vector3(1.3f, 1.3f, 1);
                var renderer = aura.GetComponent<SpriteRenderer>();
                renderer.sprite = obj.GetComponent<SpriteRenderer>().sprite;
                renderer.color = new Color(1, .65f, .1f, .8f);
                renderer.sortingOrder = obj.GetComponent<SpriteRenderer>().sortingOrder - 1;
            }
            aura.gameObject.SetActive(false);
            combatants[i] = obj.GetComponent<PlayerCombatant>();
            if (combatants[i] == null) combatants[i] = obj.AddComponent<PlayerCombatant>();
            combatants[i].Configure(i, manager, follows[i], aura.gameObject);
            EditorUtility.SetDirty(combatants[i]);

            string labelName = "P" + (i + 1) + "HuntStatus";
            var labelObject = canvas.transform.Find(labelName);
            if (labelObject == null) labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text)).transform;
            labelObject.SetParent(canvas.transform, false);
            var rect = (RectTransform)labelObject;
            float x = i == 1 ? .5f : 0;
            float y = i == 2 ? 0 : .5f;
            rect.anchorMin = new Vector2(x, y + .33f);
            rect.anchorMax = new Vector2(x + .5f, y + .43f);
            rect.offsetMin = new Vector2(24, 0);
            rect.offsetMax = new Vector2(-24, 0);
            labels[i] = labelObject.GetComponent<Text>();
            labels[i].font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labels[i].fontSize = 25;
            labels[i].resizeTextForBestFit = true;
            labels[i].resizeTextMinSize = 16;
            labels[i].resizeTextMaxSize = 25;
            labels[i].alignment = TextAnchor.UpperLeft;
            labels[i].raycastTarget = false;
            labels[i].color = Color.white;
            labels[i].supportRichText = true;
            string[] hex = new string[3];
            for (int p = 0; p < 3; p++) hex[p] = ColorUtility.ToHtmlStringRGB(movements[p].GetComponent<SpriteRenderer>().color);
            labels[i].text = HuntPresentation.InitialLabel(i, hex);
            EditorUtility.SetDirty(labels[i]);
        }
        manager.Configure(intro, combatants, labels);
        EditorUtility.SetDirty(manager);
    }

    [MenuItem("Game Jam/Check Phase 4 Rules")]
    public static void CheckRules()
    {
        int checks = HuntRulesTests.Run();
        Debug.Log("PHASE4_RULES_OK: " + checks + " assertions.");
    }

    [MenuItem("Game Jam/Check Phase 4a Rules and Labels")]
    public static void CheckSpeedAndLabels()
    {
        int originalChecks = HuntRulesTests.Run();
        int newChecks = Phase4aTests.Run();
        Debug.Log("PHASE4A_CHECK_OK: " + newChecks + " speed/label assertions; " + originalChecks + " original combat assertions.");
    }
}
