using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Onda rasteira criada pelo impacto da Rainha. Corre pelo chão; é preciso pular. Dano forte (30).</summary>
    public class HiveShockwave : MonoBehaviour
    {
        private Rigidbody2D rb;
        private float dir = 1f;
        private float speed = 6.5f;
        private float minX, maxX;
        private int damage = 30;
        private float trail;
        private bool done;

        public static HiveShockwave Create(Vector2 groundPos, float direction, float velocity,
                                           float xMin, float xMax, int dmg)
        {
            var go = new GameObject("Shockwave");
            go.transform.position = groundPos;
            go.AddComponent<HiveHostile>();

            var w = go.AddComponent<HiveShockwave>();
            w.dir = direction >= 0f ? 1f : -1f;
            w.speed = velocity;
            w.minX = xMin;
            w.maxX = xMax;
            w.damage = dmg;

            w.rb = go.AddComponent<Rigidbody2D>();
            w.rb.bodyType = RigidbodyType2D.Kinematic;
            w.rb.gravityScale = 0f;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.7f);
            col.offset = new Vector2(0f, 0.35f);

            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            v.transform.localScale = new Vector3(w.dir, 1f, 1f);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Wave();
            sr.sortingOrder = 12;
            return w;
        }

        private void FixedUpdate()
        {
            if (done || rb == null) return;
            Vector2 next = rb.position + new Vector2(dir * speed * Time.fixedDeltaTime, 0f);
            if (next.x < minX || next.x > maxX)
            {
                done = true;
                HiveFx.Burst(rb.position, HivePalette.Pink, 6, 3f, 0.35f, 0.16f);
                Destroy(gameObject);
                return;
            }
            rb.MovePosition(next);

            trail -= Time.fixedDeltaTime;
            if (trail <= 0f)
            {
                trail = 0.07f;
                HiveFx.Burst(rb.position + Vector2.up * 0.2f, HivePalette.Gold, 1, 1.2f, 0.3f, 0.14f, 3f);
            }
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (done || !HiveBridge.IsPlayer(o)) return;
            HiveBridge.DamagePlayer(damage, transform.position, 9f);
        }
    }
}
