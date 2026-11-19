using UnityEngine;

/// <summary>
/// Sistema de partículas leve e genérico — funciona em qualquer fase.
/// Uso:
///   GameFx.Burst(pos, cor, 12)       — explosão de partículas
///   GameFx.Ring(pos, cor, 2f)        — onda de impacto
/// Nenhuma dependência de namespace de fase.
/// </summary>
public class GameFx : MonoBehaviour
{
    private const int Max = 300;

    private struct Particle
    {
        public bool active;
        public SpriteRenderer sr;
        public Vector2 pos, vel;
        public float life, maxLife, size0, size1, gravity, spin;
        public Color color;
    }

    private static GameFx inst;
    private Particle[] ps;
    private int created;

    private static GameFx Inst
    {
        get
        {
            if (inst == null)
            {
                var go = new GameObject("GameFx");
                DontDestroyOnLoad(go);
                inst = go.AddComponent<GameFx>();
                inst.ps = new Particle[Max];
            }
            return inst;
        }
    }

    // ── Sprites gerados em código ──────────────────────────────
    private static Sprite _dot;
    private static Sprite _ring;

    private static Sprite Dot()
    {
        if (_dot != null) return _dot;
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(7.5f, 7.5f);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(1f - (d - 5.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        _dot = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        return _dot;
    }

    private static Sprite Ring()
    {
        if (_ring != null) return _ring;
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 13f) / 1.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        _ring = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        return _ring;
    }

    // ── API pública ────────────────────────────────────────────
    public static void Burst(Vector2 pos, Color color, int count = 10, float speed = 4f,
                             float life = 0.5f, float size = 0.18f, float gravity = 0f)
    {
        var f = Inst;
        for (int i = 0; i < count; i++)
        {
            float a = Random.value * Mathf.PI * 2f;
            float s = speed * Random.Range(0.35f, 1f);
            f.Spawn(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, color,
                    life * Random.Range(0.7f, 1.2f),
                    size * Random.Range(0.7f, 1.3f), 0f, gravity,
                    Random.Range(-240f, 240f), Dot());
        }
    }

    public static void Ring(Vector2 pos, Color color, float radius = 2f, float life = 0.4f)
    {
        Inst.Spawn(pos, Vector2.zero, color, life, 0.2f, radius * 2f, 0f, 0f, Ring());
    }

    // ── Interno ────────────────────────────────────────────────
    private void Spawn(Vector2 pos, Vector2 vel, Color color, float life,
                       float size0, float size1, float gravity, float spin, Sprite sprite)
    {
        int idx = -1;
        for (int i = 0; i < created; i++)
            if (!ps[i].active) { idx = i; break; }
        if (idx < 0)
        {
            if (created >= Max) return;
            idx = created++;
            var go = new GameObject("fx");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 30;
            ps[idx].sr = sr;
        }

        if (size1 <= 0f) size1 = size0 * 0.1f;
        ps[idx].active = true;
        ps[idx].pos = pos;
        ps[idx].vel = vel;
        ps[idx].life = life;
        ps[idx].maxLife = Mathf.Max(0.01f, life);
        ps[idx].size0 = size0;
        ps[idx].size1 = size1;
        ps[idx].gravity = gravity;
        ps[idx].spin = spin;
        ps[idx].color = color;
        ps[idx].sr.sprite = sprite;
        ps[idx].sr.color = color;
        ps[idx].sr.transform.position = pos;
        ps[idx].sr.transform.localScale = Vector3.one * size0;
        ps[idx].sr.gameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < created; i++)
        {
            if (!ps[i].active) continue;
            ps[i].life -= dt;
            if (ps[i].life <= 0f)
            {
                ps[i].active = false;
                ps[i].sr.gameObject.SetActive(false);
                continue;
            }
            float k = 1f - ps[i].life / ps[i].maxLife;
            ps[i].vel.y -= ps[i].gravity * dt;
            ps[i].pos += ps[i].vel * dt;
            var t = ps[i].sr.transform;
            t.position = ps[i].pos;
            t.localScale = Vector3.one * Mathf.Lerp(ps[i].size0, ps[i].size1, k);
            t.Rotate(0f, 0f, ps[i].spin * dt);
            Color c = ps[i].color;
            c.a *= 1f - k * k;
            ps[i].sr.color = c;
        }
    }
}