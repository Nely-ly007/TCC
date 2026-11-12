using System;
using System.Collections;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Relógio rítmico da Fase 3 (114 BPM, 4/4). BeatInterval = 60 / BPM, baseado em AudioSettings.dspTime.
    /// Tudo na fase (plataformas, hazards, inimigos, boss) lê GGBeatClock.Position (batidas contínuas).
    /// Se não houver relógio na cena, usa um relógio de fallback (114 BPM) para testar prefabs isolados.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GGBeatClock : MonoBehaviour
    {
        public const float DefaultBpm = 114f;

        public static GGBeatClock Instance { get; private set; }

        /// <summary>Disparado a cada batida (índice absoluto, começa em 0).</summary>
        public static event Action<int> OnBeat;
        /// <summary>Disparado no primeiro tempo de cada compasso (índice do compasso).</summary>
        public static event Action<int> OnBar;

        [Header("Ritmo")]
        [SerializeField] float bpm = 114f;
        [SerializeField] int beatsPerBar = 4;
        [Tooltip("Atraso (s) entre iniciar o relógio e a batida 0 / início da música.")]
        [SerializeField] float startDelay = 0.5f;
        [SerializeField] bool autoStart = true;

        [Header("Música (opcional)")]
        [SerializeField] AudioClip levelMusic;
        [SerializeField] AudioClip bossMusic;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.8f;

        AudioSource sourceLevel;
        AudioSource sourceBoss;
        double songStartDsp;
        int lastBeat = -1;
        bool running;
        bool fadingOut;
        float fadeSeconds = 1f;

        public static float Bpm { get { return Instance != null ? Instance.bpm : DefaultBpm; } }
        public static float BeatInterval { get { return 60f / Bpm; } }
        public static int BeatsPerBar { get { return Instance != null ? Mathf.Max(1, Instance.beatsPerBar) : 4; } }
        public static bool IsRunning { get { return Instance != null && Instance.running; } }

        /// <summary>Posição contínua em batidas desde o início da música (negativa antes do início).</summary>
        public static double Position
        {
            get
            {
                if (Instance != null && Instance.running)
                    return (AudioSettings.dspTime - Instance.songStartDsp) * Instance.bpm / 60.0;
                return Time.timeAsDouble * Bpm / 60.0;
            }
        }

        /// <summary>Fração da batida atual (0 = no beat, vai até 1).</summary>
        public static float Phase
        {
            get
            {
                double p = Position;
                return (float)(p - Math.Floor(p));
            }
        }

        public static double Mod(double x, double m)
        {
            return x - Math.Floor(x / m) * m;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            OnBeat = null;
            OnBar = null;
            Instance = null;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            sourceLevel = CreateSource("Music_Level");
            sourceBoss = CreateSource("Music_Boss");
        }

        void Start()
        {
            if (autoStart) StartClock();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource CreateSource(string childName)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.volume = musicVolume;
            return s;
        }

        public void StartClock()
        {
            songStartDsp = AudioSettings.dspTime + startDelay;
            lastBeat = -1;
            running = true;
            fadingOut = false;
            sourceLevel.volume = musicVolume;
            sourceBoss.volume = musicVolume;
            if (levelMusic != null)
            {
                sourceLevel.clip = levelMusic;
                sourceLevel.PlayScheduled(songStartDsp);
            }
        }

        double NextBarDsp()
        {
            double bar = Math.Max(1, beatsPerBar) * 60.0 / bpm;
            double t = AudioSettings.dspTime - songStartDsp;
            return songStartDsp + (Math.Floor(t / bar) + 1.0) * bar;
        }

        /// <summary>Troca para a música do boss no início do próximo compasso (mantém a grade de batidas).</summary>
        public void SwitchToBossMusic()
        {
            if (!running) return;
            double next = NextBarDsp();
            if (levelMusic != null) sourceLevel.SetScheduledEndTime(next);
            if (bossMusic != null)
            {
                sourceBoss.Stop();
                sourceBoss.clip = bossMusic;
                sourceBoss.volume = musicVolume;
                sourceBoss.PlayScheduled(next);
            }
        }

        /// <summary>Volta para a música da fase no próximo compasso (usado quando o Player morre no boss).</summary>
        public void SwitchToLevelMusic()
        {
            if (!running) return;
            double next = NextBarDsp();
            if (bossMusic != null) sourceBoss.SetScheduledEndTime(next);
            if (levelMusic != null)
            {
                sourceLevel.Stop();
                sourceLevel.clip = levelMusic;
                sourceLevel.volume = musicVolume;
                sourceLevel.PlayScheduled(next);
            }
        }

        public void FadeOutMusic(float seconds)
        {
            fadeSeconds = Mathf.Max(0.05f, seconds);
            fadingOut = true;
        }

        void Update()
        {
            if (running)
            {
                double t = AudioSettings.dspTime - songStartDsp;
                if (t >= 0.0)
                {
                    int idx = (int)Math.Floor(t * bpm / 60.0);
                    int bpb = Mathf.Max(1, beatsPerBar);
                    while (lastBeat < idx)
                    {
                        lastBeat++;
                        if (OnBeat != null) OnBeat(lastBeat);
                        if (lastBeat % bpb == 0 && OnBar != null) OnBar(lastBeat / bpb);
                    }
                }
            }

            if (fadingOut)
            {
                float step = musicVolume / fadeSeconds * Time.unscaledDeltaTime;
                sourceLevel.volume = Mathf.Max(0f, sourceLevel.volume - step);
                sourceBoss.volume = Mathf.Max(0f, sourceBoss.volume - step);
                if (sourceLevel.volume <= 0f && sourceBoss.volume <= 0f)
                {
                    sourceLevel.Stop();
                    sourceBoss.Stop();
                    fadingOut = false;
                }
            }
        }
    }

    /// <summary>Ajudantes de espera em batidas para corrotinas.</summary>
    public static class GGBeat
    {
        public static double NextBeat()
        {
            return Math.Floor(GGBeatClock.Position) + 1.0;
        }

        public static double NextBar()
        {
            double bpb = GGBeatClock.BeatsPerBar;
            return (Math.Floor(GGBeatClock.Position / bpb) + 1.0) * bpb;
        }

        public static IEnumerator WaitUntil(double targetBeat)
        {
            while (GGBeatClock.Position < targetBeat) yield return null;
        }

        public static IEnumerator Wait(double beats)
        {
            double target = GGBeatClock.Position + beats;
            while (GGBeatClock.Position < target) yield return null;
        }

        public static IEnumerator WaitNextBeat()
        {
            double target = NextBeat();
            while (GGBeatClock.Position < target) yield return null;
        }

        public static IEnumerator WaitNextBar()
        {
            double target = NextBar();
            while (GGBeatClock.Position < target) yield return null;
        }
    }
}
