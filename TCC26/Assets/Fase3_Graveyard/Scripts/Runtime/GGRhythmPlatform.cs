using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Plataforma que só existe no ritmo da música.
    /// Blink: aparece/some (avisa piscando 1 batida antes de sumir e mostra um "fantasma" 1 batida antes de aparecer).
    /// Rise: sobe do chão e desce de novo (precisa de Rigidbody2D Kinematic; o Player pega carona).
    /// </summary>
    public class GGRhythmPlatform : MonoBehaviour
    {
        public enum Mode { Blink, Rise }

        [SerializeField] Mode mode = Mode.Blink;
        [SerializeField] int periodBeats = 4;
        [SerializeField] int onBeats = 2;
        [SerializeField] float offsetBeats = 0f;
        [SerializeField] float hiddenDrop = 2.5f;
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
            float cycle = (float)GGBeatClock.Mod(GGBeatClock.Position - offsetBeats, periodBeats);
            bool on = cycle < onBeats;
            bool leaving = on && cycle >= onBeats - 1f;
            bool preview = !on && cycle >= periodBeats - 1f;

            float target = on ? 1f : 0f;
            vis = Mathf.MoveTowards(vis, target, (mode == Mode.Rise ? 2.5f : 9f) * Time.deltaTime);

            float a;
            if (mode == Mode.Blink)
            {
                if (on) a = leaving ? 0.55f + 0.45f * Mathf.Sin(Time.time * 40f) : Mathf.Clamp01(vis * 1.5f);
                else a = preview ? 0.25f : vis;
            }
            else
            {
                a = vis > 0.02f || preview ? 1f : 0f;
            }
            SetAlpha(a);

            if (solid != null)
                solid.enabled = mode == Mode.Blink ? (on && vis > 0.5f) : vis > 0.3f;
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
