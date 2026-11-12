using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Fantasma: flutua e atravessa paredes. Persegue o Player em PULSOS no ritmo
    /// (acelera no início de cada batida). Vida 10 (1 nota), dano 15.
    /// </summary>
    public class GGGhost : GGEnemyBase
    {
        [Header("Fantasma")]
        [SerializeField] float chaseRange = 10f;
        [SerializeField] float chaseSpeed = 3.2f;
        [SerializeField] float wanderRadius = 1.2f;

        Vector3 home;
        float seed;

        public void SetAlwaysChase() { chaseRange = 999f; }

        protected override void Awake()
        {
            base.Awake();
            home = transform.position;
            seed = Random.value * 10f;
        }

        void Update()
        {
            if (dead) return;
            Vector3 p = transform.position;
            Transform pl = Player;

            if (pl != null && Vector2.Distance(pl.position, p) < chaseRange)
            {
                float pulse = Mathf.Pow(1f - GGBeatClock.Phase, 2f);
                Vector3 dir = ((pl.position + Vector3.up * 0.8f) - p).normalized;
                p += dir * (chaseSpeed * (0.25f + 1.75f * pulse) * Time.deltaTime);
            }
            else
            {
                Vector3 w = home + new Vector3(Mathf.Sin(Time.time * 0.8f + seed) * wanderRadius,
                                               Mathf.Sin(Time.time * 1.3f + seed) * 0.5f, 0f);
                p = Vector3.MoveTowards(p, w, Time.deltaTime * 1.2f);
            }
            transform.position = p;
            baseColor.a = 0.78f + 0.18f * Mathf.Sin(Time.time * 3f + seed);
        }
    }
}
