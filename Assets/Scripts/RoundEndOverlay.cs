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
    [SerializeField] private Button continueButton;
    [SerializeField] private EscapeStoryPresenter escapeStory;
    [SerializeField] private string mainMenuScenePath = "Assets/Scenes/Menu_scene.unity";
    [SerializeField] private Text errorText;
    [SerializeField, Min(0)] private float resultRevealDelay = .6f;
    private readonly RoundEndState state = new RoundEndState();
    private readonly RoundEndReveal reveal = new RoundEndReveal();
    private bool valid;
    private readonly EscapeStoryState storyState = new EscapeStoryState();
    private double storyDeadline;

    public void ConfigureEscapeStory(EscapeStoryPresenter presenter) { escapeStory = presenter; }

    public bool IsVisible => state.Visible;
    public bool IsReloading => state.IsReloading;
    public float ResultRevealDelay => resultRevealDelay;
    // Keep the earlier scene setup API compatible; Continue no longer uses map rotation.
    public void ConfigureRotation(MapRotation rotation, Button button) { continueButton = button; }
    public void ConfigureContinue(Button button) { continueButton = button; }

    public void Configure(HuntRoundManager manager, GameObject panel, Text result, Button button, Text error, Button menuButton = null)
    { roundManager = manager; overlayPanel = panel; resultText = result; restartButton = button; errorText = error; mainMenuButton = menuButton; }

    private void Awake()
    {
        if (overlayPanel != null) overlayPanel.SetActive(false);
        valid = roundManager != null && overlayPanel != null && resultText != null && restartButton != null && errorText != null
            && overlayPanel != gameObject && !transform.IsChildOf(overlayPanel.transform);
        if (!valid) Debug.LogError("RoundEndOverlay: assign all references and place this controller outside its hidden panel.", this);
        if (errorText != null) errorText.text = string.Empty;
        if (float.IsNaN(resultRevealDelay) || float.IsInfinity(resultRevealDelay) || resultRevealDelay < 0)
        {
            Debug.LogWarning("RoundEndOverlay: invalid result delay; using 0.6 seconds.", this);
            resultRevealDelay = .6f;
        }
    }

    private void OnEnable()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
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
        if (escapeStory != null) escapeStory.Cancel();
        storyState.Finish(); // Re-enable returns to results; it must not issue a load while disabled.
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueButtonClicked);
        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
    }

    private void Update()
    {
        if (!valid || roundManager.Rules == null) return;
        if (storyState.Playing && (escapeStory == null || !escapeStory.isActiveAndEnabled || Time.unscaledTimeAsDouble >= storyDeadline))
        {
            Debug.LogWarning("Phase 3e presentation interrupted or timed out; showing ending actions.", this);
            FinishEscapeStory();
        }
        if (reveal.Observe(roundManager.Rules.State == HuntRules.Phase.RoundOver, Time.timeAsDouble, resultRevealDelay)
            && state.Observe(roundManager.Rules.State)) ShowOverlay();
    }

    private void ShowOverlay()
    {
        roundManager.CancelCameraShakes();
        roundManager.CancelKillClaws();
        int winner = roundManager.Rules.WinnerId;
        resultText.text = RoundEndState.ResultText(winner, roundManager.Rules.Outcome);
        if (storyState.Completed && roundManager.Rules.Outcome == HuntRules.Result.EscapedThroughDoor)
            resultText.text = "You escaped. They didn't.\n" + resultText.text;
        resultText.color = winner < 0 ? Color.white : roundManager.GetPlayerColor(winner);
        SetButtonsInteractable(!state.IsReloading);
        overlayPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnRestartButtonClicked()
    {
        // Fresh Map A instance reruns its WFC generator and resets all round state.
        LoadScene(SceneUtility.GetBuildIndexByScenePath(MapRotation.MapAPath),
            "Map A (SplitScreenTest) is not enabled in Build Settings. Add it, then try again.");
    }

    private void OnMainMenuButtonClicked()
    {
        LoadScene(SceneUtility.GetBuildIndexByScenePath(mainMenuScenePath),
            "Menu_scene is not enabled in Build Settings. Add it, then try again.");
    }

    private void OnContinueButtonClicked()
    {
        if (!valid || storyState.Playing || state.IsReloading || !state.Visible || roundManager.Rules == null
            || roundManager.Rules.State != HuntRules.Phase.RoundOver) return;
        var rules = roundManager.Rules;
        if (storyState.TryBegin(rules.Outcome, rules.WinnerId, rules.ApexId))
        {
            bool started = false;
            try
            {
                var player = roundManager.GetPlayer(storyState.Winner);
                var renderer = player == null ? null : player.GetComponent<SpriteRenderer>();
                if (escapeStory != null && renderer != null)
                {
                    roundManager.CancelCameraShakes(); roundManager.CancelKillClaws();
                    started = escapeStory.TryPlay(renderer.sprite, roundManager.GetPlayerColor(storyState.Winner), FinishEscapeStory);
                }
                if (started)
                {
                    storyDeadline = Time.unscaledTimeAsDouble + escapeStory.Duration + 2;
                    SetButtonsInteractable(false); overlayPanel.SetActive(false);
                    return;
                }
            }
            catch (Exception error) { Debug.LogException(error, this); }
            Debug.LogWarning("Phase 3e unavailable; showing ending actions. Run Phase 3e setup on Map A.", this);
            FinishEscapeStory(); return;
        }
    }

    private void FinishEscapeStory()
    {
        if (!storyState.Finish()) return;
        if (escapeStory != null) escapeStory.Cancel();
        errorText.text = string.Empty;
        ShowOverlay();
    }

    private void LoadScene(int index, string missingSceneMessage)
    {
        if (!valid || storyState.Playing || state.IsReloading || !state.Visible || roundManager.Rules == null
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
        if (overlayPanel != null) overlayPanel.SetActive(true);
        SetButtonsInteractable(true);
        errorText.text = message;
        Debug.LogError("RoundEndOverlay: " + message, this);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        restartButton.interactable = interactable;
        if (mainMenuButton != null) mainMenuButton.interactable = interactable;
        if (continueButton != null)
        {
            var rules = roundManager.Rules;
            bool available = rules != null && storyState.CanBegin(rules.Outcome, rules.WinnerId, rules.ApexId);
            continueButton.gameObject.SetActive(available);
            continueButton.interactable = interactable && available;
        }
    }
}
