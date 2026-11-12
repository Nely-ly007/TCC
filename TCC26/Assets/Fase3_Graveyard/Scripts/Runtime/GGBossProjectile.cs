using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Projétil/onda do boss. Shockwave: perfura e corre pelo chão (pule!). Orb: some ao acertar e
    /// pode ser destruído pela nota do Player (shootable).
    /// </summary>
    public class GGBossProjectile : MonoBehaviour, IGGDamageable
    {
        [SerializeField] int damage = 15;
        [SerializeField] bool piercing = false;
        [SerializeField] bool shootable = false;
        [SerializeField] bool rotateToVelocity = false;
        [SerializeField] float pulseAmount = 0.12f;

        Vector2 velocity;
        float lifeLeft = 4f;
        Vector3 baseScale;
        bool launched;

        public void Launch(Vector2 newVelocity, int newDamage, float lifetime)
        {
            velocity = newVelocity;
            damage = newDamage;
            lifeLeft = lifetime;
            launched = true;
            if (rotateToVelocity && velocity.sqrMagnitude > 0.01f)
            {
                float ang = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
            }
        }

        void Start()
        {
            baseScale = transform.localScale;
        }

        void Update()
        {
            if (!launched) return;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            lifeLeft -= Time.deltaTime;
            if (lifeLeft <= 0f) { Destroy(gameObject); return; }
            float p = Mathf.Pow(1f - GGBeatClock.Phase, 2f);
            transform.localScale = baseScale * (1f + pulseAmount * p);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            GGPlayerHealth h = other.GetComponentInParent<GGPlayerHealth>();
            if (h == null) return;
            h.TakeDamage(damage, transform.position);
            if (!piercing) Destroy(gameObject);
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (shootable) Destroy(gameObject);
        }

        /// <summary>Projétil improvisado (círculo colorido) para quando o prefab não foi atribuído.</summary>
        public static GGBossProjectile CreateFallback(Vector3 position, Color color, float size, bool isPiercing, bool isShootable)
        {
            GameObject go = new GameObject("BossProjectile_Fallback");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GGUtil.CircleSprite;
            sr.color = color;
            sr.sortingOrder = 8;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            GGBossProjectile bp = go.AddComponent<GGBossProjectile>();
            bp.piercing = isPiercing;
            bp.shootable = isShootable;
            return bp;
        }
    }
}
