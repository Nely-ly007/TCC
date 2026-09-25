using UnityEngine;

/// <summary>
/// Seções da música da Fase 4.
///
/// Intro  = introdução da música
/// Loop   = música normal da fase
/// Combate = música durante combate
/// Boss   = música do chefe
/// </summary>
public enum MusicSection
{
    Intro,
    Loop,
    Combate,
    Boss
}

/// <summary>
/// POP ADVENTURE - MusicSectionManager
///
/// Gerencia a troca entre os blocos de música da fase.
///
/// A troca acontece sempre no início de um compasso,
/// evitando cortes no meio da música.
///
/// O áudio é controlado por duas AudioSources usando
/// AudioSettings.dspTime para manter a troca precisa.
///
/// O RhythmManager NÃO toca áudio.
/// Ele apenas acompanha o mesmo dspTime da música.
/// </summary>
[DisallowMultipleComponent]
public class MusicSectionManager : MonoBehaviour
{
    public static MusicSectionManager Instance { get; private set; }

    [Header("Ritmo")]
    [Tooltip("Use exatamente o mesmo BPM usado no RhythmManager.")]
    public float bpm = 110f;

    [Tooltip("Quantidade de beats por compasso. Para 4/4 use 4.")]
    public int beatsPerCompasso = 4;

    [Header("Clipes de cada seção")]
    [Tooltip("Música de introdução. Toca apenas uma vez.")]
    public AudioClip introClip;

    [Tooltip("Música normal da fase. Fica em loop.")]
    public AudioClip loopClip;

    [Tooltip("Música durante combates. Fica em loop.")]
    public AudioClip combatClip;

    [Tooltip("Música do Boss. Fica em loop.")]
    public AudioClip bossClip;

    // Duas AudioSources permitem preparar a próxima música
    // antes da troca acontecer.
    private AudioSource sourceA;
    private AudioSource sourceB;

    // Source atualmente tocando
    private AudioSource activeSource;

    // Source que está livre para receber a próxima música
    private AudioSource idleSource;

    // Duração de um compasso
    private double compassoLength;

    // Momento DSP em que a seção atual começou
    private double lastSectionStartDsp;

    // Momento DSP em que a próxima troca acontecerá
    private double nextSwitchDspTime;

    // Seção atualmente tocando
    private MusicSection currentSection = MusicSection.Intro;

    // Seção que será tocada na próxima troca
    private MusicSection? pendingSection = null;

    // =========================================================
    // UNITY - AWAKE
    // =========================================================

    void Awake()
    {
        // Impede que existam dois MusicSectionManagers.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Cria as duas AudioSources.
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();

        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;

        // Define qual source começa ativa.
        activeSource = sourceA;
        idleSource = sourceB;

        // Calcula a duração de um compasso.
        compassoLength =
            (60.0 / bpm) * beatsPerCompasso;
    }

    // =========================================================
    // UNITY - START
    // =========================================================

    void Start()
    {
        // Começa a Intro imediatamente.
        PlayImmediate(
            introClip,
            loopThisClip: false
        );

        currentSection = MusicSection.Intro;

        // IMPORTANTE:
        // A Intro começou em lastSectionStartDsp.
        // O RhythmManager começa a contar usando exatamente
        // o mesmo momento.
        RhythmManager.Instance?.StartBeatOnly(
            bpm,
            lastSectionStartDsp
        );

        // Descobre a duração da Intro.
        double introDuration =
            (introClip != null)
                ? introClip.length
                : compassoLength;

        // Agenda automaticamente o Loop.
        //
        // A troca acontecerá quando a Intro terminar.
        ScheduleTransition(
            MusicSection.Loop,
            AudioSettings.dspTime + introDuration
        );
    }

    // =========================================================
    // UNITY - UPDATE
    // =========================================================

    void Update()
    {
        // Não existe troca pendente.
        if (!pendingSection.HasValue)
            return;

        // Chegamos perto do momento da troca.
        //
        // A margem de 0.05 segundos permite que o Update
        // prepare a troca antes do momento exato.
        if (AudioSettings.dspTime >= nextSwitchDspTime - 0.05)
        {
            SwitchToSection(
                pendingSection.Value
            );

            pendingSection = null;
        }
    }

    // =========================================================
    // REQUEST SECTION
    // =========================================================

    /// <summary>
    /// Solicita a troca para outra seção.
    ///
    /// A troca NÃO acontece imediatamente.
    /// Ela acontece no início do próximo compasso.
    ///
    /// Exemplos:
    ///
    /// MusicSectionManager.Instance.RequestSection(
    ///     MusicSection.Combate
    /// );
    ///
    /// MusicSectionManager.Instance.RequestSection(
    ///     MusicSection.Boss
    /// );
    /// </summary>
    public void RequestSection(MusicSection section)
    {
        // Se já estamos nessa seção e não existe outra
        // troca pendente, não precisamos fazer nada.
        if (section == currentSection &&
            !pendingSection.HasValue)
        {
            return;
        }

        double now = AudioSettings.dspTime;

        // Descobre quantos compassos já passaram
        // desde o início da seção atual.
        double compassosDecorridos =
            System.Math.Ceiling(
                (now - lastSectionStartDsp)
                / compassoLength
            );

        // Garante pelo menos um compasso de distância.
        if (compassosDecorridos < 1)
        {
            compassosDecorridos = 1;
        }

        // Calcula o início do próximo compasso.
        double proximoCompasso =
            lastSectionStartDsp +
            compassosDecorridos * compassoLength;

        // Agenda a troca.
        ScheduleTransition(
            section,
            proximoCompasso
        );
    }

    // =========================================================
    // SCHEDULE TRANSITION
    // =========================================================

    private void ScheduleTransition(
        MusicSection section,
        double dspTime)
    {
        pendingSection = section;
        nextSwitchDspTime = dspTime;
    }

    // =========================================================
    // SWITCH TO SECTION
    // =========================================================

    private void SwitchToSection(
        MusicSection section)
    {
        // Descobre qual AudioClip corresponde à seção.
        AudioClip clip = GetClip(section);

        // Intro não repete.
        // Loop, Combate e Boss repetem.
        bool shouldLoop =
            section != MusicSection.Intro;

        // Configura a AudioSource que estava livre.
        idleSource.clip = clip;
        idleSource.loop = shouldLoop;

        // =====================================================
        // AQUI ESTÁ A PARTE MAIS IMPORTANTE
        // =====================================================

        // A nova música começa EXATAMENTE no
        // nextSwitchDspTime.
        idleSource.PlayScheduled(
            nextSwitchDspTime
        );

        // A música anterior termina exatamente no mesmo
        // momento em que a nova começa.
        activeSource.SetScheduledEndTime(
            nextSwitchDspTime
        );

        // =====================================================
        // TROCA AS SOURCES
        // =====================================================

        AudioSource temp = activeSource;

        activeSource = idleSource;
        idleSource = temp;

        // Atualiza a seção atual.
        currentSection = section;

        // Salva o momento exato em que a nova seção começou.
        lastSectionStartDsp = nextSwitchDspTime;

        // =====================================================
        // SINCRONIZA O RHYTHM MANAGER
        // =====================================================

        // O RhythmManager NÃO toca a música.
        //
        // Ele apenas começa sua contagem exatamente no
        // mesmo momento em que o novo AudioClip começa.
        RhythmManager.Instance?.StartBeatOnly(
            bpm,
            nextSwitchDspTime
        );
    }

    // =========================================================
    // PLAY IMMEDIATE
    // =========================================================

    private void PlayImmediate(
        AudioClip clip,
        bool loopThisClip)
    {
        // Configura a AudioSource ativa.
        activeSource.clip = clip;
        activeSource.loop = loopThisClip;

        // Começa imediatamente.
        activeSource.Play();

        // Guarda o momento exato em que começou.
        lastSectionStartDsp =
            AudioSettings.dspTime;
    }

    // =========================================================
    // GET CLIP
    // =========================================================

    private AudioClip GetClip(
        MusicSection section)
    {
        switch (section)
        {
            case MusicSection.Intro:
                return introClip;

            case MusicSection.Loop:
                return loopClip;

            case MusicSection.Combate:
                return combatClip;

            case MusicSection.Boss:
                return bossClip;

            default:
                return null;
        }
    }
}