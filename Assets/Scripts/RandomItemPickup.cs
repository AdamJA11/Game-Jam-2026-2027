using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class RandomItemPickup : MonoBehaviour
{
    private bool consumed;
    public bool Available => !consumed && enabled && gameObject.activeInHierarchy;
    public int Occupants(HuntRoundManager round)
    {
        if (!Available) return 0;
        var trigger = GetComponent<BoxCollider2D>();
        if (!trigger.enabled || !trigger.isTrigger) return 0;
        int mask = 0;
        for (int i = 0; i < 3; i++)
        {
            if (!round.Rules.IsAlive(i)) continue;
            var player = round.GetPlayer(i); if (player == null) continue;
            var hitbox = player.GetComponent<BoxCollider2D>();
            if (!hitbox.enabled || !player.gameObject.activeInHierarchy) continue;
            var distance = trigger.Distance(hitbox);
            if (distance.isValid && distance.distance <= 0) mask |= 1 << i;
        }
        return mask;
    }
    public void Consume() { consumed = true; gameObject.SetActive(false); }
}
