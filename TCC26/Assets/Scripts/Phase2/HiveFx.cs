using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Partículas leves feitas com SpriteRenderers em pool (sem ParticleSystem/materiais).
    /// Uso: HiveFx.Burst(pos, cor, 12), HiveFx.Ring(pos, cor, 2f).
    /// </summary>
    public class HiveFx : MonoBehaviour
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

        private static HiveFx inst;
        private Particle[] ps;
        private int created;

        private static HiveFx Inst
        {
            get
            {
                if (inst == null)
                {
                    var go = new GameObject("HiveFx");
                    inst = go.AddComponent<HiveFx>();
                    inst.ps = new Particle[Max];
                }
                return inst;
            }
        }

        public static void Burst(Vector2 pos, Color color, int count = 10, float speed = 4f, float life = 0.5f,
                                 float size = 0.18f, float gravity = 0f, Sprite sprite = null)
        {
            var f = Inst;
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                float s = speed * Random.Range(0.35f, 1f);
                f.Spawn(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, color, life * Random.Range(0.7f, 1.2f),
                        size * Random.Range(0.7f, 1.3f), 0f, gravity, Random.Range(-240f, 240f),
                        sprite != null ? sprite : HiveSpriteFactory.HexFill());
            }
        }

        /// <summary>Hexágono que expande e some (onda de impacto).</summary>
        public static void Ring(Vector2 pos, Color color, float radius = 2f, float life = 0.4f)
        {
            Inst.Spawn(pos, Vector2.zero, color, life, 0.2f, radius * 2f, 0f, 0f, HiveSpriteFactory.HexOutline());
        }

        private void Spawn(Vector2 pos, Vector2 vel, Color color, float life, float size0, float size1,
                           float gravity, float spin, Sprite sprite)
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
}
