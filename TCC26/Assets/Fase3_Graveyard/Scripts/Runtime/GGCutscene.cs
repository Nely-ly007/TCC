using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Cutscene por PASSOS, montada no Inspector. Coloque em um GameObject vazio (ex.: "Cutscene_BossIntro").
    /// Cada passo espera X (segundos ou batidas) e dispara um UnityEvent: ative objetos, toque Animator.SetTrigger,
    /// AudioSource.Play, PlayableDirector.Play, abra o seu diálogo em UI Toolkit, GGCutscene.ShakeCamera etc.
    /// Quem chama: GGBossArena (intro/derrota), qualquer UnityEvent (ex.: GGLevelManager.onFragmentCollected -> Play),
    /// ou "Play On Start".
    /// </summary>
    public class GGCutscene : MonoBehaviour
    {
        public enum TimeUnit { Seconds, Beats }

        [Serializable]
        public class Step
        {
            public string label = "Passo";
            [Tooltip("Espera ANTES de disparar este passo (na unidade escolhida em Time Unit).")]
            public float wait = 1f;
            public UnityEvent onStep;
        }

        [Header("Execução")]
        [SerializeField] bool playOnStart;
        [Tooltip("Marcado: só toca uma vez (a menos que seja interrompida com Stop).")]
        [SerializeField] bool playOnce = true;
        [SerializeField] TimeUnit timeUnit = TimeUnit.Seconds;

        [Header("Controle do Player")]
        [Tooltip("Scripts do Player desligados durante a cutscene (ex.: seu PlayerController). Religados no fim.")]
        [SerializeField] Behaviour[] disableDuringCutscene;
        [Tooltip("Zera a velocidade horizontal do Player (tag Player) ao começar.")]
        [SerializeField] bool stopPlayerMotion = true;

        [Header("Passos")]
        [SerializeField] Step[] steps;

        [Header("Eventos")]
        public UnityEvent onStarted;
        public UnityEvent onFinished;

        public bool IsPlaying { get; private set; }
        public bool HasPlayed { get; private set; }
        public event Action Finished;

        int nextStep;
        bool[] previousEnabled;

        void Start()
        {
            if (playOnStart) Play();
        }

        /// <summary>Começa a cutscene (ignorada se já estiver tocando ou se Play Once e já tocou).</summary>
        public void Play()
        {
            if (IsPlaying || (playOnce && HasPlayed)) return;
            HasPlayed = true;
            IsPlaying = true;
            LockPlayer();
            if (onStarted != null) onStarted.Invoke();
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            int count = steps != null ? steps.Length : 0;
            for (nextStep = 0; nextStep < count; nextStep++)
            {
                Step s = steps[nextStep];
                if (s == null) continue;
                if (s.wait > 0f)
                {
                    if (timeUnit == TimeUnit.Beats) yield return StartCoroutine(GGBeat.Wait(s.wait));
                    else yield return new WaitForSeconds(s.wait);
                }
                if (s.onStep != null) s.onStep.Invoke();
            }
            Finish();
        }

        /// <summary>Pular: dispara na hora os passos que faltam (sem esperar) e termina. Ligue a um botão "Pular".</summary>
        public void Skip()
        {
            if (!IsPlaying) return;
            StopAllCoroutines();
            int count = steps != null ? steps.Length : 0;
            for (int i = nextStep; i < count; i++)
                if (steps[i] != null && steps[i].onStep != null) steps[i].onStep.Invoke();
            Finish();
        }

        /// <summary>Interromper sem disparar o resto (ex.: o Player morreu). Libera o Player e permite tocar de novo.</summary>
        public void Stop()
        {
            if (!IsPlaying) return;
            StopAllCoroutines();
            IsPlaying = false;
            HasPlayed = false;
            UnlockPlayer();
        }

        void Finish()
        {
            IsPlaying = false;
            UnlockPlayer();
            if (onFinished != null) onFinished.Invoke();
            if (Finished != null) Finished();
        }

        void LockPlayer()
        {
            if (disableDuringCutscene != null)
            {
                previousEnabled = new bool[disableDuringCutscene.Length];
                for (int i = 0; i < disableDuringCutscene.Length; i++)
                {
                    Behaviour b = disableDuringCutscene[i];
                    if (b == null) continue;
                    previousEnabled[i] = b.enabled;
                    b.enabled = false;
                }
            }
            if (stopPlayerMotion)
            {
                Transform p = GGUtil.Player;
                Rigidbody2D rb = p != null ? p.GetComponent<Rigidbody2D>() : null;
                if (rb != null) rb.SetVelocity(new Vector2(0f, rb.GetVelocity().y));
            }
        }

        void UnlockPlayer()
        {
            if (disableDuringCutscene == null || previousEnabled == null) return;
            for (int i = 0; i < disableDuringCutscene.Length && i < previousEnabled.Length; i++)
                if (disableDuringCutscene[i] != null) disableDuringCutscene[i].enabled = previousEnabled[i];
            previousEnabled = null;
        }

        // ---- atalhos para usar nos UnityEvents dos passos ----
        public void ShakeCamera(float amplitude) { GGCameraFollow.Shake(amplitude, 0.6f); }
    }
}
