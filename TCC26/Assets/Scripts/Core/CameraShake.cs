
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// POP ADVENTURE - CameraShake
/// Pulsação sincronizada com o BPM das Fases 1, 2 e 3.
/// Não pulsa no Menu, Loading, Hub ou Fase 4.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Configurações do Shake")]
    [SerializeField] private float defaultShakeDuration = 0.1f;
    [SerializeField] private float defaultShakeMagnitude = 0.1f;

    [Header("Configurações do Pulse")]
    [SerializeField] private float pulseIntensity = 0.05f;

    [Tooltip("Percentual do intervalo entre beats utilizado pelo pulso.")]
    [SerializeField, Range(0.05f, 0.5f)]
    private float pulseDurationPercent = 0.2f;

    [SerializeField] private float minPulseDuration = 0.04f;
    [SerializeField] private float maxPulseDuration = 0.18f;

    private Vector3 originalPosition;
    private float originalOrthographicSize;

    private Camera cam;

    private Coroutine pulseCoroutine;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        cam = GetComponent<Camera>();

        originalPosition = transform.localPosition;

        if (cam != null && cam.orthographic)
        {
            originalOrthographicSize = cam.orthographicSize;
        }
    }

    /// <summary>
    /// Verifica se a cena atual permite pulsação.
    /// </summary>
    private bool IsPulseScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        return sceneName == "Fase 1" ||
               sceneName == "Fase 2" ||
               sceneName == "Fase 3";
    }

    /// <summary>
    /// Pulsação chamada pelo RhythmManager a cada beat.
    /// </summary>
    public void Pulse(float intensity = -1f)
    {
        // Não pulsa fora das fases permitidas.
        if (!IsPulseScene())
            return;

        // Verifica se o sistema de ritmo está funcionando.
        if (RhythmManager.Instance == null ||
            !RhythmManager.Instance.IsPlaying)
            return;

        if (cam == null || !cam.orthographic)
            return;

        if (intensity < 0)
            intensity = pulseIntensity;

        // Obtém o intervalo real entre os beats.
        float beatInterval = RhythmManager.Instance.BeatInterval;

        // Ajusta a duração conforme o BPM.
        float duration = Mathf.Clamp(
            beatInterval * pulseDurationPercent,
            minPulseDuration,
            maxPulseDuration
        );

        // Interrompe somente o pulso anterior.
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        pulseCoroutine = StartCoroutine(
            DoPulse(intensity, duration)
        );
    }

    /// <summary>
    /// Executa o zoom suave da câmera.
    /// </summary>
    private IEnumerator DoPulse(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            float offset = Mathf.Sin(t * Mathf.PI) * intensity;

            cam.orthographicSize =
                originalOrthographicSize + offset;

            elapsed += Time.deltaTime;

            yield return null;
        }

        cam.orthographicSize = originalOrthographicSize;

        pulseCoroutine = null;
    }

    /// <summary>
    /// Tremor independente para dano e explosões.
    /// </summary>
    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (duration < 0)
            duration = defaultShakeDuration;

        if (magnitude < 0)
            magnitude = defaultShakeMagnitude;

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(
            DoShake(duration, magnitude)
        );
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition =
                originalPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPosition;

        shakeCoroutine = null;
    }

    private void OnDisable()
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        pulseCoroutine = null;
        shakeCoroutine = null;

        if (cam != null && cam.orthographic)
            cam.orthographicSize = originalOrthographicSize;

        transform.localPosition = originalPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
