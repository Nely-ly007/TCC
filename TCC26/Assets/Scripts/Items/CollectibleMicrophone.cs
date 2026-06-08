using UnityEngine;

/// <summary>
/// POP ADVENTURE - CollectibleMicrophone
/// Microfone: concede vida extra (revive automático).
/// </summary>
public class CollectibleMicrophone : MonoBehaviour
{
    [SerializeField] private AudioClip collectSFX;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController.Instance?.PickupMicrophone();
        if (collectSFX != null)
            AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        Destroy(gameObject);
    }
}

