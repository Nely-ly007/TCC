using UnityEngine;
using System.Collections;

/// <summary>
/// Colecionável de Vinil.
/// Ao ser coletado, adiciona vinis ao GameManager.
/// Também possui efeito de rotação, flutuação e atração pelo jogador.
/// </summary>
public class CollectibleVinyl : MonoBehaviour
{
    [Header("Vinyl")]
    [SerializeField] private int value = 1;

    [Header("Attraction")]
    [SerializeField] private float attractRadius = 2f;
    [SerializeField] private float attractSpeed = 8f;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSFX;

    [Header("Animation")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float bobSpeed = 3f;

    private bool isCollected;
    private Transform player;
    private Vector3 startPosition;

    private void Start()
    {
        player = PlayerController.Instance?.transform;
        startPosition = transform.position;

        StartCoroutine(RotateAndBob());
    }

    private void Update()
    {
        if (player == null || isCollected)
            return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance < attractRadius)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                attractSpeed * Time.deltaTime
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
            return;

        if (!other.CompareTag("Player"))
            return;

        isCollected = true;

        GameManager.Instance?.AddVinyls(value);

        if (collectSFX != null)
        {
            AudioSource.PlayClipAtPoint(
                collectSFX,
                transform.position
            );
        }

        Destroy(gameObject);
    }

    private IEnumerator RotateAndBob()
    {
        float time = 0f;

        while (!isCollected)
        {
            time += Time.deltaTime;

            transform.Rotate(
                Vector3.forward,
                rotationSpeed * Time.deltaTime
            );

            transform.position =
                startPosition +
                Vector3.up * Mathf.Sin(time * bobSpeed) * bobHeight;

            yield return null;
        }
    }
}
