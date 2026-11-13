using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Pulsa (escala e/ou cor) a cada batida. Para banners, lustres, luzes, props do cenário.</summary>
    public class MTBeatPulse : MTBeatListener
    {
        public float scalePunch = 0.06f;
        public float decay = 7f;
        public int everyNBeats = 1;
        public bool downbeatOnly = false;
        public bool flashColor = false;
        public Color flash = new Color(1f, 0.85f, 0.5f, 1f);

        Vector3 baseScale;
        SpriteRenderer[] renderers;
        Color[] baseColors;
        float punch;

        void Awake()
        {
            baseScale = transform.localScale;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        }

        protected override void OnBeat(int beatAbs, int beatInBar, int bar)
        {
            if (downbeatOnly && beatInBar != 0) return;
            if (everyNBeats > 1 && beatAbs % everyNBeats != 0) return;
            punch = 1f;
        }

        void Update()
        {
            punch = Mathf.MoveTowards(punch, 0f, decay * Time.deltaTime);
            transform.localScale = baseScale * (1f + scalePunch * punch);

            if (!flashColor) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = Color.Lerp(baseColors[i], flash, punch * 0.6f);
                c.a = renderers[i].color.a;
                renderers[i].color = c;
            }
        }
    }
}
