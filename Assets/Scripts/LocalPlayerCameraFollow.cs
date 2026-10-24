using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class LocalPlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Rect mapBounds = new Rect(-21f, -13f, 42f, 26f);
    private Camera view;

    public Transform Target => target;
    public Rect MapBounds => mapBounds;

    public void Configure(Transform player, Rect bounds)
    {
        target = player;
        mapBounds = bounds;
        FollowNow();
    }

    private void OnEnable() { view = GetComponent<Camera>(); FollowNow(); }
    private void Start() { FollowNow(); }
    private void LateUpdate() { FollowNow(); }

    public void FollowNow()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;
        if (view == null) view = GetComponent<Camera>();
        float halfHeight = view.orthographicSize;
        float halfWidth = halfHeight * view.aspect;
        float x = mapBounds.width <= halfWidth * 2f ? mapBounds.center.x
            : Mathf.Clamp(target.position.x, mapBounds.xMin + halfWidth, mapBounds.xMax - halfWidth);
        float y = mapBounds.height <= halfHeight * 2f ? mapBounds.center.y
            : Mathf.Clamp(target.position.y, mapBounds.yMin + halfHeight, mapBounds.yMax - halfHeight);
        transform.SetPositionAndRotation(new Vector3(x, y, -10f), Quaternion.identity);
    }
}
