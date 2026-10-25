using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class LocalPlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Rect mapBounds = new Rect(-21f, -13f, 42f, 26f);
    [Header("Death camera shake")]
    [SerializeField] private bool deathShakeConfigured;
    [SerializeField] private bool deathShakeEnabled;
    [SerializeField, Min(0)] private float deathShakeAmplitude = .12f;
    [SerializeField, Min(.01f)] private float deathShakeDuration = .25f;
    [SerializeField, Min(.01f)] private float deathShakeFrequency = 25f;
    private readonly DeathCameraShakeState shake = new DeathCameraShakeState();
    private Vector3 basePose;
    private bool hasBasePose, holdingView;
    private Camera view;
    public bool AutomaticFollowSuspended { get; private set; }

    public Transform Target => target;
    public Rect MapBounds => mapBounds;

    public void Configure(Transform player, Rect bounds)
    {
        target = player;
        mapBounds = bounds;
        FollowNow();
    }

    public void ConfigureDeathShake()
    {
        if (!deathShakeConfigured) { deathShakeEnabled = true; deathShakeConfigured = true; }
    }

    public bool ValidateDeathShake(out string error)
    {
        error = null;
        if (!Finite(deathShakeAmplitude) || deathShakeAmplitude < 0 || !Finite(deathShakeDuration) || deathShakeDuration <= 0
            || !Finite(deathShakeFrequency) || deathShakeFrequency <= 0 || (double)deathShakeDuration * deathShakeFrequency > int.MaxValue - 2)
            error = "Use finite non-negative amplitude and positive duration/frequency.";
        return error == null;
    }

    public void SetAutomaticFollowSuspended(bool suspended)
    {
        AutomaticFollowSuspended = suspended;
        if (!suspended) { holdingView = false; shake.Cancel(); }
    }

    public void SuspendForIntroduction()
    {
        // Introduction writes its own pose; suspended follow must not overwrite it.
        AutomaticFollowSuspended = true; holdingView = false; shake.Cancel();
    }

    public void HoldView()
    {
        if (holdingView) return;
        if (view == null) view = GetComponent<Camera>();
        if (!AutomaticFollowSuspended && target != null && target.gameObject.activeInHierarchy)
            basePose = GetFollowPosition(view.orthographicSize, view.aspect);
        else if (!hasBasePose) basePose = transform.position;
        hasBasePose = true; holdingView = true; AutomaticFollowSuspended = true;
    }

    public void HoldDeathViewAndShake(int playerId)
    {
        // Do not take camera ownership away from the opening/external pose controller.
        if (AutomaticFollowSuspended && !holdingView) return;
        HoldView();
        if (!deathShakeEnabled || !isActiveAndEnabled) return;
        if (!ValidateDeathShake(out string error)) { Debug.LogWarning("LocalPlayerCameraFollow: " + error, this); return; }
        shake.TryStart(Time.unscaledTimeAsDouble, playerId, deathShakeDuration, deathShakeAmplitude, deathShakeFrequency);
    }

    public void CancelDeathShake()
    {
        shake.Cancel();
        if (holdingView && hasBasePose && view != null) RenderPose();
    }

    private void OnEnable() { view = GetComponent<Camera>(); if (!AutomaticFollowSuspended) FollowNow(); else if (holdingView) RenderPose(); }
    private void Start() { if (!AutomaticFollowSuspended) FollowNow(); }
    private void LateUpdate()
    {
        if (!deathShakeEnabled) shake.Cancel();
        if (!AutomaticFollowSuspended) FollowNow();
        else if (holdingView) RenderPose();
    }
    private void OnDisable() { CancelDeathShake(); }

    public void FollowNow()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;
        if (view == null) view = GetComponent<Camera>();
        basePose = GetFollowPosition(view.orthographicSize, view.aspect); hasBasePose = true;
        RenderPose();
    }

    public Vector3 GetFollowPosition(float size, float aspect)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return hasBasePose ? basePose : transform.position;
        float halfHeight = size;
        float halfWidth = halfHeight * aspect;
        float x = DeathCameraShakeState.ClampAxis(target.position.x, mapBounds.xMin, mapBounds.xMax, halfWidth);
        float y = DeathCameraShakeState.ClampAxis(target.position.y, mapBounds.yMin, mapBounds.yMax, halfHeight);
        return new Vector3(x, y, -10f);
    }

    private void RenderPose()
    {
        if (!hasBasePose || view == null) return;
        shake.Sample(Time.unscaledTimeAsDouble, out float x, out float y);
        float halfY = view.orthographicSize, halfX = halfY * view.aspect;
        Vector3 pose = new Vector3(DeathCameraShakeState.ClampAxis(basePose.x + x, mapBounds.xMin, mapBounds.xMax, halfX),
            DeathCameraShakeState.ClampAxis(basePose.y + y, mapBounds.yMin, mapBounds.yMax, halfY), basePose.z);
        transform.SetPositionAndRotation(pose, Quaternion.identity);
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
