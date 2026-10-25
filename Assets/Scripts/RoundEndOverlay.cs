using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RoundEndOverlay : MonoBehaviour
{
    [SerializeField] private HuntRoundManager roundManager;
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Text resultText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuScenePath = "Assets/Scenes/Menu_scene.unity";
    [SerializeField] private Text errorText;
    private readonly RoundEndState state = new RoundEndState();
    private bool valid;

    public bool IsVisible => state.Visible;
    public bool IsReloading => state.IsReloading;

    public void Configure(HuntRoundManager manager, GameObject panel, Text result, Button button, Text error, Button menuButton = null)
    { roundManager = manager; overlayPanel = panel; resultText = result; restartButton = button; errorText = error; mainMenuButton = menuButton; }

    private void Awake()
    {
        if (overlayPanel != null) overlayPanel.SetActive(false);
        valid = roundManager != null && overlayPanel != null && resultText != null && restartButton != null && errorText != null
            && overlayPanel != gameObject && !transform.IsChildOf(overlayPanel.transform);
        if (!valid) Debug.LogError("RoundEndOverlay: assign all references and place this controller outside its hidden panel.", this);
        if (errorText != null) errorText.text = string.Empty;
    }

    private void OnEnable()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
        if (state.Visible && valid) ShowOverlay();
    }

    private void OnDisable()
    {
        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
    }

    private void Update()
    {
        if (!valid || roundManager.Rules == null) return;
        if (state.Observe(roundManager.Rules.State)) ShowOverlay();
    }

    private void ShowOverlay()
    {
        int winner = roundManager.Rules.WinnerId;
        resultText.text = RoundEndState.ResultText(winner);
        resultText.color = winner < 0 ? Color.white : roundManager.GetPlayerColor(winner);
        SetButtonsInteractable(!state.IsReloading);
        overlayPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnRestartButtonClicked()
    {
        // Reload the scene owning this overlay, never a menu or ambiguous same-named scene.
        Scene gameplay = gameObject.scene;
        int index = SceneUtility.GetBuildIndexByScenePath(gameplay.path);
        LoadScene(gameplay.IsValid() && !string.IsNullOrEmpty(gameplay.path) ? index : -1,
            "This gameplay scene is not enabled in Build Settings. Add it, then try again.");
    }

    private void OnMainMenuButtonClicked()
    {
        LoadScene(SceneUtility.GetBuildIndexByScenePath(mainMenuScenePath),
            "Menu_scene is not enabled in Build Settings. Add it, then try again.");
    }

    private void LoadScene(int index, string missingSceneMessage)
    {
        if (!valid || state.IsReloading || !state.Visible || roundManager.Rules == null
            || roundManager.Rules.State != HuntRules.Phase.RoundOver) return;
        if (index < 0 || !Application.CanStreamedLevelBeLoaded(index))
        {
            LoadingFailed(missingSceneMessage);
            return;
        }
        if (!state.TryBeginReload(true, true)) return;
        SetButtonsInteractable(false);
        errorText.text = string.Empty;
        try
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(index, LoadSceneMode.Single);
            if (load == null) LoadingFailed("Unity could not start loading the scene. Please try again.");
        }
        catch (Exception error)
        {
            LoadingFailed("The scene could not load. Check the Console and try again.");
            Debug.LogException(error, this);
        }
    }

    private void LoadingFailed(string message)
    {
        state.ReloadFailed();
        SetButtonsInteractable(true);
        errorText.text = message;
        Debug.LogError("RoundEndOverlay: " + message, this);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        restartButton.interactable = interactable;
        if (mainMenuButton != null) mainMenuButton.interactable = interactable;
    }
}
