using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// POP ADVENTURE - RhythmManager
/// Gerencia o sistema de BPM e dispara eventos sincronizados com a música.
/// Usa AudioSettings.dspTime para precisão máxima (sem drift de frames).
/// </summary>
public class RhythmManager : MonoBehaviour
{
    public static RhythmManager Instance { get; private set; }

    [Header("Configurações de BPM")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private AudioSource musicSource;

    [Header("Eventos de Beat")]
    public UnityEvent OnBeat;           // Dispara em todo beat
    public UnityEvent OnBeat1;          // Beat 1 do compasso (downbeat)
    public UnityEvent OnBeat3;          // Beat 3 do compasso (backbeat)
    public UnityEvent OnHalfBeat;       // Dispara em meios-beats (colcheias)

    [Header("Feedback Visual")]
    [SerializeField] private bool enableScreenPulse = true;
    [SerializeField] private float pulseIntensity = 0.05f;

    // Tempo interno
    private double nextBeatTime;
    private double nextHalfBeatTime;
    private double beatInterval;
    private double halfBeatInterval;
    private int currentBeat = 0;       // 0-3 (compasso 4/4)
    private bool isPlaying = false;

    // Propriedades públicas
    public float BPM => bpm;
    public double BeatInterval => beatInterval;
    public int CurrentBeat => currentBeat;
    public bool IsPlaying => isPlaying;

    // Evento estático para sistemas que não têm referência ao manager
    public static event System.Action OnBeatStatic;
    public static event System.Action<int> OnBeatNumberStatic; // envia número do beat (0-3)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (GetComponent<AudioSource>() == null)
            gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        SetBPM(bpm);
    }

    /// <summary>
    /// Define o BPM e recalcula os intervalos.
    /// </summary>
    public void SetBPM(float newBpm)
    {
        bpm = newBpm;
        beatInterval = 60.0 / bpm;
        halfBeatInterval = beatInterval / 2.0;
    }

    /// <summary>
    /// Inicia a música e sincroniza o sistema de beat com o dspTime.
    /// </summary>
    public void StartMusic(AudioClip clip = null, float startBpm = -1)
    {
        if (startBpm > 0) SetBPM(startBpm);

        if (clip != null) musicSource.clip = clip;
        musicSource.Play();

        // Sincroniza com dspTime para precisão
        nextBeatTime = AudioSettings.dspTime + beatInterval;
        nextHalfBeatTime = AudioSettings.dspTime + halfBeatInterval;
        currentBeat = 0;
        isPlaying = true;
    }

    public void StopMusic()
    {
        musicSource.Stop();
        isPlaying = false;
    }

    void Update()
    {
        if (!isPlaying) return;

        double currentDspTime = AudioSettings.dspTime;

        // Verifica half-beat
        if (currentDspTime >= nextHalfBeatTime)
        {
            nextHalfBeatTime += halfBeatInterval;
            OnHalfBeat?.Invoke();
        }

        // Verifica beat completo
        if (currentDspTime >= nextBeatTime)
        {
            nextBeatTime += beatInterval;
            TriggerBeat();
        }
    }

    private void TriggerBeat()
    {
        OnBeat?.Invoke();
        OnBeatStatic?.Invoke();
        OnBeatNumberStatic?.Invoke(currentBeat);

        // Dispara eventos específicos por beat do compasso
        if (currentBeat == 0) OnBeat1?.Invoke();
        if (currentBeat == 2) OnBeat3?.Invoke();

        if (enableScreenPulse)
            CameraShake.Instance?.Pulse(pulseIntensity);

        currentBeat = (currentBeat + 1) % 4;
    }

    /// <summary>
    /// Retorna quanto tempo (0-1) falta para o próximo beat.
    /// Útil para animações de antecipação.
    /// </summary>
    public float GetBeatProgress()
    {
        double timeSinceLastBeat = nextBeatTime - AudioSettings.dspTime;
        return 1f - (float)(timeSinceLastBeat / beatInterval);
    }

    /// <summary>
    /// Verifica se estamos dentro da janela de timing (para ataques rítmicos).
    /// </summary>
    public bool IsOnBeat(float toleranceSeconds = 0.1f)
    {
        double timeToBeat = nextBeatTime - AudioSettings.dspTime;
        double timeSinceBeat = AudioSettings.dspTime - (nextBeatTime - beatInterval);
        return timeToBeat < toleranceSeconds || timeSinceBeat < toleranceSeconds;
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        GUI.Label(new Rect(10, 10, 200, 20), $"BPM: {bpm} | Beat: {currentBeat + 1}/4");
        GUI.Label(new Rect(10, 30, 200, 20), $"Progress: {GetBeatProgress():F2}");
    }
#endif
}
