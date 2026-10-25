using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class DisappearOnPlayerTouch2D : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // On utilise collision.gameObject au lieu de other
        if (collision.gameObject.GetComponentInParent<TopDownPlayerMovement>() != null)
        {
            // Toujours lancer l'effet avant de désactiver l'objet
            CameraShake.Instance.ShakeCam(0.3f, 0.2f); 
            gameObject.SetActive(false);
        }
    }
}
