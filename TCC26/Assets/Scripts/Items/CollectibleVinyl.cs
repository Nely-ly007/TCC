using UnityEngine;
using System.Collections;


/// <summary>
/// POP ADVENTURE - CollectibleVinyl
/// Vinis B&W: moeda do jogo.
/// </summary>
public class CollectibleVinyl : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float attractRadius = 2f;
    [SerializeField] private float attractSpeed = 8f;
    [SerializeField] private AudioClip collectSFX;

    private bool isCollected;
    private Transform player;

    void Start()
    {
        player = PlayerController.Instance?.transform;
        // Rotação contínua (efeito visual de vinil)
        StartCoroutine(RotateAndBob());
    }

    void Update()
    {
        if (player == null || isCollected) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < attractRadius)
        {
            // Atrai em direção ao player (magnetismo)
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                attractSpeed * Time.deltaTime);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || !other.CompareTag("Player")) return;
        Collect();
    }

    private void Collect()
    {
        isCollected = true;
        GameManager.Instance?.AddVinyls(value);
        if (collectSFX != null)
            AudioSource.PlayClipAtPoint(collectSFX, transform.position);
        Destroy(gameObject);
    }

    private IEnumerator RotateAndBob()
    {
        float t = 0;
        Vector3 startPos = transform.position;
        while (true)
        {
            t += Time.deltaTime;
            transform.Rotate(Vector3.forward, 120f * Time.deltaTime);
            transform.position = startPos + Vector3.up * Mathf.Sin(t * 3f) * 0.1f;
            yield return null;
        }
    }
}
