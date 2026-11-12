using System.Collections;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Base dos inimigos da Fase 3: vida, dano de contato (15 básico / 30 forte), flash ao ser atingido e drop de nota.
    /// Cada inimigo só tem os colliders como triggers; o dano chega pelo GGProjectileRelay do projétil do Player.
    /// </summary>
    public abstract class GGEnemyBase : MonoBehaviour, IGGDamageable
    {
        [Header("Vida / Dano")]
        [SerializeField] protected int maxHealth = 20;
        [SerializeField] protected int contactDamage = 15;

        [Header("Visual")]
        [SerializeField] protected Transform visual;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected GGSpriteAnimator animator;

        [Header("Drop (nota musical de cura)")]
        [SerializeField] GameObject dropPrefab;
        [SerializeField, Range(0f, 1f)] float dropChance = 0.3f;

        protected int health;
        protected bool dead;
        protected Color baseColor = Color.white;
        float flashUntil;

        public event System.Action<GGEnemyBase> Died;
        public bool IsDead { get { return dead; } }

        protected Transform Player { get { return GGUtil.Player; } }

        public void SetDropChance(float chance) { dropChance = chance; }

        protected virtual void Awake()
        {
            health = maxHealth;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<GGSpriteAnimator>();
            if (visual == null && spriteRenderer != null) visual = spriteRenderer.transform;
            if (spriteRenderer != null) baseColor = spriteRenderer.color;
        }

        public virtual void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (dead || amount <= 0) return;
            health -= amount;
            flashUntil = Time.time + 0.12f;
            OnHit(sourcePosition);
            if (health <= 0) Die();
        }

        protected virtual void OnHit(Vector2 sourcePosition) { }

        protected virtual void Die()
        {
            if (dead) return;
            dead = true;
            StopAllCoroutines();
            if (Died != null) Died(this);
            if (dropPrefab != null && Random.value < dropChance)
                Instantiate(dropPrefab, transform.position + Vector3.up * 0.4f, Quaternion.identity);
            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            float t = 0f;
            Vector3 s0 = transform.localScale;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                if (spriteRenderer != null)
                {
                    Color c = baseColor;
                    c.a = baseColor.a * (1f - k);
                    spriteRenderer.color = c;
                }
                transform.localScale = s0 * (1f + 0.25f * k);
                yield return null;
            }
            Destroy(gameObject);
        }

        protected virtual void LateUpdate()
        {
            if (dead || spriteRenderer == null) return;
            spriteRenderer.color = Time.time < flashUntil
                ? new Color(1f, 0.55f, 0.55f, baseColor.a)
                : baseColor;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (dead || !CanHurtPlayer) return;
            GGPlayerHealth h = other.GetComponentInParent<GGPlayerHealth>();
            if (h != null) h.TakeDamage(contactDamage, transform.position);
        }

        protected virtual bool CanHurtPlayer { get { return true; } }

        protected void SetFacing(bool faceRight)
        {
            if (visual == null) return;
            Vector3 s = visual.localScale;
            float sign = faceRight ? 1f : -1f;
            if (Mathf.Sign(s.x) != sign)
            {
                s.x = Mathf.Abs(s.x) * sign;
                visual.localScale = s;
            }
        }

        protected void FacePlayer()
        {
            Transform p = Player;
            if (p != null) SetFacing(p.position.x >= transform.position.x);
        }

        protected void PlayAnim(string clip)
        {
            if (animator != null) animator.Play(clip);
        }
    }
}
