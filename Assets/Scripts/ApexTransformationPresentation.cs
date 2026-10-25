using UnityEngine;

[DisallowMultipleComponent]
public sealed class ApexTransformationPresentation : MonoBehaviour
{
    private const int RingSegments = 64;
    [SerializeField] private HuntRoundManager round;
    [SerializeField] private PlayerCombatant[] players = new PlayerCombatant[3];
    [SerializeField] private SpriteRenderer[] auras = new SpriteRenderer[3];
    [SerializeField] private SpriteRenderer[] barBackgrounds = new SpriteRenderer[3];
    [SerializeField] private SpriteRenderer[] barFills = new SpriteRenderer[3];
    [SerializeField] private LineRenderer ring;
    [SerializeField] private Rect playableBounds = new Rect(-20, -12, 40, 24);
    [SerializeField] private Color apexBodyColor = new Color(.082f, .063f, .082f, 1);
    [SerializeField] private Color apexRedColor = new Color(1, .188f, .188f, 1);
    [SerializeField, Min(.1f)] private float pulseDuration = 2f;
    [SerializeField, Min(.01f)] private float ringWidth = .25f;
    [SerializeField, Min(.01f)] private float barWidth = 1f;
    [SerializeField, Min(.01f)] private float barHeight = .12f;
    [SerializeField, Min(0)] private float barGap = .2f;
    private readonly SpriteRenderer[] bodies = new SpriteRenderer[3];
    private readonly TopDownPlayerMovement[] movements = new TopDownPlayerMovement[3];
    private readonly Color[] originalBody = new Color[3];
    private readonly Color[] originalAura = new Color[3];
    private readonly Vector3[] unitCircle = new Vector3[RingSegments];
    private readonly ApexWarningTimeline timeline = new ApexWarningTimeline();
    private bool initialized;
    private bool warned;
    private Vector3 origin;
    private float startRadius, endRadius;

    public bool WaveActive => initialized && round != null && round.Rules != null && enabled && gameObject.activeInHierarchy
        && timeline.Active(round.Rules.State, Time.timeAsDouble, pulseDuration);

    public void Configure(HuntRoundManager manager, PlayerCombatant[] characters, SpriteRenderer[] outlines,
        SpriteRenderer[] backgrounds, SpriteRenderer[] fills, LineRenderer warningRing, Rect bounds)
    { round = manager; players = characters; auras = outlines; barBackgrounds = backgrounds; barFills = fills; ring = warningRing; playableBounds = bounds; }

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (round == null || round.gameObject != gameObject || ring == null || ring.sharedMaterial == null || ring.gameObject.scene != gameObject.scene
            || players == null || players.Length != 3 || auras == null || auras.Length != 3
            || barBackgrounds == null || barBackgrounds.Length != 3 || barFills == null || barFills.Length != 3)
        { error = "Assign this scene's round, three players/auras/bars, and one ring with a shared material."; return false; }
        if (!Positive(playableBounds.width) || !Positive(playableBounds.height) || !Finite(playableBounds.x) || !Finite(playableBounds.y)
            || !Positive(pulseDuration) || !Positive(ringWidth) || !Positive(barWidth) || !Positive(barHeight)
            || !Finite(playableBounds.xMax) || !Finite(playableBounds.yMax)
            || !Finite(barGap) || barGap < 0 || !Finite(apexBodyColor) || !Finite(apexRedColor))
        { error = "Use finite colors/bounds, positive effect sizes/duration, and a non-negative bar gap."; return false; }
        for (int i = 0; i < 3; i++)
        {
            if (players[i] == null || players[i] != round.GetPlayer(i) || auras[i] == null || barBackgrounds[i] == null || barFills[i] == null
                || players[i].GetComponent<SpriteRenderer>() == null || players[i].GetComponent<SpriteRenderer>().sprite == null
                || barBackgrounds[i].sprite == null || barFills[i].sprite == null
                || !auras[i].transform.IsChildOf(players[i].transform)
                || !barBackgrounds[i].transform.IsChildOf(players[i].transform)
                || barFills[i].transform.parent != barBackgrounds[i].transform)
            { error = "Player " + (i + 1) + " needs its matching body, aura, and child background/fill sprites."; return false; }
        }
        return true;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Finite(Color value) => Finite(value.r) && Finite(value.g) && Finite(value.b) && Finite(value.a);
    private static bool Positive(float value) => Finite(value) && value > 0;

    // Called after the manager has captured every original identity color, including from early Awake.
    public void Observe()
    {
        if (!enabled || !gameObject.activeInHierarchy || (warned && !initialized)) return;
        if (!initialized)
        {
            if (!ValidateConfiguration(out string error))
            {
                if (!warned) { warned = true; Debug.LogWarning("ApexTransformationPresentation: " + error, this); Hide(); }
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                bodies[i] = players[i].GetComponent<SpriteRenderer>();
                movements[i] = players[i].GetComponent<TopDownPlayerMovement>();
                originalBody[i] = bodies[i].color;
                originalAura[i] = auras[i].color;
            }
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2 / RingSegments;
                unitCircle[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            }
            ring.useWorldSpace = true; ring.loop = true; ring.positionCount = RingSegments;
            initialized = true;
            Hide();
        }
        if (round.Rules == null) return;
        var rules = round.Rules;
        if (rules.State == HuntRules.Phase.RoundOver) { timeline.Cancel(); Hide(); return; }
        if (rules.ApexId < 0 || !timeline.TryStart(rules.State, Time.timeAsDouble)) return;
        origin = players[rules.ApexId].transform.position;
        startRadius = Mathf.Max(bodies[rules.ApexId].bounds.extents.x, bodies[rules.ApexId].bounds.extents.y) + .15f;
        float survivorSpeed = 0;
        for (int i = 0; i < 3; i++) if (rules.IsAlive(i) && i != rules.ApexId)
            survivorSpeed = Mathf.Max(survivorSpeed, movements[i].EffectiveMoveSpeed);
        endRadius = Mathf.Max(startRadius, ApexTransformationMath.RequiredRadius(origin.x, origin.y,
            playableBounds.xMin, playableBounds.yMin, playableBounds.xMax, playableBounds.yMax, survivorSpeed * pulseDuration + 2));
    }

    private void LateUpdate()
    {
        if (!initialized || round == null || round.Rules == null) return;
        var rules = round.Rules;
        float progress = ApexTransformationMath.Progress(rules.TransformRemaining);
        for (int i = 0; i < 3; i++)
        {
            bool apex = rules.IsAlive(i) && rules.ApexId == i;
            bool transforming = apex && rules.State == HuntRules.Phase.Transforming;
            bodies[i].color = apex ? Color.Lerp(originalBody[i], apexBodyColor, transforming ? progress : 1) : originalBody[i];
            auras[i].color = apex ? apexRedColor : originalAura[i];
            barBackgrounds[i].enabled = transforming;
            barFills[i].enabled = transforming && progress > 0;
            if (transforming) PositionBar(i, progress);
        }
        ring.enabled = WaveActive;
        if (!ring.enabled) return;
        float t = Mathf.Clamp01(timeline.Age(Time.timeAsDouble) / pulseDuration);
        float radius = Mathf.Lerp(startRadius, endRadius, t);
        ring.widthMultiplier = ringWidth;
        Color tint = apexRedColor;
        tint.a *= .65f * Mathf.Clamp01((1 - t) / .2f);
        ring.startColor = ring.endColor = tint;
        for (int i = 0; i < RingSegments; i++) ring.SetPosition(i, origin + unitCircle[i] * radius);
    }

    private void PositionBar(int i, float progress)
    {
        var background = barBackgrounds[i];
        Bounds bounds = bodies[i].bounds;
        background.transform.position = new Vector3(bounds.center.x, bounds.max.y + barGap + barHeight / 2, players[i].transform.position.z);
        background.transform.rotation = Quaternion.identity;
        Vector3 parentScale = background.transform.parent.lossyScale;
        Vector3 spriteSize = background.sprite.bounds.size;
        background.transform.localScale = new Vector3(barWidth / Mathf.Max(.0001f, Mathf.Abs(parentScale.x * spriteSize.x)),
            barHeight / Mathf.Max(.0001f, Mathf.Abs(parentScale.y * spriteSize.y)), 1);
        var fill = barFills[i];
        // Setup uses centered square sprites; the fill's left edge remains fixed as width grows.
        fill.transform.localScale = new Vector3(.94f * progress, .7f, 1);
        fill.transform.localPosition = new Vector3(spriteSize.x * (-.47f + .47f * progress), 0, -.01f);
        fill.color = apexRedColor;
    }

    private void Hide()
    {
        if (ring != null) ring.enabled = false;
        if (barBackgrounds != null) foreach (var bar in barBackgrounds) if (bar != null) bar.enabled = false;
        if (barFills != null) foreach (var bar in barFills) if (bar != null) bar.enabled = false;
    }

    private void OnDisable()
    {
        timeline.Cancel(); Hide();
        if (!initialized) return;
        for (int i = 0; i < 3; i++)
        {
            if (bodies[i] != null) bodies[i].color = originalBody[i];
            if (auras[i] != null) auras[i].color = originalAura[i];
        }
    }
}
