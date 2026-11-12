using System.Collections;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Arena do boss. O BoxCollider2D (trigger) desta raiz cobre a arena inteira.
    /// Quando o Player entra: fecha os portões, trava a câmera na arena, troca a música (no próximo compasso)
    /// e inicia o boss. Se o Player morre, tudo reinicia. Ao derrotar o boss, abre tudo.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class GGBossArena : MonoBehaviour
    {
        [SerializeField] GGBoss boss;
        [SerializeField] GGGate[] gates;
        [SerializeField] float startDelay = 0.8f;

        BoxCollider2D area;
        bool started;
        bool finished;

        public bool FightActive { get { return started && !finished; } }

        void Awake()
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
        }

        void OnEnable()
        {
            if (boss != null) boss.Defeated += OnBossDefeated;
        }

        void OnDisable()
        {
            if (boss != null) boss.Defeated -= OnBossDefeated;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (started || finished || boss == null) return;
            if (other.GetComponentInParent<GGPlayerHealth>() == null) return;
            StartCoroutine(BeginRoutine());
        }

        Rect ArenaRect()
        {
            Bounds b = area.bounds;
            return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        }

        IEnumerator BeginRoutine()
        {
            started = true;
            SetGates(true);
            GGCameraFollow.LockBounds(ArenaRect());
            if (GGBeatClock.Instance != null) GGBeatClock.Instance.SwitchToBossMusic();
            yield return new WaitForSeconds(startDelay);
            boss.BeginFight();
        }

        void SetGates(bool closed)
        {
            if (gates == null) return;
            for (int i = 0; i < gates.Length; i++)
                if (gates[i] != null) gates[i].SetClosed(closed);
        }

        /// <summary>Chamado pelo GGLevelManager quando o Player morre e volta ao checkpoint.</summary>
        public void ResetFight()
        {
            if (!started || finished) return;
            StopAllCoroutines();
            started = false;
            SetGates(false);
            GGCameraFollow.UnlockBounds();
            if (boss != null) boss.ResetFight();
            if (GGBeatClock.Instance != null) GGBeatClock.Instance.SwitchToLevelMusic();
        }

        void OnBossDefeated()
        {
            finished = true;
            SetGates(false);
            GGCameraFollow.UnlockBounds();
            if (GGBeatClock.Instance != null) GGBeatClock.Instance.FadeOutMusic(5f);
        }

        void OnDrawGizmosSelected()
        {
            BoxCollider2D c = GetComponent<BoxCollider2D>();
            if (c == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.6f, 0.9f);
            Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
        }
    }
}
