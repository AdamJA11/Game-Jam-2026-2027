using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class EscapeDoor : MonoBehaviour
{
    [SerializeField] private HuntRoundManager round;
    [SerializeField] private SpriteRenderer frame;
    [SerializeField] private SpriteRenderer opening;
    private BoxCollider2D trigger;
    public bool Configured => enabled && gameObject.activeInHierarchy && round != null && trigger != null && trigger.isTrigger && trigger.enabled;
    public void Configure(HuntRoundManager manager, SpriteRenderer border, SpriteRenderer interior)
    { round = manager; frame = border; opening = interior; }

    private void Awake()
    {
        trigger = GetComponent<BoxCollider2D>();
        if (round == null || frame == null || opening == null || !trigger.isTrigger)
        { Debug.LogWarning("EscapeDoor: run Phase 3d setup and assign the trigger/visuals.", this); enabled = false; }
        if (frame != null) frame.enabled = false;
        if (opening != null) opening.enabled = false;
    }

    // Read current geometry; no dependency on callback order or a fresh trigger entry.
    public int Occupants()
    {
        if (!Configured) return 0;
        int mask = 0;
        for (int i = 0; i < 3; i++)
        {
            var player = round.GetPlayer(i);
            if (player == null || !round.Rules.IsAlive(i)) continue;
            var hitbox = player.GetComponent<BoxCollider2D>();
            if (!hitbox.enabled || !hitbox.gameObject.activeInHierarchy) continue;
            var distance = trigger.Distance(hitbox);
            if (distance.isValid && distance.distance <= 0) mask |= 1 << i;
        }
        return mask;
    }

    private void LateUpdate()
    {
        if (!Configured || round.Rules == null) return;
        bool visible = round.Rules.ApexId >= 0 && round.Rules.State != HuntRules.Phase.Opening && round.Rules.State != HuntRules.Phase.TriangleHunt;
        frame.enabled = opening.enabled = visible;
        bool escaped = round.Rules.Outcome == HuntRules.Result.EscapedThroughDoor;
        frame.color = escaped ? Color.green : Color.cyan;
        opening.color = escaped ? new Color(.2f, 1, .5f, .25f) : new Color(.02f, .04f, .06f, 1);
    }

    private void OnDisable() { if (frame != null) frame.enabled = false; if (opening != null) opening.enabled = false; }
}
