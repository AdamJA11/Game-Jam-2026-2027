using UnityEngine;

// Spawn placement (-200) precedes the introduction; normal movement/follow run afterwards.
[DefaultExecutionOrder(-100)]
public sealed class CameraIntroduction : MonoBehaviour
{
    public enum Stage { Pending, Overview, Approach, Complete, Cancelled }
    [SerializeField] private RandomPlayerSpawns randomSpawns;
    [SerializeField] private TopDownPlayerMovement[] players = new TopDownPlayerMovement[3];
    [SerializeField] private LocalPlayerCameraFollow[] playerCameras = new LocalPlayerCameraFollow[3];
    [SerializeField] private Rect overviewBounds = new Rect(-21, -13, 42, 26);
    [SerializeField, Min(0f)] private float overviewHold = 1f;
    [SerializeField, Min(.01f)] private float approachDuration = 2f;
    [SerializeField, Min(.01f)] private float normalSize = 3f;
    [SerializeField, Min(0f)] private float framingMargin = .5f;
    private readonly bool[] previousBlocks = new bool[3];
    private readonly bool[] previousSuspensions = new bool[3];
    private bool ownsState;
    private float elapsed;
    public Stage CurrentStage { get; private set; } = Stage.Pending;
    public bool IsRunning => CurrentStage == Stage.Overview || CurrentStage == Stage.Approach;
    public float Elapsed => elapsed;
    public float OverviewHold => overviewHold;
    public float ApproachDuration => approachDuration;
    public float NormalSize => normalSize;

    public void Configure(RandomPlayerSpawns spawns, TopDownPlayerMovement[] characters, LocalPlayerCameraFollow[] follows)
    {
        randomSpawns = spawns; players = characters; playerCameras = follows;
    }

    private void Awake()
    {
        if (!ValidateConfiguration(out string error)) { Cancel(error); return; }
        for (int i = 0; i < 3; i++)
        {
            previousBlocks[i] = players[i].InputBlocked;
            previousSuspensions[i] = playerCameras[i].AutomaticFollowSuspended;
            players[i].SetInputBlocked(true);
            playerCameras[i].SuspendForIntroduction();
        }
        ownsState = true;
        CurrentStage = Stage.Overview;
        ApplyPose(0f);
    }

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (randomSpawns == null || !randomSpawns.HasSpawned) error = "RandomPlayerSpawns must finish successfully before the introduction.";
        else if (players == null || players.Length != 3 || playerCameras == null || playerCameras.Length != 3)
            error = "Assign exactly three players and three follow components.";
        else if (!Finite(overviewBounds.x) || !Finite(overviewBounds.y) || !Positive(overviewBounds.width)
            || !Positive(overviewBounds.height) || !Positive(normalSize) || !Positive(approachDuration)
            || !Finite(overviewHold) || overviewHold < 0 || !Finite(framingMargin) || framingMargin < 0)
            error = "Bounds, sizes, and durations must be finite and valid.";
        if (error != null) return false;
        for (int i = 0; i < 3; i++)
        {
            if (players[i] == null || !players[i].gameObject.activeInHierarchy || players[i].GetComponent<Rigidbody2D>() == null
                || playerCameras[i] == null)
            { error = "Player/camera pair " + (i + 1) + " is missing an active player, Rigidbody2D, or follow reference."; return false; }
            Camera camera = playerCameras[i].GetComponent<Camera>();
            // This Awake deliberately runs before follow-camera Awake/OnEnable. A Behaviour's
            // isActiveAndEnabled may still be false then; validate configured state instead.
            if (!playerCameras[i].enabled || !playerCameras[i].gameObject.activeInHierarchy
                || camera == null || !camera.enabled || !camera.orthographic
                || playerCameras[i].Target != players[i].transform)
            { error = "Camera " + playerCameras[i].name + " must be enabled, active, orthographic, and follow player " + (i + 1) + "."; return false; }
            for (int j = 0; j < i; j++) if (players[i] == players[j] || playerCameras[i] == playerCameras[j])
            { error = "Player and camera references must be distinct."; return false; }
        }
        return true;
    }

    private void LateUpdate()
    {
        if (!IsRunning) return;
        for (int i = 0; i < 3; i++)
            if (players[i] == null || !players[i].gameObject.activeInHierarchy || playerCameras[i] == null
                || !playerCameras[i].isActiveAndEnabled || !playerCameras[i].GetComponent<Camera>().isActiveAndEnabled)
            { Cancel("A required player or camera became unavailable during the introduction."); return; }

        // Local timing avoids changing global time scale; movement is gated independently.
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01((elapsed - overviewHold) / approachDuration);
        CurrentStage = elapsed < overviewHold ? Stage.Overview : Stage.Approach;
        ApplyPose(Mathf.SmoothStep(0f, 1f, progress));
        if (progress >= 1f) { CurrentStage = Stage.Complete; RestoreGameplay(); }
    }

    public static float CalculateOverviewSize(Rect bounds, float aspect, float margin)
    {
        return Mathf.Max(bounds.height * .5f, bounds.width / (2f * Mathf.Max(.01f, aspect))) + margin;
    }

    private void ApplyPose(float progress)
    {
        Vector3 start = new Vector3(overviewBounds.center.x, overviewBounds.center.y, -10f);
        for (int i = 0; i < 3; i++)
        {
            Camera camera = playerCameras[i].GetComponent<Camera>();
            // Recompute from current aspect every frame, including when the window resizes.
            float wideSize = Mathf.Max(normalSize, CalculateOverviewSize(overviewBounds, camera.aspect, framingMargin));
            camera.orthographicSize = Mathf.Lerp(wideSize, normalSize, progress);
            camera.transform.SetPositionAndRotation(Vector3.Lerp(start,
                playerCameras[i].GetFollowPosition(normalSize, camera.aspect), progress), Quaternion.identity);
        }
    }

    private void RestoreGameplay()
    {
        if (!ownsState) return;
        // Set every final camera pose before releasing any player's input gate.
        for (int i = 0; i < 3; i++) if (playerCameras[i] != null)
        {
            playerCameras[i].GetComponent<Camera>().orthographicSize = normalSize;
            playerCameras[i].FollowNow();
            playerCameras[i].SetAutomaticFollowSuspended(previousSuspensions[i]);
        }
        for (int i = 0; i < 3; i++) if (players[i] != null) players[i].SetInputBlocked(previousBlocks[i]);
        ownsState = false;
    }

    private void Cancel(string error)
    {
        CurrentStage = Stage.Cancelled;
        RestoreGameplay();
        Debug.LogError("CameraIntroduction: " + error, this);
    }

    private void OnDisable()
    {
        if (!IsRunning) return;
        CurrentStage = Stage.Cancelled;
        RestoreGameplay();
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Positive(float value) => Finite(value) && value > 0f;
}
