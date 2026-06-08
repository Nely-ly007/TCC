using UnityEngine;
using System.Collections;


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

