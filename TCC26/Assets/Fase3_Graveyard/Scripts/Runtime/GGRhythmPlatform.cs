using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Plataforma que só existe no ritmo da música (114 BPM: 1 batida = 0,53 s).
    /// Blink: aparece/some. Avisa 1 batida antes de sumir (pisca devagar, MAS continua sólida) e mostra um "fantasma"
    ///        1 batida antes de aparecer.
    /// Rise:  sobe do chão e desce de novo (precisa de Rigidbody2D Kinematic; o Player pega carona). Avisa 1 batida antes de descer.
    ///
    /// Padrão (mais lento/fácil): Period Beats 8, On Beats 6 → fica 3,2 s presente e 1 s ausente.
    /// Em cadeia, defasar o Offset Beats de 2 em 2 (Blink: 0, 2, 4, 6) ou de 3 em 3 (Rise: 0, 3).
    /// </summary>
    public class GGRhythmPlatform : MonoBehaviour
    {
        public enum Mode { Blink, Rise }

        [SerializeField] Mode mode = Mode.Blink;
        [Tooltip("Duração do ciclo em batidas (1 batida = 0,53 s a 114 BPM).")]
        [SerializeField] int periodBeats = 8;
        [Tooltip("Quantas batidas do ciclo a plataforma fica presente (a última é o aviso). Precisa ser menor que Period Beats.")]
        [SerializeField] int onBeats = 6;
        [Tooltip("Defasagem do ciclo em batidas. Em cadeia: Blink 0, 2, 4, 6 — Rise 0, 3.")]
        [SerializeField] float offsetBeats = 0f;
        [SerializeField] float hiddenDrop = 2.5f;
        [Tooltip("Blink: tempo (s) para aparecer/sumir por completo. Maior = mais suave.")]
        [SerializeField] float fadeSeconds = 0.3f;
        [Tooltip("Rise: tempo (s) para subir/descer.")]
        [SerializeField] float riseSeconds = 0.55f;
        [SerializeField] SpriteRenderer[] renderers;
        [SerializeField] Collider2D solid;

        Rigidbody2D rb;
        GGRiderTracker tracker;
        Vector3 home;
        float vis;
        float alpha = 1f;

        public void Configure(Mode newMode, int newPeriod, int newOn, float newOffset, float newDrop)
        {
            mode = newMode;
            periodBeats = Mathf.Max(2, newPeriod);
            onBeats = Mathf.Clamp(newOn, 1, periodBeats - 1);
            offsetBeats = newOffset;
            hiddenDrop = newDrop;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            tracker = GetComponent<GGRiderTracker>();
            home = transform.position;
            if (solid == null) solid = GetComponent<Collider2D>();
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        void Update()
        {
            int period = Mathf.Max(2, periodBeats);
            float on1 = Mathf.Clamp(onBeats, 1, period - 1);
            float cycle = (float)GGBeatClock.Mod(GGBeatClock.Position - offsetBeats, period);
            bool on = cycle < on1;
            bool leaving = on && cycle >= on1 - 1f;            // última batida presente = aviso
            bool preview = !on && cycle >= period - 1f;         // última batida ausente = fantasma

            float seconds = mode == Mode.Rise ? riseSeconds : fadeSeconds;
            float speed = 1f / Mathf.Max(0.05f, seconds);
            vis = Mathf.MoveTowards(vis, on ? 1f : 0f, speed * Time.deltaTime);

            float a;
            float warn = 0.72f + 0.28f * Mathf.Sin(Time.time * 16f);   // pisca ~2,5x por segundo, sem sumir
            if (mode == Mode.Blink)
            {
                if (on) a = leaving ? Mathf.Min(warn, Mathf.Clamp01(vis * 1.5f)) : Mathf.Clamp01(vis * 1.5f);
                else a = preview ? Mathf.Max(0.25f, vis) : vis;
            }
            else
            {
                a = vis > 0.02f || preview ? (leaving ? warn : 1f) : 0f;
            }
            SetAlpha(a);

            // sólida enquanto estiver presente (inclusive no aviso) e por um instante enquanto some
            if (solid != null)
                solid.enabled = mode == Mode.Blink ? (on ? vis > 0.4f : vis > 0.5f) : vis > 0.3f;
        }

        void FixedUpdate()
        {
            if (mode != Mode.Rise) return;
            Vector2 target = new Vector2(home.x, home.y - hiddenDrop * (1f - Mathf.SmoothStep(0f, 1f, vis)));
            Vector2 current = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 delta = target - current;
            if (rb != null) rb.MovePosition(target);
            else transform.position = new Vector3(target.x, target.y, transform.position.z);
            if (tracker != null) tracker.Carry(delta);
        }

        void SetAlpha(float a)
        {
            if (Mathf.Abs(a - alpha) < 0.001f) return;
            alpha = a;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = renderers[i].color;
                c.a = a;
                renderers[i].color = c;
            }
        }
    }
}
