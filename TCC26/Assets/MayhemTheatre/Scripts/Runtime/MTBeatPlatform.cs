using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Plataforma que aparece/desaparece no ritmo. Pattern: um caractere por batida ('1' = visível, '0' = oculta).
    /// Ex.: "1110" = 3 batidas visível, 1 oculta (ciclo de 1 compasso em 4/4). Beat Offset desloca o padrão.
    /// </summary>
    public class MTBeatPlatform : MTBeatListener
    {
        public string pattern = "1110";
        public int beatOffset = 0;
        public bool blinkWarning = true;
        public float hiddenAlpha = 0.15f;
        public float fadeSpeed = 12f;

        SpriteRenderer[] renderers;
        Collider2D[] colliders;
        bool visible = true;
        bool warnOut;
        bool warnIn;
        float alpha = 1f;

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
            colliders = GetComponentsInChildren<Collider2D>();
        }

        protected override void Start()
        {
            base.Start();
            MTBeatClock c = MTBeatClock.Instance;
            Apply(c != null && c.Running ? Mathf.Max(0, c.CurrentBeat) : 0);
            alpha = visible ? 1f : hiddenAlpha;
        }

        protected override void OnBeat(int beatAbs, int beatInBar, int bar)
        {
            Apply(beatAbs);
        }

        void Apply(int beat)
        {
            int n = pattern != null ? pattern.Length : 0;
            if (n == 0) return;
            int i = MTUtil.Mod(beat + beatOffset, n);
            int next = (i + 1) % n;
            visible = pattern[i] == '1';
            warnOut = blinkWarning && visible && pattern[next] == '0';
            warnIn = blinkWarning && !visible && pattern[next] == '1';

            if (colliders != null)
                for (int k = 0; k < colliders.Length; k++)
                    if (colliders[k] != null) colliders[k].enabled = visible;
        }

        void Update()
        {
            float target = visible ? 1f : hiddenAlpha;
            if (warnOut) target = Mathf.Repeat(Time.time * 8f, 1f) < 0.5f ? 1f : 0.45f;
            else if (warnIn) target = 0.4f;

            alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);
            if (renderers == null) return;
            for (int k = 0; k < renderers.Length; k++)
            {
                if (renderers[k] == null) continue;
                Color c = renderers[k].color;
                c.a = alpha;
                renderers[k].color = c;
            }
        }
    }
}
