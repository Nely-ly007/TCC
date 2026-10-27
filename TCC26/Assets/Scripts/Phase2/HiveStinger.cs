using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Ferrão disparado pela Rainha. Dano básico (15). A nota do jogador cancela o ferrão.</summary>
    public class HiveStinger : MonoBehaviour
    {
        private Rigidbody2D rb;
        private Vector2 vel;
        private int damage = 15;
        private float life = 5f;
        private bool done;

        public static HiveStinger Create(Vector2 pos, Vector2 dir, float speed, int dmg)
        {
            var go = new GameObject("Stinger");
            go.transform.position = pos;
            go.AddComponent<HiveHostile>();

            var s = go.AddComponent<HiveStinger>();
            s.vel = dir.normalized * speed;
            s.damage = dmg;
            float ang = Mathf.Atan2(s.vel.y, s.vel.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, ang);

            s.rb = go.AddComponent<Rigidbody2D>();
            s.rb.bodyType = RigidbodyType2D.Kinematic;
            s.rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Stinger();
            sr.sortingOrder = 12;

            HiveSfx.Play(HiveSfxId.Shoot, 0.22f, Random.Range(0.9f, 1.2f));
            return s;
        }

        private void FixedUpdate()
        {
            if (done || rb == null) return;
            life -= Time.fixedDeltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            rb.MovePosition(rb.position + vel * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (done) return;

            if (HiveBridge.IsPlayer(o))
            {
                HiveBridge.DamagePlayer(damage, transform.position, 6f);
                Pop();
                return;
            }

            GameObject proj;
            if (HiveBridge.TryGetPlayerProjectile(o, out proj))
            {
                Pop();   // a nota cancela o ferrão (e continua)
                return;
            }

            if (o.isTrigger) return;
            if (o.GetComponentInParent<HiveDamageable>() != null) return;
            Pop();
        }

        private void Pop()
        {
            done = true;
            HiveFx.Burst(transform.position, HivePalette.Pink, 6, 3f, 0.3f, 0.14f);
            Destroy(gameObject);
        }
    }
}
