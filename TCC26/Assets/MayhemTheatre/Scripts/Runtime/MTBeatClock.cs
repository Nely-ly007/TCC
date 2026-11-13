using System;
using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Relógio de batidas da Fase 4, baseado em AudioSettings.dspTime (BeatInterval = 60 / BPM).
    /// O MTMusicDirector reancora o relógio a cada troca de momento musical, sempre numa fronteira de compasso.
    /// </summary>
    public class MTBeatClock : MonoBehaviour
    {
        public static MTBeatClock Instance { get; private set; }

        /// <summary>(batida absoluta desde a âncora, batida no compasso 0..N-1, compasso)</summary>
        public event Action<int, int, int> OnBeat;
        /// <summary>Disparado no tempo 1 de cada compasso (argumento = número do compasso).</summary>
        public event Action<int> OnBar;

        public float bpm = 64f;
        public int beatsPerBar = 4;

        double anchorDsp;
        bool running;
        int lastBeat = -1;

        public bool Running { get { return running; } }
        public double SecondsPerBeat { get { return 60.0 / Math.Max(1.0, bpm); } }
        public double SongTime { get { return running ? AudioSettings.dspTime - anchorDsp : 0.0; } }
        /// <summary>Posição em batidas (fracionária). Negativa antes do início agendado.</summary>
        public double BeatPosition { get { return running ? SongTime / SecondsPerBeat : 0.0; } }
        public int CurrentBeat { get { return (int)Math.Floor(BeatPosition); } }
        public int CurrentBeatInBar { get { return MTUtil.Mod(CurrentBeat, beatsPerBar); } }
        public int CurrentBar { get { return Mathf.FloorToInt((float)Math.Floor(BeatPosition) / Mathf.Max(1, beatsPerBar)); } }
        /// <summary>0..1 dentro da batida atual.</summary>
        public float BeatPhase { get { double p = BeatPosition; return (float)(p - Math.Floor(p)); } }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Reancora o relógio. startDsp pode estar no futuro (música agendada com PlayScheduled).</summary>
        public void Anchor(double startDsp, float newBpm, int newBeatsPerBar)
        {
            anchorDsp = startDsp;
            bpm = Mathf.Max(20f, newBpm);
            beatsPerBar = Mathf.Max(1, newBeatsPerBar);
            lastBeat = -1;
            running = true;
        }

        public void StopClock()
        {
            running = false;
        }

        /// <summary>Tempo dsp do próximo início de compasso.</summary>
        public double NextBarDsp()
        {
            double bp = BeatPosition;
            double nextBarBeat = (Math.Floor(bp / beatsPerBar) + 1.0) * beatsPerBar;
            return anchorDsp + nextBarBeat * SecondsPerBeat;
        }

        public double DspTimeOfBeat(int beat)
        {
            return anchorDsp + beat * SecondsPerBeat;
        }

        void Update()
        {
            if (!running) return;

            int now = CurrentBeat;
            if (now < 0) return;

            // Jogo pausado (timeScale 0): não dispara batidas acumuladas
            if (Time.timeScale <= 0.0001f)
            {
                lastBeat = now;
                return;
            }

            if (now - lastBeat > 8) lastBeat = now - 1; // perdeu muitas batidas (hitch): ressincroniza

            for (int b = lastBeat + 1; b <= now; b++)
            {
                int inBar = MTUtil.Mod(b, beatsPerBar);
                int bar = b / Mathf.Max(1, beatsPerBar);
                if (inBar == 0 && OnBar != null) OnBar(bar);
                if (OnBeat != null) OnBeat(b, inBar, bar);
            }
            lastBeat = now;
        }
    }

    /// <summary>Base para qualquer objeto que reage às batidas. Inscreve/desinscreve sozinho.</summary>
    public abstract class MTBeatListener : MonoBehaviour
    {
        bool subscribed;
        bool started;

        protected virtual void Start()
        {
            started = true;
            Subscribe();
        }

        protected virtual void OnEnable()
        {
            if (started) Subscribe();
        }

        protected virtual void OnDisable()
        {
            Unsubscribe();
        }

        void Subscribe()
        {
            if (subscribed) return;
            MTBeatClock c = MTBeatClock.Instance;
            if (c == null) return;
            c.OnBeat += HandleBeat;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed) return;
            MTBeatClock c = MTBeatClock.Instance;
            if (c != null) c.OnBeat -= HandleBeat;
            subscribed = false;
        }

        void HandleBeat(int beatAbs, int beatInBar, int bar)
        {
            OnBeat(beatAbs, beatInBar, bar);
        }

        protected abstract void OnBeat(int beatAbs, int beatInBar, int bar);
    }
}
