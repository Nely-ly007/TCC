using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Trigger de entrada da arena: tranca a porta (paredes), toca o Mix Harmônico e inicia a luta.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class MTBossArena : MonoBehaviour
    {
        public MTBossController boss;
        [Tooltip("Paredes invisíveis ativadas durante a luta (a de trás do Player).")]
        public Collider2D[] lockWalls = new Collider2D[0];
        public UnityEvent onFightStarted = new UnityEvent();

        bool started;

        void Reset()
        {
            BoxCollider2D b = GetComponent<BoxCollider2D>();
            if (b != null) b.isTrigger = true;
        }

        void Start()
        {
            SetWalls(false);
            if (boss != null) boss.onDefeated.AddListener(OnBossDefeated);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (started || !MTUtil.IsPlayer(other)) return;
            Begin();
        }

        public void Begin()
        {
            if (started) return;
            started = true;
            SetWalls(true);
            if (boss != null) boss.BeginFight();
            onFightStarted.Invoke();
        }

        /// <summary>Chamado pelo MTLevelController quando o Player renasce.</summary>
        public void ResetArena()
        {
            if (!started) return;
            started = false;
            SetWalls(false);
            if (boss != null) boss.ResetFight();
        }

        void OnBossDefeated()
        {
            SetWalls(false);
        }

        void SetWalls(bool on)
        {
            for (int i = 0; i < lockWalls.Length; i++)
                if (lockWalls[i] != null) lockWalls[i].enabled = on;
        }
    }
}
