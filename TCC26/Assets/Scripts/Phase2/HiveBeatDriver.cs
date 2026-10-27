using System;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Relógio rítmico da Fase 2 (The Hive): BPM fixo + AudioSettings.dspTime.
    /// Dispara OnBeat / OnBar. A música em 4 seções é tocada pelo HiveMusic (alinhada a este relógio);
    /// o campo "Music Clip" só é usado quando NÃO existe HiveMusic com clipes (modo faixa única).
    /// Fica no objeto [Systems].
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class HiveBeatDriver : MonoBehaviour
    {
        public static HiveBeatDriver Instance { get; private set; }

        /// <summary>(batida total desde o início da música, batida dentro do compasso 0..N-1)</summary>
        public static event Action<int, int> OnBeat;

        /// <summary>(índice do compasso) — dispara junto com a batida 1.</summary>
        public static event Action<int> OnBar;

        [Header("Ritmo")]
        [SerializeField] private float bpm = 108f;
        [SerializeField] private int beatsPerBar = 4;
        [SerializeField, Tooltip("Segundos de contagem antes da 1ª batida.")]
        private float startDelay = 1.2f;

        [Header("Música")]
        [SerializeField] private bool playMusic = true;
        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.85f;
        [SerializeField] private bool loop = true;
        [SerializeField, Tooltip("Silêncio no começo do .wav antes da 1ª batida (segundos).")]
        private float firstBeatOffsetInClip = 0f;

        private double dspStart;
        private int lastBeat = -1;
        private bool started;
        private AudioSource source;

        public bool HasMusic { get { return musicClip != null; } }

        public static bool Running => Instance != null && Instance.started;
        public static float Bpm => Instance != null ? Instance.bpm : 108f;
        public static double BeatInterval => 60.0 / Bpm;
        public static int BeatsPerBar => Instance != null ? Mathf.Max(1, Instance.beatsPerBar) : 4;
        public static int CurrentBeat => Instance != null ? Mathf.Max(0, Instance.lastBeat) : 0;

        /// <summary>Duração de um compasso em segundos (4 batidas a 108 BPM = 2,2222 s).</summary>
        public static double BarLength => BeatInterval * BeatsPerBar;

        /// <summary>Tempo do DSP (AudioSettings.dspTime) em que cai a batida 0 — início da música.</summary>
        public static double BeatZeroDsp => Instance != null ? Instance.dspStart : 0.0;

        /// <summary>Posição contínua na música, em batidas (0 = 1ª batida; negativo = contagem).</summary>
        public static double SongBeat
        {
            get
            {
                var d = Instance;
                if (d == null || !d.started) return 0.0;
                return (AudioSettings.dspTime - d.dspStart) / (60.0 / d.bpm);
            }
        }

        /// <summary>Progresso 0..1 dentro da batida atual.</summary>
        public static float BeatProgress
        {
            get
            {
                double s = SongBeat;
                return (float)(s - Math.Floor(s));
            }
        }

        /// <summary>Pulso que vai de 1 para 0 a cada batida (use para escala/brilho).</summary>
        public static float Pulse => Running ? Mathf.Pow(1f - BeatProgress, 3f) : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (Instance != this) return;

            float minDelay = firstBeatOffsetInClip + 0.25f;
            if (startDelay < minDelay) startDelay = minDelay;
            dspStart = AudioSettings.dspTime + startDelay;

            bool externalMusic = HiveMusic.Instance != null && HiveMusic.Instance.HasClips;

            if (externalMusic)
            {
                // o HiveMusic toca as 4 seções, agendadas a partir do tempo 0 deste relógio
            }
            else if (playMusic && musicClip != null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.clip = musicClip;
                source.loop = loop;
                source.volume = musicVolume;
                source.spatialBlend = 0f;
                source.playOnAwake = false;
                source.PlayScheduled(dspStart - firstBeatOffsetInClip);

                double barLen = 60.0 / bpm * Mathf.Max(1, beatsPerBar);
                double bars = (musicClip.length - firstBeatOffsetInClip) / barLen;
                if (Math.Abs(bars - Math.Round(bars)) > 0.03)
                {
                    Debug.LogWarning("[Hive] O clip tem " + bars.ToString("0.00") +
                        " compassos. Corte o .wav em um número inteiro de compassos para o loop não desalinhar.");
                }
            }
            else if (playMusic)
            {
                Debug.LogWarning("[Hive] HiveBeatDriver sem Music Clip: o relógio roda, mas sem música.");
            }

            started = true;
        }

        private void Update()
        {
            if (!started) return;

            double interval = 60.0 / bpm;
            long idx = (long)Math.Floor((AudioSettings.dspTime - dspStart) / interval);
            if (idx < 0) return; // contagem inicial

            if (idx - lastBeat > 8) lastBeat = (int)idx - 1; // perdeu foco: não dispara rajada

            int bpb = Mathf.Max(1, beatsPerBar);
            while (lastBeat < idx)
            {
                lastBeat++;
                int inBar = lastBeat % bpb;
                RaiseBeat(lastBeat, inBar);
                if (inBar == 0) RaiseBar(lastBeat / bpb);
            }
        }

        /// <summary>Alinha o relógio a uma música tocada por outro sistema (ex.: MusicSectionManager).</summary>
        public void SyncTo(double dspTimeOfBeatZero)
        {
            dspStart = dspTimeOfBeatZero;
            lastBeat = -1;
            started = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static void RaiseBeat(int beat, int inBar)
        {
            var ev = OnBeat;
            if (ev == null) return;
            foreach (Delegate d in ev.GetInvocationList())
            {
                try { ((Action<int, int>)d)(beat, inBar); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private static void RaiseBar(int bar)
        {
            var ev = OnBar;
            if (ev == null) return;
            foreach (Delegate d in ev.GetInvocationList())
            {
                try { ((Action<int>)d)(bar); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
