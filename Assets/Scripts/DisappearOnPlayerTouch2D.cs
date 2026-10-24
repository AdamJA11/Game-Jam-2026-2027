using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class DisappearOnPlayerTouch2D : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<TopDownPlayerMovement>() != null)
            gameObject.SetActive(false);
    }
}
