using UnityEngine;

[RequireComponent(typeof(TopDownPlayerMovement), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class PlayerCombatant : MonoBehaviour
{
    [SerializeField] private int playerId;
    [SerializeField] private HuntRoundManager round;
    [SerializeField] private LocalPlayerCameraFollow follow;
    [SerializeField] private GameObject apexAura;
    private TopDownPlayerMovement movement;
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual;
    private PlayerDeathBurst deathBurst;
    private bool pinned;
    private RigidbodyConstraints2D previousConstraints;
    public int PlayerId => playerId;
    public bool IsEliminated { get; private set; }
    public HuntRoundManager Round => round;

    public void Configure(int id, HuntRoundManager manager, LocalPlayerCameraFollow cameraFollow, GameObject aura)
    {
        playerId = id; round = manager; follow = cameraFollow; apexAura = aura;
    }

    public bool IsConfigured(HuntRoundManager manager, int expectedId)
    {
        Cache();
        return playerId == expectedId && round == manager && movement != null && body != null && hitbox != null
            && !hitbox.isTrigger && visual != null && follow != null && follow.Target == transform && apexAura != null;
    }

    private void Cache()
    {
        if (movement == null) movement = GetComponent<TopDownPlayerMovement>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (hitbox == null) hitbox = GetComponent<BoxCollider2D>();
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (deathBurst == null) deathBurst = GetComponent<PlayerDeathBurst>();
    }

    public void ApplyState(bool alive, bool allowMovement, bool pinPosition, bool apex, float apexMultiplier = 1f)
    {
        Cache();
        movement.SetSpeedMultiplier(MovementSpeedModifier.ForCombatState(alive, allowMovement, apex, apexMultiplier));
        if (!alive && !IsEliminated)
        {
            IsEliminated = true;
            // Capture the intact visual before hiding it. Effects never decide the kill.
            if (deathBurst != null)
                deathBurst.Play(round != null ? round.GetKillerPosition(playerId) : (Vector2)transform.position);
            else
                Debug.LogWarning("PlayerCombatant: no death burst configured. Run the Phase 6 setup outside Play mode.", this);
            movement.SetInputBlocked(true);
            body.velocity = Vector2.zero; body.angularVelocity = 0;
            hitbox.enabled = false;
            body.simulated = false;
            visual.enabled = false;
            follow.SetAutomaticFollowSuspended(true);
        }
        if (alive)
        {
            if (pinPosition && !pinned) { previousConstraints = body.constraints; body.constraints = RigidbodyConstraints2D.FreezeAll; pinned = true; }
            else if (!pinPosition && pinned) { body.constraints = previousConstraints; pinned = false; }
            bool blocked = !allowMovement;
            if (movement.InputBlocked != blocked) movement.SetInputBlocked(blocked);
            if (!allowMovement) { body.velocity = Vector2.zero; body.angularVelocity = 0; }
        }
        if (apexAura != null) apexAura.SetActive(alive && apex);
    }

    private void OnCollisionEnter2D(Collision2D collision) { Report(collision); }
    public void HoldCamera() { if (follow != null) follow.SetAutomaticFollowSuspended(true); }
    private void OnCollisionStay2D(Collision2D collision) { Report(collision); }
    private void OnCollisionExit2D(Collision2D collision)
    {
        var other = collision.collider.GetComponentInParent<PlayerCombatant>();
        if (round != null && other != null && other.round == round) round.EndContact(this, other);
    }
    private void Report(Collision2D collision)
    {
        if (round == null || IsEliminated) return;
        var other = collision.collider.GetComponentInParent<PlayerCombatant>();
        if (other != null && !other.IsEliminated && other.round == round) round.ReportContact(this, other);
    }
}
