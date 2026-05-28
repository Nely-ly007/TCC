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
    private Camera cam;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cam = GetComponent<Camera>();
        originalPosition = transform.localPosition;
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
        if (duration < 0) duration = defaultShakeDuration;
        if (magnitude < 0) magnitude = defaultShakeMagnitude;
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoPulse(float intensity)
    {
        float elapsed = 0f;
        float duration = 0.08f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float offset = Mathf.Sin(t * Mathf.PI) * intensity;
            cam.orthographicSize = 5f + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.orthographicSize = 5f;
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = originalPosition + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}
