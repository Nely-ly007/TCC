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

/// <summary>
/// POP ADVENTURE - CollectibleMusicNote
/// Nota musical: restaura +15 HP ao jogador.
/// </summary>
public class CollectibleMusicNote : MonoBehaviour
{
    [SerializeField] private int healAmount = 15; // GDD: +15 HP
    [SerializeField] private AudioClip collectSFX;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController.Instance?.Heal(healAmount);
        if (collectSFX != null)
            AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        // Efeito de partícula poderia ser adicionado aqui
        Destroy(gameObject);
    }

    void Update()
    {
        // Flutua para cima suavemente
        transform.position += Vector3.up * 0.5f * Time.deltaTime;
    }
}

/// <summary>
/// POP ADVENTURE - CollectibleDiscFragment
/// Fragmento do Disco Dourado: item de progressão principal.
/// Coletado após derrotar cada boss.
/// </summary>
public class CollectibleDiscFragment : MonoBehaviour
{
    [SerializeField] private int phaseIndex = 0; // 0-3 (qual fragmento)
    [SerializeField] private AudioClip collectSFX;
    [SerializeField] private ParticleSystem collectEffect;

    private bool isCollected;

    void Start()
    {
        // Rotação e brilho
        StartCoroutine(GoldenEffect());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || !other.CompareTag("Player")) return;
        StartCoroutine(Collect());
    }

    private IEnumerator Collect()
    {
        isCollected = true;

        if (collectEffect != null)
        {
            ParticleSystem fx = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(fx.gameObject, 3f);
        }

        if (collectSFX != null)
            AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        // Pequena pausa dramática
        Time.timeScale = 0.3f;
        yield return new WaitForSecondsRealtime(0.5f);
        Time.timeScale = 1f;

        GameManager.Instance?.CollectFragment(phaseIndex);
        GameManager.Instance?.SaveGame();

        Destroy(gameObject);
    }

    private IEnumerator GoldenEffect()
    {
        float t = 0;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        while (true)
        {
            t += Time.deltaTime;
            transform.Rotate(Vector3.forward, 60f * Time.deltaTime);
            // Pulsação de escala
            float scale = 1f + Mathf.Sin(t * 4f) * 0.05f;
            transform.localScale = Vector3.one * scale;

            // Brilho dourado pulsante
            if (sr != null)
            {
                float brightness = 1f + Mathf.Sin(t * 6f) * 0.2f;
                sr.color = new Color(brightness, brightness * 0.85f, 0f);
            }
            yield return null;
        }
    }
}

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
