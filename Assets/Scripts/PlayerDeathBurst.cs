using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerDeathBurst : MonoBehaviour
{
    [SerializeField, Min(0)] private float minimumSpeed = 2.5f;
    [SerializeField, Min(0)] private float maximumSpeed = 5f;
    [SerializeField, Min(0)] private float maximumSpin = 540f;
    [SerializeField, Min(.01f)] private float lifetime = .9f;
    [SerializeField, Min(0)] private float fadeDuration = .5f;
    [SerializeField, Range(0, 1)] private float killerDirectionBias = .3f;
    private readonly SpriteRenderer[] pieces = new SpriteRenderer[DeathBurstTiming.PieceCount];
    private readonly Vector3[] origins = new Vector3[DeathBurstTiming.PieceCount];
    private readonly Vector2[] velocities = new Vector2[DeathBurstTiming.PieceCount];
    private readonly float[] spins = new float[DeathBurstTiming.PieceCount];
    private SpriteRenderer source;
    private Color tint;
    private Quaternion rotation;
    private float age;
    private bool played;
    private bool ready;

    public bool IsBurstActive { get; private set; }
    public float Lifetime => lifetime;

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null || renderer.drawMode != SpriteDrawMode.Simple)
            error = "Assign a square sprite using Simple draw mode.";
        else if (!Finite(minimumSpeed) || !Finite(maximumSpeed) || minimumSpeed < 0 || maximumSpeed < minimumSpeed
            || !Finite(maximumSpin) || maximumSpin < 0 || !Finite(lifetime) || lifetime <= 0
            || !Finite(fadeDuration) || fadeDuration < 0 || fadeDuration > lifetime
            || !Finite(killerDirectionBias) || killerDirectionBias < 0 || killerDirectionBias > 1)
            error = "Use finite non-negative speeds/spin, positive lifetime, fade <= lifetime, and bias between 0 and 1.";
        return error == null;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        if (!ValidateConfiguration(out string error))
        { Debug.LogWarning("PlayerDeathBurst: " + error, this); enabled = false; return; }
        for (int i = 0; i < pieces.Length; i++)
        {
            var part = new GameObject("DeathFragment" + i);
            part.layer = gameObject.layer;
            part.transform.SetParent(transform, false);
            pieces[i] = part.AddComponent<SpriteRenderer>();
            part.SetActive(false);
        }
        ready = true;
    }

    public void Play(Vector2 killerPosition)
    {
        if (!ready || played || !isActiveAndEnabled) return;
        played = true;
        age = 0;
        tint = source.color;
        rotation = transform.rotation;
        Bounds bounds = source.sprite.bounds;
        Vector3 center = bounds.center;
        if (source.flipX) center.x = -center.x;
        if (source.flipY) center.y = -center.y;
        Vector2 away = ((Vector2)transform.position - killerPosition).normalized;
        for (int i = 0; i < pieces.Length; i++)
        {
            var piece = pieces[i];
            int x = i % 3 - 1, y = i / 3 - 1;
            Vector3 offset = new Vector3(x * bounds.size.x / 3f, y * bounds.size.y / 3f, 0);
            // Account for non-centered pivots: the scaled piece has its own scaled center.
            piece.transform.localPosition = center * (2f / 3f) + offset;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = Vector3.one / 3f;
            origins[i] = piece.transform.position;
            Vector2 radial = transform.TransformVector(offset);
            if (radial.sqrMagnitude < .000001f) radial = Random.insideUnitCircle;
            if (radial.sqrMagnitude < .000001f) radial = Vector2.right;
            Vector2 direction = radial.normalized + away * killerDirectionBias + Random.insideUnitCircle * .15f;
            velocities[i] = direction.normalized * Random.Range(minimumSpeed, maximumSpeed);
            spins[i] = Random.Range(-maximumSpin, maximumSpin);
            piece.sprite = source.sprite;
            piece.sharedMaterial = source.sharedMaterial;
            piece.flipX = source.flipX; piece.flipY = source.flipY;
            piece.sortingLayerID = source.sortingLayerID;
            piece.sortingOrder = Mathf.Min(source.sortingOrder + 1, short.MaxValue);
            piece.color = tint;
            piece.gameObject.SetActive(true);
        }
        IsBurstActive = true;
    }

    private void Update()
    {
        if (!IsBurstActive) return;
        age += Time.deltaTime;
        if (DeathBurstTiming.Complete(age, lifetime)) { Stop(); return; }
        float travel = DeathBurstTiming.Travel(age);
        Color color = tint;
        color.a *= DeathBurstTiming.Opacity(age, lifetime, fadeDuration);
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].transform.position = origins[i] + (Vector3)(velocities[i] * travel);
            pieces[i].transform.rotation = rotation * Quaternion.Euler(0, 0, spins[i] * age);
            pieces[i].color = color;
        }
    }

    private void Stop()
    {
        IsBurstActive = false;
        for (int i = 0; i < pieces.Length; i++)
            if (pieces[i] != null) pieces[i].gameObject.SetActive(false);
    }

    private void OnDisable() { Stop(); }
}
