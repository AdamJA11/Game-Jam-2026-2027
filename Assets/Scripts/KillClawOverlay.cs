using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class KillClawOverlay : MonoBehaviour
{
    [SerializeField] private HuntRoundManager round;
    [SerializeField] private Camera[] views = new Camera[3];
    [SerializeField] private RectTransform[] viewportMasks = new RectTransform[3];
    [SerializeField] private Image[] clawImages = new Image[3];
    [SerializeField] private Sprite clawSprite = null;
    [SerializeField] private bool effectEnabled = true;
    [SerializeField, Min(.01f)] private float duration = .35f;
    [SerializeField, Range(0, 1)] private float killerAlpha = .22f, victimAlpha = .38f;
    [SerializeField, Range(.01f, 1)] private float killerSize = .45f, victimSize = .6f;
    [SerializeField] private float angle = 0;
    [SerializeField, Min(.01f)] private float startScale = .95f, endScale = 1.05f;
    private readonly KillClawEvents events = new KillClawEvents();
    private readonly KillClawAnimation[] animations = { new KillClawAnimation(), new KillClawAnimation(), new KillClawAnimation() };
    private readonly Vector3[] deathPositions = new Vector3[3];
    private bool ready;

    public void Configure(HuntRoundManager manager, Camera[] cameras, RectTransform[] masks, Image[] images, Sprite sprite)
    { round = manager; views = cameras; viewportMasks = masks; clawImages = images; clawSprite = sprite; }

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (round == null || round.gameObject != gameObject || clawSprite == null || views == null || views.Length != 3
            || viewportMasks == null || viewportMasks.Length != 3 || clawImages == null || clawImages.Length != 3)
        { error = "Assign this round manager, claw sprite, and three camera/masked-image pairs."; return false; }
        if (!Positive(duration) || !Unit(killerAlpha) || !Unit(victimAlpha) || !Positive(killerSize) || killerSize > 1
            || !Positive(victimSize) || victimSize > 1 || !Finite(angle) || !Positive(startScale) || !Positive(endScale))
        { error = "Use finite positive timing/sizes/scales, alpha between 0 and 1, and a finite angle."; return false; }
        for (int i = 0; i < 3; i++)
        {
            if (round.GetPlayer(i) == null || views[i] == null || viewportMasks[i] == null || clawImages[i] == null
                || !views[i].orthographic || views[i].GetComponent<LocalPlayerCameraFollow>() == null || views[i].GetComponent<LocalPlayerCameraFollow>().Target != round.GetPlayer(i).transform
                || viewportMasks[i].GetComponent<RectMask2D>() == null || !viewportMasks[i].GetComponent<RectMask2D>().enabled
                || !viewportMasks[i].gameObject.activeInHierarchy || !clawImages[i].transform.IsChildOf(viewportMasks[i])
                || views[i].gameObject.scene != gameObject.scene || clawImages[i].gameObject.scene != gameObject.scene)
            { error = "Match each claw slot to its player's camera and RectMask2D parent."; return false; }
            var canvas = viewportMasks[i].GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            { error = "Claw masks must be under the gameplay screen-space overlay Canvas."; return false; }
            for (int j = 0; j < i; j++) if (views[i] == views[j] || viewportMasks[i] == viewportMasks[j] || clawImages[i] == clawImages[j])
            { error = "Each camera, mask, and claw image must be distinct."; return false; }
        }
        return true;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Positive(float value) => Finite(value) && value > 0;
    private static bool Unit(float value) => Finite(value) && value >= 0 && value <= 1;

    private void Awake()
    {
        CancelAll();
        if (!ValidateConfiguration(out string error))
        { Debug.LogWarning("KillClawOverlay: " + error + " Run Phase 4c setup. Gameplay will continue.", this); enabled = false; return; }
        for (int i = 0; i < 3; i++)
        {
            clawImages[i].sprite = clawSprite; clawImages[i].raycastTarget = false; clawImages[i].maskable = true;
            clawImages[i].preserveAspect = true; clawImages[i].type = Image.Type.Simple;
            var rect = clawImages[i].rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        }
        ready = true;
    }

    // Invoked only after the manager commits the entire collision/escape batch.
    public void ObserveCommittedKills(HuntRules rules)
    {
        KillClawBatch batch = events.Observe(rules);
        if (!ready || !isActiveAndEnabled || !effectEnabled) return;
        for (int i = 0; i < 3; i++)
        {
            KillClawRole role = batch.ForPlayer(i);
            if (role == KillClawRole.None) continue;
            int victim = batch.VictimForPlayer(rules, i);
            var player = victim >= 0 ? round.GetPlayer(victim) : null;
            if (player == null) continue;
            // Capture before ApplyState hides the victim. Never chase the killer's position.
            var facing = player.GetComponent<PlayerVisualFacing>();
            deathPositions[i] = facing != null ? facing.VisibleBounds.center : player.transform.position;
            animations[i].Start(role, Time.unscaledTimeAsDouble, duration);
            Render(i, Time.unscaledTimeAsDouble);
            if (!ready) return;
        }
    }

    private void LateUpdate()
    {
        if (!ready) return;
        if (!effectEnabled) { CancelAll(); return; }
        for (int i = 0; i < 3; i++) Render(i, Time.unscaledTimeAsDouble);
    }

    private void Render(int id, double now)
    {
        if (!ready) return;
        if (clawImages == null || clawImages.Length != 3 || views == null || views.Length != 3 || viewportMasks == null || viewportMasks.Length != 3
            || clawImages[id] == null || views[id] == null || viewportMasks[id] == null || clawSprite == null)
        {
            ready = false; CancelAll(); enabled = false;
            Debug.LogWarning("KillClawOverlay: a presentation reference became unavailable. Effect disabled; gameplay continues.", this);
            return;
        }
        var image = clawImages[id];
        if (!animations[id].Active(now)) { Hide(id); return; }
        Rect cameraRect = views[id].rect;
        var mask = viewportMasks[id];
        if (mask.anchorMin != cameraRect.min) mask.anchorMin = cameraRect.min;
        if (mask.anchorMax != cameraRect.max) mask.anchorMax = cameraRect.max;
        float aspect = clawSprite.rect.width / Mathf.Max(1, clawSprite.rect.height);
        float largest = Mathf.Max(killerSize, victimSize), maxScale = Mathf.Max(startScale, endScale);
        float baseWidth = mask.rect.width;
        float width = baseWidth * largest, height = width / aspect;
        float radians = angle * Mathf.Deg2Rad, cos = Mathf.Abs(Mathf.Cos(radians)), sin = Mathf.Abs(Mathf.Sin(radians));
        float fit = Mathf.Min(1, mask.rect.width * .94f / Mathf.Max(.01f, (width * cos + height * sin) * maxScale),
            mask.rect.height * .6f / Mathf.Max(.01f, (width * sin + height * cos) * maxScale));
        float fraction = animations[id].Role == KillClawRole.Victim ? victimSize : killerSize;
        width = baseWidth * fit * fraction;
        var rect = image.rectTransform;
        rect.sizeDelta = new Vector2(width, width / aspect);
        Vector3 cameraPosition = views[id].GetComponent<LocalPlayerCameraFollow>().GetUnshakenPosition();
        KillClawPositioning.Project(deathPositions[id].x, deathPositions[id].y, cameraPosition.x, cameraPosition.y,
            views[id].orthographicSize, views[id].aspect, out float x, out float y);
        // localPosition uses the mask's actual rectangle, including non-centered mask pivots.
        rect.localPosition = new Vector3(mask.rect.xMin + x * mask.rect.width, mask.rect.yMin + y * mask.rect.height, 0);
        rect.localRotation = Quaternion.Euler(0, 0, angle);
        rect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, animations[id].Progress(now));
        float alpha = animations[id].Alpha(now, animations[id].Role == KillClawRole.Victim ? victimAlpha : killerAlpha);
        image.color = new Color(1, 1, 1, alpha); image.enabled = alpha > 0;
    }

    private void Hide(int id)
    {
        if (clawImages == null || id >= clawImages.Length || clawImages[id] == null) return;
        var image = clawImages[id];
        image.enabled = false; image.color = new Color(1, 1, 1, 0); image.rectTransform.localScale = Vector3.one;
    }
    public void CancelAll() { for (int i = 0; i < 3; i++) { animations[i].Cancel(); Hide(i); } }
    private void OnDisable() { CancelAll(); }
}
