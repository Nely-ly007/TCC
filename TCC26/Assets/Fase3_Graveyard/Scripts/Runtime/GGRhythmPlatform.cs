
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Plataforma rítmica.
    /// Blink: aparece e desaparece com aviso antecipado.
    /// Rise: sobe e desce suavemente acompanhando o ritmo.
    /// </summary>
    public class GGRhythmPlatform : MonoBehaviour
    {
        public enum Mode { Blink, Rise }

        [SerializeField] Mode mode = Mode.Blink;

        [Header("Ritmo")]
        [SerializeField] int periodBeats = 6;
        [SerializeField] int onBeats = 4;
        [SerializeField] float offsetBeats = 0f;

        [Header("Movimento")]
        [SerializeField] float hiddenDrop = 2.5f;

        [Header("Velocidade")]
        [SerializeField] float blinkSpeed = 3f;
        [SerializeField] float riseSpeed = 1.2f;

        [Header("Referências")]
        [SerializeField] SpriteRenderer[] renderers;
        [SerializeField] Collider2D solid;

        Rigidbody2D rb;
        GGRiderTracker tracker;

        Vector3 home;
        float vis;
        float alpha = -1f;

        public void Configure(
            Mode newMode,
            int newPeriod,
            int newOn,
            float newOffset,
            float newDrop)
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

            if (solid == null)
                solid = GetComponent<Collider2D>();

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SpriteRenderer>();

            vis = 0f;
        }

        void Update()
        {
            float cycle = (float)GGBeatClock.Mod(
                GGBeatClock.Position - offsetBeats,
                periodBeats
            );

            bool on = cycle < onBeats;

            // Aviso uma batida antes de desaparecer.
            bool leaving = on && cycle >= onBeats - 1f;

            // Fantasma uma batida antes de aparecer.
            bool preview = !on && cycle >= periodBeats - 1f;

            float target = on ? 1f : 0f;

            float speed = mode == Mode.Rise
                ? riseSpeed
                : blinkSpeed;

            vis = Mathf.MoveTowards(
                vis,
                target,
                speed * Time.deltaTime
            );

            float a;

            if (mode == Mode.Blink)
            {
                if (on)
                {
                    a = leaving
                        ? 0.55f + 0.45f * Mathf.Sin(Time.time * 12f)
                        : Mathf.Clamp01(vis * 1.5f);
                }
                else
                {
                    a = preview ? 0.25f : vis;
                }
            }
            else
            {
                a = vis > 0.02f || preview ? 1f : 0f;
            }

            SetAlpha(a);

            if (solid != null)
            {
                solid.enabled = mode == Mode.Blink
                    ? on && vis > 0.5f
                    : vis > 0.3f;
            }
        }

        void FixedUpdate()
        {
            if (mode != Mode.Rise)
                return;

            Vector2 target = new Vector2(
                home.x,
                home.y - hiddenDrop *
                (1f - Mathf.SmoothStep(0f, 1f, vis))
            );

            Vector2 current = rb != null
                ? rb.position
                : (Vector2)transform.position;

            Vector2 delta = target - current;

            if (rb != null)
                rb.MovePosition(target);
            else
                transform.position = new Vector3(
                    target.x,
                    target.y,
                    transform.position.z
                );

            if (tracker != null)
                tracker.Carry(delta);
        }

        void SetAlpha(float a)
        {
            if (Mathf.Abs(a - alpha) < 0.001f)
                return;

            alpha = a;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                Color c = renderers[i].color;
                c.a = a;
                renderers[i].color = c;
            }
        }
    }
}
