
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// POP ADVENTURE - RhythmManager
/// Gerencia BPM, áudio e eventos sincronizados.
/// Persiste entre cenas através de DontDestroyOnLoad.
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

    private double nextBeatTime;
    private double nextHalfBeatTime;

    private double beatInterval;
    private double halfBeatInterval;

    private int currentBeat = 0;

    private bool isPlaying = false;

    public float BPM => bpm;
    public double BeatInterval => beatInterval;
    public int CurrentBeat => currentBeat;
    public bool IsPlaying => isPlaying;

    public static event System.Action OnBeatStatic;
    public static event System.Action<int> OnBeatNumberStatic;

    // ── INICIALIZAÇÃO ──────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        // Obtém ou cria o AudioSource.
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();

            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
    }

    private void Start()
    {
        SetBPM(bpm);
    }

    // ── BPM ────────────────────────────────────────────

    public void SetBPM(float newBpm)
    {
        if (newBpm <= 0)
        {
            Debug.LogError("[RhythmManager] BPM inválido.");
            return;
        }

        bpm = newBpm;

        beatInterval = 60.0 / bpm;
        halfBeatInterval = beatInterval / 2.0;
    }

    // ── INICIAR SISTEMA DE BEAT ────────────────────────

    public void StartBeatOnly(float startBpm, double startDspTime = -1)
    {
        SetBPM(startBpm);

        if (startDspTime < 0)
            startDspTime = AudioSettings.dspTime;

        nextBeatTime = startDspTime + beatInterval;
        nextHalfBeatTime = startDspTime + halfBeatInterval;

        currentBeat = 0;
        isPlaying = true;
    }

    // ── INICIAR MÚSICA ─────────────────────────────────

    public void StartMusic(AudioClip clip = null, float startBpm = -1)
    {
        if (musicSource == null)
        {
            Debug.LogError("[RhythmManager] AudioSource não encontrado.");
            return;
        }

        if (clip == null)
        {
            Debug.LogError("[RhythmManager] Nenhum AudioClip informado.");
            return;
        }

        // Interrompe qualquer música anterior.
        StopMusic();

        if (startBpm > 0)
            SetBPM(startBpm);

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();

        StartBeatOnly(bpm, AudioSettings.dspTime);

        Debug.Log("[RhythmManager] Música iniciada: " + clip.name);
    }

    // ── PARAR MÚSICA ───────────────────────────────────

    public void StopMusic()
    {
        isPlaying = false;

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        currentBeat = 0;

        nextBeatTime = 0;
        nextHalfBeatTime = 0;

        Debug.Log("[RhythmManager] Música e sistema de beats interrompidos.");
    }

    // ── ATUALIZAÇÃO DOS BEATS ──────────────────────────

    private void Update()
    {
        if (!isPlaying)
            return;

        double currentDspTime = AudioSettings.dspTime;

        // HALF BEAT
        while (currentDspTime >= nextHalfBeatTime)
        {
            nextHalfBeatTime += halfBeatInterval;

            OnHalfBeat?.Invoke();
        }

        // BEAT COMPLETO
        while (currentDspTime >= nextBeatTime)
        {
            nextBeatTime += beatInterval;

            TriggerBeat();
        }
    }

    // ── DISPARAR EVENTOS ───────────────────────────────

    private void TriggerBeat()
    {
        OnBeat?.Invoke();

        OnBeatStatic?.Invoke();
        OnBeatNumberStatic?.Invoke(currentBeat);

        if (currentBeat == 0)
            OnBeat1?.Invoke();

        if (currentBeat == 2)
            OnBeat3?.Invoke();

        currentBeat = (currentBeat + 1) % 4;
    }

    // ── PROGRESSO DO BEAT ──────────────────────────────

    public float GetBeatProgress()
    {
        if (!isPlaying || beatInterval <= 0)
            return 0f;

        double timeSinceLastBeat =
            nextBeatTime - beatInterval - AudioSettings.dspTime;

        return Mathf.Clamp01(
            1f - (float)(timeSinceLastBeat / beatInterval)
        );
    }

    public bool IsOnBeat(float toleranceSeconds = 0.1f)
    {
        if (!isPlaying)
            return false;

        double timeToBeat =
            nextBeatTime - AudioSettings.dspTime;

        double timeSinceBeat =
            AudioSettings.dspTime - (nextBeatTime - beatInterval);

        return timeToBeat >= 0 &&
               timeToBeat < toleranceSeconds ||
               timeSinceBeat >= 0 &&
               timeSinceBeat < toleranceSeconds;
    }
}
