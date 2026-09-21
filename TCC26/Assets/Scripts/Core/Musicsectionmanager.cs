using UnityEngine;

/// <summary>
/// Seções da música da Fase 4 (mix harmônico).
/// Adicione/remova valores aqui se sua estrutura de blocos mudar.
/// </summary>
public enum MusicSection { Intro, Loop, Combate, Boss }

/// <summary>
/// Gerencia a troca entre os blocos de música da fase, sempre alinhada
/// ao início de um compasso (nunca corta no meio de uma frase musical).
///
/// COMO USAR:
/// 1. Crie um GameObject vazio na cena da Fase 4 (ex: "MusicManager").
/// 2. Adicione este script a ele.
/// 3. Arraste os 4 arquivos .wav exportados do BandLab nos campos
///    Intro Clip / Loop Clip / Combat Clip / Boss Clip no Inspector.
/// 4. Ajuste o BPM para o mesmo valor usado no RhythmManager da fase.
/// 5. Chame MusicSectionManager.Instance.RequestSection(MusicSection.X)
///    de qualquer outro script (ex: um trigger, o BossController, etc).
/// </summary>
[DisallowMultipleComponent]
public class MusicSectionManager : MonoBehaviour
{
    public static MusicSectionManager Instance { get; private set; }

    [Header("Ritmo (deve bater com o RhythmManager desta fase)")]
    [Tooltip("Use o mesmo BPM definido para a Fase 4 no RhythmManager.")]
    public float bpm = 110f;
    [Tooltip("Quantos tempos (batidas) tem cada compasso. Use 4 para compasso 4/4.")]
    public int beatsPerCompasso = 4;

    [Header("Clipes de cada seção (exportados do BandLab)")]
    public AudioClip introClip;
    public AudioClip loopClip;
    public AudioClip combatClip;
    public AudioClip bossClip;

    // Duas AudioSources para permitir a troca sem gap/clique perceptível.
    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;
    private AudioSource idleSource;

    private double compassoLength;      // duração de 1 compasso, em segundos
    private double lastSectionStartDsp; // quando a seção atual começou (dspTime)
    private double nextSwitchDspTime;   // quando a próxima troca deve acontecer
    private MusicSection currentSection = MusicSection.Intro;
    private MusicSection? pendingSection = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;

        activeSource = sourceA;
        idleSource = sourceB;

        compassoLength = (60.0 / bpm) * beatsPerCompasso;
    }

    void Start()
    {
        PlayImmediate(introClip, loopThisClip: false);
        currentSection = MusicSection.Intro;

        // Agenda a troca automática pro Loop assim que a Intro terminar.
        double introDuration = (introClip != null) ? introClip.length : compassoLength;
        ScheduleTransition(MusicSection.Loop, AudioSettings.dspTime + introDuration);
    }

    void Update()
    {
        if (pendingSection.HasValue && AudioSettings.dspTime >= nextSwitchDspTime - 0.05)
        {
            SwitchToSection(pendingSection.Value);
            pendingSection = null;
        }
    }

    /// <summary>
    /// Chame este método a partir de um marcador (trigger), do BossController,
    /// do GameManager, etc. A troca real só acontece no início do PRÓXIMO
    /// compasso — nunca imediatamente — para não quebrar o ritmo da música.
    /// </summary>
    public void RequestSection(MusicSection section)
    {
        if (section == currentSection && !pendingSection.HasValue) return;

        double now = AudioSettings.dspTime;
        double compassosDecorridos = System.Math.Ceiling((now - lastSectionStartDsp) / compassoLength);
        if (compassosDecorridos < 1) compassosDecorridos = 1;
        double proximoCompasso = lastSectionStartDsp + compassosDecorridos * compassoLength;

        ScheduleTransition(section, proximoCompasso);
    }

    private void ScheduleTransition(MusicSection section, double dspTime)
    {
        pendingSection = section;
        nextSwitchDspTime = dspTime;
    }

    private void SwitchToSection(MusicSection section)
    {
        AudioClip clip = GetClip(section);
        bool shouldLoop = section != MusicSection.Intro; // Loop/Combate/Boss repetem; Intro não.

        idleSource.clip = clip;
        idleSource.loop = shouldLoop;
        idleSource.PlayScheduled(nextSwitchDspTime);

        activeSource.SetScheduledEndTime(nextSwitchDspTime);

        // Troca os papéis: quem tocava vira "ociosa", a nova vira "ativa".
        var temp = activeSource;
        activeSource = idleSource;
        idleSource = temp;

        currentSection = section;
        lastSectionStartDsp = nextSwitchDspTime;
    }

    private void PlayImmediate(AudioClip clip, bool loopThisClip)
    {
        activeSource.clip = clip;
        activeSource.loop = loopThisClip;
        activeSource.Play();
        lastSectionStartDsp = AudioSettings.dspTime;
    }

    private AudioClip GetClip(MusicSection section)
    {
        switch (section)
        {
            case MusicSection.Intro: return introClip;
            case MusicSection.Loop: return loopClip;
            case MusicSection.Combate: return combatClip;
            case MusicSection.Boss: return bossClip;
            default: return null;
        }
    }
}