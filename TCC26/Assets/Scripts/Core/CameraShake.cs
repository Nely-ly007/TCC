using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - CameraShake
/// Pulso e tremor da câmera sincronizados com o ritmo.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Configurações")]
    [SerializeField] private float defaultShakeDuration = 0.1f;
    [SerializeField] private float defaultShakeMagnitude = 0.1f;

    private Vector3 originalPosition;
    private float originalOrthographicSize;

    private Camera cam;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        cam = GetComponent<Camera>();

        // Guarda a posição original da câmera.
        originalPosition = transform.localPosition;

        // Guarda o tamanho original configurado no Inspector.
        if (cam != null && cam.orthographic)
        {
            originalOrthographicSize = cam.orthographicSize;
        }
    }

    /// <summary>
    /// Pulsação suave sincronizada com o beat.
    /// </summary>
    public void Pulse(float intensity = 0.05f)
    {
        StopAllCoroutines();
        StartCoroutine(DoPulse(intensity));
    }

    /// <summary>
    /// Tremor de câmera (para dano, explosões, etc).
    /// </summary>
    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (duration < 0)
            duration = defaultShakeDuration;

        if (magnitude < 0)
            magnitude = defaultShakeMagnitude;

        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    /// <summary>
    /// Faz um pequeno zoom sincronizado com o beat.
    /// </summary>
    private IEnumerator DoPulse(float intensity)
    {
        float elapsed = 0f;

        // Duração do pulso.
        float duration = 0.08f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Cria uma curva suave:
            // 0 → máximo → 0
            float offset =
                Mathf.Sin(t * Mathf.PI) * intensity;

            // Usa o tamanho ORIGINAL da câmera
            // em vez de um valor fixo como 5f.
            cam.orthographicSize =
                originalOrthographicSize + offset;

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Retorna exatamente ao tamanho original.
        cam.orthographicSize =
            originalOrthographicSize;
    }

    /// <summary>
    /// Tremor da câmera.
    /// </summary>
    private IEnumerator DoShake(
        float duration,
        float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x =
                Random.Range(-1f, 1f) * magnitude;

            float y =
                Random.Range(-1f, 1f) * magnitude;

            transform.localPosition =
                originalPosition +
                new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Retorna à posição original.
        transform.localPosition =
            originalPosition;
    }
}