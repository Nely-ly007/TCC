using UnityEngine;

/// <summary>
/// Nota musical que recupera vida do jogador.
/// </summary>
public class CollectibleMusicNote : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField] private int healAmount = 15;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSFX;

    [Header("Animation")]
    [SerializeField] private float floatSpeed = 0.5f;

    private bool isCollected;

    private void Update()
    {
        if (isCollected)
            return;

        transform.position +=
            Vector3.up * floatSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
            return;

        if (!other.CompareTag("Player"))
            return;

        isCollected = true;

        PlayerController.Instance?.Heal(healAmount);

        if (collectSFX != null)
        {
            AudioSource.PlayClipAtPoint(
                collectSFX,
                transform.position
            );
        }

        Destroy(gameObject);
    }
}