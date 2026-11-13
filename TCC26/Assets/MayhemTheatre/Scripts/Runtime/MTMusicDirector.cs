using System;
using System.Collections;
using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Toca os 5 momentos musicais da Fase 4 (Fase1, Fase2, Fase3, Fase4, MixHarmonico).
    /// A troca é agendada (PlayScheduled) para o início do próximo compasso e o MTBeatClock é reancorado nesse instante.
    /// Sem AudioClip em um momento, o relógio de batidas continua rodando em silêncio com o BPM configurado.
    /// </summary>
    public class MTMusicDirector : MonoBehaviour
    {
        public static MTMusicDirector Instance { get; private set; }

        public MTMomentConfig[] moments = new MTMomentConfig[0];
        public int beatsPerBar = 4;
        public float startDelay = 0.15f;
        public bool applyBackgroundColor = true;
        public float backgroundLerpSpeed = 1.2f;

        public event Action<MTMoment> OnMomentStarted;

        public MTMoment Current { get; private set; }
        public bool HasMoment { get; private set; }

        MTMoment requested;
        bool hasRequested;
        readonly AudioSource[] sources = new AudioSource[2];
        int active;
        Color targetBg = Color.black;
        Camera cam;
        Coroutine pending;
        Coroutine fading;

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < sources.Length; i++)
            {
                GameObject child = new GameObject("MusicSource_" + i);
                child.transform.SetParent(transform, false);
                AudioSource s = child.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sources[i] = s;
            }
        }

        void Start()
        {
            cam = Camera.main;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!applyBackgroundColor || !HasMoment) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            float t = 1f - Mathf.Exp(-backgroundLerpSpeed * Time.unscaledDeltaTime);
            cam.backgroundColor = Color.Lerp(cam.backgroundColor, targetBg, t);
        }

        public MTMomentConfig Get(MTMoment m)
        {
            for (int i = 0; i < moments.Length; i++)
                if (moments[i] != null && moments[i].moment == m) return moments[i];
            return null;
        }

        public void PlayMoment(MTMoment m, bool immediate = false)
        {
            if (hasRequested && requested == m) return;

            MTMomentConfig cfg = Get(m);
            MTBeatClock clock = MTBeatClock.Instance;
            if (cfg == null || clock == null)
            {
                Debug.LogWarning("[MT] MTMusicDirector: momento " + m + " sem configuração ou MTBeatClock ausente.");
                return;
            }

            hasRequested = true;
            requested = m;

            if (fading != null) { StopCoroutine(fading); fading = null; }
            if (pending != null) StopCoroutine(pending);

            double now = AudioSettings.dspTime;
            double start;
            if (!clock.Running || immediate || !HasMoment) start = now + startDelay;
            else start = Math.Max(clock.NextBarDsp(), now + startDelay);

            AudioSource current = sources[active];
            AudioSource next = sources[1 - active];

            next.Stop();
            if (cfg.clip != null)
            {
                next.clip = cfg.clip;
                next.loop = cfg.loop;
                next.volume = cfg.volume;
                next.PlayScheduled(start);
            }
            else
            {
                next.clip = null;
            }

            if (current.clip != null) current.SetScheduledEndTime(start);

            pending = StartCoroutine(ApplyAt(m, cfg, start, clock));
        }

        IEnumerator ApplyAt(MTMoment m, MTMomentConfig cfg, double start, MTBeatClock clock)
        {
            double wait = start - AudioSettings.dspTime - 0.03;
            if (wait > 0.0) yield return new WaitForSecondsRealtime((float)wait);

            clock.Anchor(start, cfg.bpm, beatsPerBar);
            active = 1 - active;
            Current = m;
            HasMoment = true;
            targetBg = cfg.backgroundColor;
            pending = null;
            if (OnMomentStarted != null) OnMomentStarted(m);
        }

        public void FadeOut(float seconds)
        {
            if (fading != null) StopCoroutine(fading);
            fading = StartCoroutine(FadeRoutine(seconds));
        }

        IEnumerator FadeRoutine(float seconds)
        {
            float t = 0f;
            float[] start = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++) start[i] = sources[i].volume;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, seconds));
                for (int i = 0; i < sources.Length; i++) sources[i].volume = start[i] * k;
                yield return null;
            }
            for (int i = 0; i < sources.Length; i++) sources[i].Stop();
            fading = null;
        }
    }
}
