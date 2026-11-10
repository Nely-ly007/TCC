using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - CollectibleDiscFragment
/// Ao ser coletado, registra o fragmento,
/// salva o jogo e abre a saída da fase.
/// </summary>
public class CollectibleDiscFragment : MonoBehaviour
{
    [Header("Fragment")]
    [Tooltip("Informe o número da fase correspondente (Ex: 1 para Fase 1, 2 para Fase 2)")]
    [SerializeField] private int phaseIndex = 1;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSFX;

    [Header("Effect")]
    [SerializeField] private ParticleSystem collectEffect;

    [Header("Animation")]
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.05f;

    private bool isCollected;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        StartCoroutine(GoldenEffect());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
            return;

        if (!other.CompareTag("Player"))
            return;

        StartCoroutine(Collect());
    }

    private IEnumerator Collect()
    {
        isCollected = true;

        // Efeito visual
        if (collectEffect != null)
        {
            ParticleSystem fx = Instantiate(
                collectEffect,
                transform.position,
                Quaternion.identity
            );

            Destroy(fx.gameObject, 3f);
        }

        // Som
        if (collectSFX != null)
        {
            AudioSource.PlayClipAtPoint(
                collectSFX,
                transform.position
            );
        }

        // Pequeno efeito de câmera lenta
        Time.timeScale = 0.3f;

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1f;

        Debug.Log($"[CollectibleDiscFragment] Coletado fragmento da Fase {phaseIndex}!");

        // Registra o fragmento e desbloqueia a próxima fase no GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectFragment(phaseIndex);
            GameManager.Instance.SaveGame();
        }

        // Abre a porta de saída
        FindFirstObjectByType<PhaseExit>()?.Open();

        Destroy(gameObject);
    }

    private IEnumerator GoldenEffect()
    {
        float time = 0f;

        while (!isCollected)
        {
            time += Time.deltaTime;

            // Rotação
            transform.Rotate(
                Vector3.forward,
                rotationSpeed * Time.deltaTime
            );

            // Pulsação
            float scale =
                1f + Mathf.Sin(time * pulseSpeed) * pulseAmount;

            transform.localScale =
                originalScale * scale;

            // Brilho
            if (spriteRenderer != null)
            {
                float brightness =
                    1f + Mathf.Sin(time * 6f) * 0.2f;

                spriteRenderer.color = new Color(
                    brightness,
                    brightness * 0.85f,
                    0f
                );
            }

            yield return null;
        }
    }
}