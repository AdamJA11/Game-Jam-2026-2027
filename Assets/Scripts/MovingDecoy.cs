using UnityEngine;

// Deliberately not a PlayerCombatant or TopDownPlayerMovement.
public sealed class MovingDecoy : MonoBehaviour
{
    private HuntRoundManager round;
    private PlayerCombatant owner;
    private TopDownPlayerMovement movement;
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual, aura;
    private double deadline;
    private float offset;
    public bool Active => gameObject.activeSelf;
    public double Deadline => deadline;
    public BoxCollider2D Hitbox => hitbox;

    public void Prepare(HuntRoundManager manager)
    {
        round = manager;
        body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var reveal = new GameObject("Reveal", typeof(BoxCollider2D)); reveal.transform.SetParent(transform, false);
        reveal.layer = gameObject.layer;
        var trigger = reveal.GetComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = Vector2.one * 1.1f;
        var outline = new GameObject("DecoyAura", typeof(SpriteRenderer)); outline.transform.SetParent(transform, false); outline.transform.localScale = Vector3.one * 1.3f;
        outline.layer = gameObject.layer;
        aura = outline.GetComponent<SpriteRenderer>();
        for (int i = 0; i < 3; i++) Physics2D.IgnoreCollision(hitbox, round.GetPlayer(i).GetComponent<BoxCollider2D>(), true);
        Stop();
    }

    public void Begin(PlayerCombatant player, float angle, float lifetime)
    {
        owner = player; movement = player.GetComponent<TopDownPlayerMovement>(); offset = angle; deadline = Time.timeAsDouble + lifetime;
        var source = player.GetComponent<SpriteRenderer>();
        transform.position = player.transform.position; transform.rotation = player.transform.rotation;
        Vector3 worldScale = player.transform.lossyScale, parentScale = transform.parent.lossyScale;
        transform.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, worldScale.z / parentScale.z);
        visual.sprite = source.sprite; visual.sharedMaterial = source.sharedMaterial; visual.color = source.color;
        visual.flipX = source.flipX; visual.flipY = source.flipY; visual.sortingLayerID = source.sortingLayerID; visual.sortingOrder = source.sortingOrder;
        hitbox.size = player.GetComponent<BoxCollider2D>().size; hitbox.offset = player.GetComponent<BoxCollider2D>().offset;
        hitbox.sharedMaterial = player.GetComponent<BoxCollider2D>().sharedMaterial;
        var reveal = transform.Find("Reveal").GetComponent<BoxCollider2D>(); reveal.size = hitbox.size * 1.1f; reveal.offset = hitbox.offset;
        var sourceAura = player.transform.Find("ApexAura");
        aura.gameObject.SetActive(sourceAura != null && sourceAura.gameObject.activeSelf);
        if (sourceAura != null)
        {
            var renderer = sourceAura.GetComponent<SpriteRenderer>();
            if (renderer != null) { aura.sprite = renderer.sprite; aura.sharedMaterial = renderer.sharedMaterial; aura.color = renderer.color; aura.sortingLayerID = renderer.sortingLayerID; aura.sortingOrder = renderer.sortingOrder; }
        }
        body.simulated = true; gameObject.SetActive(true); body.position = player.transform.position; body.velocity = Vector2.zero;
        // Ignore state can be lost when colliders were inactive. Reapply after activation.
        for (int i = 0; i < 3; i++) Physics2D.IgnoreCollision(hitbox, round.GetPlayer(i).GetComponent<BoxCollider2D>(), true);
        foreach (var other in transform.parent.GetComponentsInChildren<MovingDecoy>(true))
            if (other != this && other.Hitbox != null) Physics2D.IgnoreCollision(hitbox, other.Hitbox, true);
    }

    private void FixedUpdate()
    {
        if (owner == null || owner.IsEliminated || round.Rules.State == HuntRules.Phase.RoundOver || Time.timeAsDouble >= deadline) { Stop(); return; }
        if (!round.GameplayAvailable || movement.InputBlocked || !movement.enabled) { body.velocity = Vector2.zero; return; }
        Vector2 intent = movement.MovementIntent;
        RandomItemRules.Rotate(intent.x, intent.y, offset, out float x, out float y);
        Vector2 target = new Vector2(x, y) * movement.EffectiveMoveSpeed;
        body.velocity = Vector2.MoveTowards(body.velocity, target, (intent.sqrMagnitude > 0 ? movement.Acceleration : movement.Deceleration) * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other) { Reveal(other); }
    private void OnTriggerStay2D(Collider2D other) { Reveal(other); }
    private void Reveal(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerCombatant>();
        if (round != null && round.GameplayAvailable && player != null && player != owner && player.Round == round && !player.IsEliminated) Stop();
    }
    public void Stop()
    {
        if (body != null) { body.velocity = Vector2.zero; body.simulated = false; }
        gameObject.SetActive(false);
    }
}
