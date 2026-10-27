using System;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Seções da música da Fase 2 (BandLab, 108 BPM, 4/4).</summary>
    public enum HiveMusicSection
    {
        Intro = 0,    // compassos 1-8   (toca 1 vez, no começo)
        Loop = 1,     // compassos 9-24  (loop principal)
        Combat = 2,   // compassos 25-40 (loop de combate)
        Boss = 3      // compassos 41-72 (loop do boss)
    }

    /// <summary>
    /// Toca a música da fase em 4 seções e troca entre elas SEMPRE no início de um compasso,
    /// agendando com AudioSettings.dspTime (sem atraso e sem estalo). O relógio de batidas
    /// (HiveBeatDriver) continua contínuo: todas as seções têm número inteiro de compassos de 4 batidas,
    /// então plataformas, inimigos e boss ficam sincronizados antes, durante e depois de cada troca.
    ///
    /// Quem pede a troca:
    ///   - marcador MusicCue (Player entra na zona)  → Loop ou Combat
    ///   - HiveQueenArena (luta começa)               → Boss
    ///   - HiveLevelBootstrap (morte/checkpoint/boss derrotado/fim da fase)
    ///
    /// Clipes: se os campos ficarem vazios, carrega de Assets/Resources/Hive/
    /// (hive_intro, hive_loop, hive_combate, hive_boss). Import settings recomendados: ver README.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class HiveMusic : MonoBehaviour
    {
        private const string ResPath = "Hive/";

        public static HiveMusic Instance { get; private set; }

        [Header("Clipes (vazio = carrega de Assets/Resources/Hive)")]
        public AudioClip intro;     // hive_intro.wav   — compassos 1-8   — toca 1x
        public AudioClip loop;      // hive_loop.wav    — compassos 9-24  — loop
        public AudioClip combat;    // hive_combate.wav — compassos 25-40 — loop
        public AudioClip boss;      // hive_boss.wav    — compassos 41-72 — loop

        [Header("Mix")]
        [Range(0f, 1f)] public float volume = 0.85f;
        [Tooltip("Toca o INTRO (8 compassos) uma vez no começo da fase. Desmarque para começar direto no LOOP.")]
        public bool playIntro = true;
        [Tooltip("Antecedência mínima (s) para agendar uma troca. 0.12 é seguro.")]
        public float scheduleLead = 0.12f;
        [Tooltip("Escreve no Console cada troca de seção (e em qual compasso).")]
        public bool logTransitions = true;

        private readonly AudioSource[] src = new AudioSource[2];
        private int active;
        private HiveMusicSection current = HiveMusicSection.Intro;
        private bool begun;

        private bool pendingActive;
        private HiveMusicSection pending;
        private double pendingTime;

        private bool hasQueued;
        private HiveMusicSection queued;
        private bool hasDeferred;
        private HiveMusicSection deferred;

        private double introEnd;
        private float master = 1f, masterTarget = 1f, fadeSeconds = 1f;

        public bool HasClips { get { return loop != null; } }

        /// <summary>Seção que está tocando ou que já está agendada para o próximo compasso.</summary>
        public static HiveMusicSection Target
        {
            get
            {
                var m = Instance;
                if (m == null) return HiveMusicSection.Loop;
                if (m.pendingActive) return m.pending;
                if (!m.begun || m.current == HiveMusicSection.Intro)
                    return m.hasQueued ? m.queued : HiveMusicSection.Loop;
                return m.current;
            }
        }

        // ============================================================ API estática (segura sem HiveMusic)

        public static void Request(HiveMusicSection section)
        {
            if (Instance != null) Instance.RequestInternal(section);
        }

        public static void FadeOut(float seconds)
        {
            if (Instance == null) return;
            Instance.fadeSeconds = Mathf.Max(0.05f, seconds);
            Instance.masterTarget = 0f;
        }

        /// <summary>Cria o HiveMusic sozinho se a cena não tiver um e existirem clipes em Resources/Hive.</summary>
        public static HiveMusic EnsureExists()
        {
            if (Instance != null) return Instance;
            if (Resources.Load<AudioClip>(ResPath + "hive_loop") == null) return null;
            var go = new GameObject("HiveMusic");
            return go.AddComponent<HiveMusic>();
        }

        // ============================================================ Ciclo de vida

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (intro == null) intro = Resources.Load<AudioClip>(ResPath + "hive_intro");
            if (loop == null) loop = Resources.Load<AudioClip>(ResPath + "hive_loop");
            if (combat == null) combat = Resources.Load<AudioClip>(ResPath + "hive_combate");
            if (boss == null) boss = Resources.Load<AudioClip>(ResPath + "hive_boss");

            for (int i = 0; i < src.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = false;
                s.spatialBlend = 0f;
                src[i] = s;
            }

            if (loop == null)
                Debug.LogWarning("[Hive] HiveMusic sem o clipe LOOP (hive_loop). Coloque os 4 .wav em Assets/Resources/Hive/ " +
                                 "ou arraste-os nos campos do HiveMusic.");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static bool Ready(AudioClip c)
        {
            if (c == null) return true;
            if (c.loadState == AudioDataLoadState.Unloaded) c.LoadAudioData();
            return c.loadState == AudioDataLoadState.Loaded || c.loadState == AudioDataLoadState.Failed;
        }

        private bool AllReady()
        {
            return Ready(intro) && Ready(loop) && Ready(combat) && Ready(boss);
        }

        private AudioClip ClipFor(HiveMusicSection s)
        {
            AudioClip c;
            switch (s)
            {
                case HiveMusicSection.Intro: c = intro; break;
                case HiveMusicSection.Combat: c = combat; break;
                case HiveMusicSection.Boss: c = boss; break;
                default: c = loop; break;
            }
            return c != null ? c : loop;
        }

        // ============================================================ Tempo

        /// <summary>Próximo início de compasso (no relógio do HiveBeatDriver) com pelo menos "lead" segundos de folga.</summary>
        private double NextBar(double lead)
        {
            double bz = HiveBeatDriver.BeatZeroDsp;
            double bl = HiveBeatDriver.BarLength;
            double k = Math.Ceiling((AudioSettings.dspTime + lead - bz) / bl);
            if (k < 0.0) k = 0.0;
            return bz + k * bl;
        }

        private int BarIndex(double dsp)
        {
            return (int)Math.Round((dsp - HiveBeatDriver.BeatZeroDsp) / HiveBeatDriver.BarLength) + 1;
        }

        // ============================================================ Máquina de estados

        private void Update()
        {
            master = Mathf.MoveTowards(master, masterTarget, Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeSeconds));
            float v = volume * master;
            for (int i = 0; i < src.Length; i++)
                if (src[i] != null) src[i].volume = v;

            if (!begun)
            {
                if (!HiveBeatDriver.Running || loop == null || !AllReady()) return;
                Begin();
                return;
            }

            double now = AudioSettings.dspTime;

            // fim do intro: agenda a próxima seção exatamente no compasso 9
            if (current == HiveMusicSection.Intro && !pendingActive && now >= introEnd - 0.6)
            {
                HiveMusicSection next = hasQueued ? queued : HiveMusicSection.Loop;
                hasQueued = false;
                double tb = (introEnd - now >= scheduleLead) ? introEnd : NextBar(scheduleLead);
                Schedule(next, tb);
            }

            if (pendingActive && now >= pendingTime) Complete();
        }

        private void Begin()
        {
            begun = true;

            double bz = HiveBeatDriver.BeatZeroDsp;
            double now = AudioSettings.dspTime;
            double t0 = now <= bz - 0.1 ? bz : NextBar(scheduleLead);

            bool withIntro = playIntro && intro != null;
            HiveMusicSection first = withIntro ? HiveMusicSection.Intro : (hasQueued ? queued : HiveMusicSection.Loop);
            if (!withIntro) hasQueued = false;

            AudioClip clip = ClipFor(first);
            active = 0;
            var a = src[0];
            a.clip = clip;
            a.loop = first != HiveMusicSection.Intro;
            a.volume = volume * master;
            a.PlayScheduled(t0);
            current = first;

            if (first == HiveMusicSection.Intro)
                introEnd = t0 + (double)clip.samples / clip.frequency;

            if (logTransitions)
                Debug.Log("[Hive] Música: " + Label(first) + " no compasso " + BarIndex(t0));
        }

        private void Schedule(HiveMusicSection s, double tb)
        {
            AudioClip clip = ClipFor(s);
            if (clip == null) return;

            int other = 1 - active;
            var o = src[other];
            o.Stop();                       // cancela um agendamento anterior, se houver
            o.clip = clip;
            o.loop = s != HiveMusicSection.Intro;
            o.volume = volume * master;
            o.PlayScheduled(tb);
            src[active].SetScheduledEndTime(tb);

            pending = s;
            pendingTime = tb;
            pendingActive = true;

            if (logTransitions)
                Debug.Log("[Hive] Música: " + Label(s) + " agendada para o compasso " + BarIndex(tb));
        }

        private void Complete()
        {
            src[active].Stop();
            active = 1 - active;
            current = pending;
            pendingActive = false;

            if (hasDeferred)
            {
                hasDeferred = false;
                RequestInternal(deferred);
            }
        }

        private void RequestInternal(HiveMusicSection s)
        {
            if (loop == null) return;
            if (s == HiveMusicSection.Intro) s = HiveMusicSection.Loop;   // o intro só toca no começo

            // ainda no intro (ou antes de começar): guarda e aplica quando o intro acabar
            if (!begun || (current == HiveMusicSection.Intro && !pendingActive))
            {
                queued = s;
                hasQueued = true;
                return;
            }

            double now = AudioSettings.dspTime;

            if (pendingActive)
            {
                if (s == pending) return;
                if (pendingTime - now < scheduleLead)
                {
                    deferred = s;               // tarde demais para trocar o alvo: aplica logo depois
                    hasDeferred = true;
                    return;
                }
                Schedule(s, pendingTime);       // mesmo compasso, outro destino
                return;
            }

            if (s == current) return;
            Schedule(s, NextBar(scheduleLead));
        }

        private static string Label(HiveMusicSection s)
        {
            switch (s)
            {
                case HiveMusicSection.Intro: return "INTRO";
                case HiveMusicSection.Combat: return "COMBATE";
                case HiveMusicSection.Boss: return "BOSS";
                default: return "LOOP PRINCIPAL";
            }
        }
    }
}
