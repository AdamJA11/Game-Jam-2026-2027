using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class LocalPlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Rect mapBounds = new Rect(-21f, -13f, 42f, 26f);
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

    public void SetAutomaticFollowSuspended(bool suspended) { AutomaticFollowSuspended = suspended; }

    private void OnEnable() { view = GetComponent<Camera>(); if (!AutomaticFollowSuspended) FollowNow(); }
    private void Start() { if (!AutomaticFollowSuspended) FollowNow(); }
    private void LateUpdate() { if (!AutomaticFollowSuspended) FollowNow(); }

    public void FollowNow()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;
        if (view == null) view = GetComponent<Camera>();
        transform.SetPositionAndRotation(GetFollowPosition(view.orthographicSize, view.aspect), Quaternion.identity);
    }

    public Vector3 GetFollowPosition(float size, float aspect)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return transform.position;
        float halfHeight = size;
        float halfWidth = halfHeight * aspect;
        float x = mapBounds.width <= halfWidth * 2f ? mapBounds.center.x
            : Mathf.Clamp(target.position.x, mapBounds.xMin + halfWidth, mapBounds.xMax - halfWidth);
        float y = mapBounds.height <= halfHeight * 2f ? mapBounds.center.y
            : Mathf.Clamp(target.position.y, mapBounds.yMin + halfHeight, mapBounds.yMax - halfHeight);
        return new Vector3(x, y, -10f);
    }
}
