using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Gota de mel que cai e causa dano ao tocar o jogador; espirra ao tocar o cenário.</summary>
    public class HiveHoneyDrop : MonoBehaviour
    {
        private Rigidbody2D rb;
        private float speed = 9f;
        private int damage = 15;
        private float life = 6f;
        private bool done;

        public static HiveHoneyDrop Create(Vector2 pos, float fallSpeed, int dmg)
        {
            var go = new GameObject("HoneyDrop");
            go.transform.position = pos;
            go.AddComponent<HiveHostile>();

            var d = go.AddComponent<HiveHoneyDrop>();
            d.speed = fallSpeed;
            d.damage = dmg;

            d.rb = go.AddComponent<Rigidbody2D>();
            d.rb.bodyType = RigidbodyType2D.Kinematic;
            d.rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.18f;

            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            v.transform.localScale = Vector3.one * 0.8f;
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Drop();
            sr.sortingOrder = 8;
            return d;
        }

        private void FixedUpdate()
        {
            if (done || rb == null) return;
            life -= Time.fixedDeltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            rb.MovePosition(rb.position + Vector2.down * speed * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (done) return;
            if (HiveBridge.IsPlayer(o))
            {
                HiveBridge.DamagePlayer(damage, transform.position, 4f);
                Splash();
                return;
            }
            if (o.isTrigger) return;
            if (o.GetComponentInParent<HiveDamageable>() != null) return;
            GameObject proj;
            if (HiveBridge.TryGetPlayerProjectile(o, out proj)) return;
            Splash();
        }

        private void Splash()
        {
            done = true;
            HiveFx.Burst(transform.position, HivePalette.Honey, 8, 3.2f, 0.45f, 0.14f, 10f, HiveSpriteFactory.Dot());
            HiveSfx.Play(HiveSfxId.Splash, 0.3f, Random.Range(0.9f, 1.15f));
            Destroy(gameObject);
        }
    }
}
