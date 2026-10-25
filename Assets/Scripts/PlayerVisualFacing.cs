using UnityEngine;

// The physics root remains stable. Only these owned renderers turn with input.
[DefaultExecutionOrder(30)]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerVisualFacing : MonoBehaviour
{
    [SerializeField] private float normalFacingOffset = 0;
    [SerializeField] private float apexFacingOffset = 0;
    [SerializeField, Min(.01f)] private float apexSizeMultiplier = 1f;
    private SpriteRenderer source, normal, apex, aura;
    private TopDownPlayerMovement movement;
    private Transform visualRoot;
    private bool ready, eliminated, sourceWasEnabled;
    private float heading, progress;
    private Sprite apexSprite, originalAuraSprite;
    private Vector3 originalAuraPosition, originalAuraScale;
    private Quaternion originalAuraRotation;
    private bool auraCached;

    public float FacingAngle => heading;
    public SpriteRenderer VisibleRenderer
    {
        get { Ensure(); return ready ? (apexSprite != null && progress >= .5f ? apex : normal) : source; }
    }
    public Bounds VisibleBounds
    {
        get
        {
            Ensure(); if (!ready) return source != null ? source.bounds : new Bounds(transform.position, Vector3.zero);
            if (normal.enabled && apex.enabled) { Bounds bounds = normal.bounds; bounds.Encapsulate(apex.bounds); return bounds; }
            return apex.enabled ? apex.bounds : normal.bounds;
        }
    }

    public static PlayerVisualFacing For(GameObject player)
    {
        var result = player.GetComponent<PlayerVisualFacing>();
        return result != null ? result : player.AddComponent<PlayerVisualFacing>();
    }

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (float.IsNaN(normalFacingOffset) || float.IsInfinity(normalFacingOffset) || float.IsNaN(apexFacingOffset) || float.IsInfinity(apexFacingOffset)
            || float.IsNaN(apexSizeMultiplier) || float.IsInfinity(apexSizeMultiplier) || apexSizeMultiplier <= 0)
            error = "Facing offsets must be finite and Apex Size Multiplier positive.";
        return error == null;
    }

    private void Awake() { Ensure(); }
    private void OnEnable() { Ensure(); if (ready) { source.enabled = false; Refresh(); } }

    private void Ensure()
    {
        if (ready || !Application.isPlaying) return;
        source = GetComponent<SpriteRenderer>(); movement = GetComponent<TopDownPlayerMovement>();
        if (source == null || source.sprite == null) return;
        if (!ValidateConfiguration(out string error)) { Debug.LogWarning("PlayerVisualFacing: " + error, this); enabled = false; return; }
        sourceWasEnabled = source.enabled; heading = transform.eulerAngles.z;
        var obj = new GameObject("RuntimeCharacterVisuals"); obj.layer = gameObject.layer;
        visualRoot = obj.transform; visualRoot.SetParent(transform, false);
        normal = Child("Normal"); apex = Child("Apex");
        ready = true; source.enabled = false; Refresh();
    }

    private SpriteRenderer Child(string name)
    {
        var obj = new GameObject(name, typeof(SpriteRenderer)); obj.layer = gameObject.layer; obj.transform.SetParent(visualRoot, false);
        var renderer = obj.GetComponent<SpriteRenderer>(); renderer.enabled = false; return renderer;
    }

    public void PresentApex(Sprite sprite, float amount, SpriteRenderer outline)
    {
        Ensure(); if (!ready || !enabled) return;
        if (outline != null && !auraCached)
        {
            aura = outline; originalAuraSprite = aura.sprite; originalAuraPosition = aura.transform.localPosition;
            originalAuraRotation = aura.transform.localRotation; originalAuraScale = aura.transform.localScale; auraCached = true;
        }
        apexSprite = sprite; progress = sprite != null ? SpriteFacingRules.Blend(amount) : 0;
        Refresh();
    }

    private void LateUpdate()
    {
        Ensure(); if (!ready) return;
        if (!eliminated && movement != null)
        {
            Vector2 input = movement.MovementIntent;
            heading = SpriteFacingRules.Heading(input.x, input.y, heading);
        }
        Refresh();
    }

    private void Refresh()
    {
        if (!ready) return;
        Vector3 normalCenter = FlippedCenter(source);
        visualRoot.localPosition = normalCenter;
        visualRoot.rotation = Quaternion.Euler(0, 0, heading);
        normal.sprite = source.sprite; normal.sharedMaterial = source.sharedMaterial;
        normal.flipX = source.flipX; normal.flipY = source.flipY;
        normal.sortingLayerID = source.sortingLayerID; normal.sortingOrder = source.sortingOrder;
        normal.transform.localRotation = Quaternion.Euler(0, 0, normalFacingOffset);
        normal.transform.localPosition = -(normal.transform.localRotation * normalCenter);
        Color tint = source.color; tint.a *= 1 - progress; normal.color = tint;
        normal.enabled = !eliminated && progress < 1;
        apex.enabled = !eliminated && apexSprite != null && progress > 0;
        if (apexSprite != null)
        {
            float size = SpriteFacingRules.FitHeight(source.sprite.bounds.size.y, apexSprite.bounds.size.y, apexSizeMultiplier);
            apex.sprite = apexSprite; apex.sharedMaterial = source.sharedMaterial;
            apex.sortingLayerID = source.sortingLayerID; apex.sortingOrder = source.sortingOrder + 1;
            apex.color = new Color(1, 1, 1, progress); // Preserve the native Apex artwork colors.
            apex.transform.localScale = Vector3.one * size;
            apex.transform.localRotation = Quaternion.Euler(0, 0, apexFacingOffset);
            apex.transform.localPosition = -(apex.transform.localRotation * (apexSprite.bounds.center * size));
            if (aura != null)
            {
                aura.sprite = apexSprite;
                aura.transform.rotation = apex.transform.rotation;
                Vector3 worldScale = apex.transform.lossyScale * 1.3f, parentScale = aura.transform.parent.lossyScale;
                aura.transform.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, 1);
                aura.transform.position = visualRoot.position - aura.transform.TransformVector(apexSprite.bounds.center);
            }
        }
        else if (auraCached && aura != null)
        {
            aura.sprite = originalAuraSprite; aura.transform.localScale = originalAuraScale;
            aura.transform.localPosition = originalAuraPosition;
            aura.transform.rotation = Quaternion.Euler(0, 0, heading + normalFacingOffset);
        }
    }

    private static Vector3 FlippedCenter(SpriteRenderer renderer)
    {
        Vector3 center = renderer.sprite.bounds.center;
        if (renderer.flipX) center.x = -center.x; if (renderer.flipY) center.y = -center.y;
        return center;
    }

    public void HideForElimination()
    {
        Ensure(); eliminated = true;
        if (normal != null) normal.enabled = false;
        if (apex != null) apex.enabled = false;
        if (source != null) source.enabled = false;
    }

    private void OnDisable()
    {
        if (!ready) return;
        normal.enabled = apex.enabled = false;
        if (source != null) source.enabled = sourceWasEnabled && !eliminated;
        if (auraCached && aura != null)
        {
            aura.sprite = originalAuraSprite; aura.transform.localPosition = originalAuraPosition;
            aura.transform.localRotation = originalAuraRotation; aura.transform.localScale = originalAuraScale;
        }
    }
    private void OnDestroy() { if (visualRoot != null) Destroy(visualRoot.gameObject); }
}
