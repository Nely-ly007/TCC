using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// POP ADVENTURE - RhythmManager
/// Gerencia o sistema de BPM e dispara eventos sincronizados com a música.
///
/// IMPORTANTE:
/// Este sistema NÃO precisa tocar a música.
/// O MusicSectionManager controla o áudio.
/// O RhythmManager apenas acompanha o tempo da música usando dspTime.
/// </summary>
public class RhythmManager : MonoBehaviour
{
    public static RhythmManager Instance { get; private set; }

    [Header("Configurações de BPM")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private AudioSource musicSource;

    [Header("Eventos de Beat")]
    public UnityEvent OnBeat;
    public UnityEvent OnBeat1;
    public UnityEvent OnBeat3;
    public UnityEvent OnHalfBeat;

    [Header("Feedback Visual")]
    [SerializeField] private bool enableScreenPulse = true;
    [SerializeField] private float pulseIntensity = 0.05f;

    // Tempo interno
    private double nextBeatTime;
    private double nextHalfBeatTime;
    private double beatInterval;
    private double halfBeatInterval;

    // 0 = Beat 1
    // 1 = Beat 2
    // 2 = Beat 3
    // 3 = Beat 4
    private int currentBeat = 0;

    private bool isPlaying = false;

    // Propriedades públicas
    public float BPM => bpm;
    public double BeatInterval => beatInterval;
    public int CurrentBeat => currentBeat;
    public bool IsPlaying => isPlaying;

    // Eventos estáticos
    public static event System.Action OnBeatStatic;
    public static event System.Action<int> OnBeatNumberStatic;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        // Garante que exista um AudioSource caso outro sistema
        // queira utilizar o RhythmManager para tocar áudio.
        if (GetComponent<AudioSource>() == null)
        {
            gameObject.AddComponent<AudioSource>();
        }
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
    /// Inicia o sistema de beat sem tocar nenhum áudio.
    ///
    /// startDspTime indica exatamente quando a música começou.
    /// Isso permite sincronizar o RhythmManager com músicas
    /// iniciadas através de PlayScheduled().
    /// </summary>
    public void StartBeatOnly(float startBpm, double startDspTime = -1)
    {
        SetBPM(startBpm);

        // Se nenhum tempo foi informado, começa imediatamente.
        if (startDspTime < 0)
        {
            startDspTime = AudioSettings.dspTime;
        }

        // O primeiro beat acontece um intervalo depois
        // do início da música.
        nextBeatTime = startDspTime + beatInterval;

        // O primeiro half-beat acontece metade de um beat
        // depois do início da música.
        nextHalfBeatTime = startDspTime + halfBeatInterval;

        currentBeat = 0;
        isPlaying = true;
    }

    /// <summary>
    /// Inicia a música diretamente pelo RhythmManager.
    ///
    /// Mantido para compatibilidade com outros sistemas.
    /// Para a Fase 4, o MusicSectionManager deve controlar o áudio.
    /// </summary>
    public void StartMusic(AudioClip clip = null, float startBpm = -1)
    {
        if (startBpm > 0)
        {
            SetBPM(startBpm);
        }

        if (clip != null)
        {
            musicSource.clip = clip;
        }

        musicSource.Play();

        // Como o áudio começa imediatamente,
        // usamos o dspTime atual como referência.
        StartBeatOnly(bpm, AudioSettings.dspTime);
    }

    /// <summary>
    /// Para o sistema de música e de beat.
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        isPlaying = false;
    }

    void Update()
    {
        if (!isPlaying)
            return;

        double currentDspTime = AudioSettings.dspTime;

        // =========================
        // HALF-BEAT
        // =========================

        if (currentDspTime >= nextHalfBeatTime)
        {
            // Evita acumular atraso caso um frame demore.
            while (currentDspTime >= nextHalfBeatTime)
            {
                nextHalfBeatTime += halfBeatInterval;
                OnHalfBeat?.Invoke();
            }
        }

        // =========================
        // BEAT COMPLETO
        // =========================

        if (currentDspTime >= nextBeatTime)
        {
            // Evita perder beats caso haja um frame muito demorado.
            while (currentDspTime >= nextBeatTime)
            {
                nextBeatTime += beatInterval;
                TriggerBeat();
            }
        }
    }

    /// <summary>
    /// Dispara todos os eventos relacionados ao beat.
    /// </summary>
    private void TriggerBeat()
    {
        // Evento geral
        OnBeat?.Invoke();

        // Eventos estáticos
        OnBeatStatic?.Invoke();
        OnBeatNumberStatic?.Invoke(currentBeat);

        // Beat 1 do compasso
        if (currentBeat == 0)
        {
            OnBeat1?.Invoke();
        }

        // Beat 3 do compasso
        if (currentBeat == 2)
        {
            OnBeat3?.Invoke();
        }

        // Feedback visual
        if (enableScreenPulse)
        {
            CameraShake.Instance?.Pulse(pulseIntensity);
        }

        // Próximo beat
        currentBeat = (currentBeat + 1) % 4;
    }

    /// <summary>
    /// Retorna o progresso em direção ao próximo beat.
    /// </summary>
    public float GetBeatProgress()
    {
        double timeSinceLastBeat =
            nextBeatTime - AudioSettings.dspTime;

        return 1f -
            (float)(timeSinceLastBeat / beatInterval);
    }

    /// <summary>
    /// Verifica se estamos dentro da janela de timing.
    /// </summary>
    public bool IsOnBeat(float toleranceSeconds = 0.1f)
    {
        double timeToBeat =
            nextBeatTime - AudioSettings.dspTime;

        double timeSinceBeat =
            AudioSettings.dspTime -
            (nextBeatTime - beatInterval);

        return timeToBeat < toleranceSeconds ||
               timeSinceBeat < toleranceSeconds;
    }
}