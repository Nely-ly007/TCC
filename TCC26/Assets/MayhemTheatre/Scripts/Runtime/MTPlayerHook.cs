using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Ponte entre a Fase 4 e o seu Player. Coloque no Player (o builder faz isso se achar o objeto com tag "Player").
    ///  - Hazards/boss/inimigos chamam TakeDamage aqui.
    ///  - Ligue On Damaged / On Healed ao seu sistema de vida, OU deixe "Use Internal Health" ligado.
    ///  - Se usar sua própria vida, chame MTPlayerHook.Instance.ReportDeath() quando o Player morrer.
    /// </summary>
    public class MTPlayerHook : MonoBehaviour
    {
        public static MTPlayerHook Instance { get; private set; }

        [Header("Vida espelho (usada para respawn e para a luta)")]
        public float maxHealth = 100f;
        [Tooltip("Ligado: este componente controla a vida (0 = morreu). Desligado: use On Damaged e ReportDeath().")]
        public bool useInternalHealth = true;
        public float invulnerableSeconds = 0.8f;
        public float knockbackImpulse = 5f;
        public bool blinkWhenHit = true;

        [Header("Ligue ao seu sistema de vida")]
        public UnityEvent<float> onDamaged = new UnityEvent<float>();
        public UnityEvent<float> onHealed = new UnityEvent<float>();
        public UnityEvent onDied = new UnityEvent();
        public UnityEvent onRespawned = new UnityEvent();

        public float Health { get; private set; }
        public bool IsDead { get; private set; }
        /// <summary>Multiplique a velocidade do seu PlayerController por isto (1 = normal). Usado pelas zonas de lentidão.</summary>
        public float SpeedMultiplier { get; private set; }
        public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

        float invulnerableUntil;
        float slowUntil;
        SpriteRenderer[] renderers;
        Rigidbody2D rb;
        bool blinkApplied;

        void Awake()
        {
            Instance = this;
            Health = maxHealth;
            SpeedMultiplier = 1f;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (SpeedMultiplier != 1f && Time.time > slowUntil) SpeedMultiplier = 1f;

            if (blinkWhenHit)
            {
                if (IsInvulnerable && !IsDead)
                {
                    bool low = Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
                    SetAlpha(low ? 0.35f : 1f);
                    blinkApplied = true;
                }
                else if (blinkApplied)
                {
                    SetAlpha(1f);
                    blinkApplied = false;
                }
            }
        }

        void SetAlpha(float a)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = renderers[i].color;
                c.a = a;
                renderers[i].color = c;
            }
        }

        /// <summary>Retorna true se o dano foi aplicado.</summary>
        public bool TakeDamage(float amount, Vector2 sourcePosition)
        {
            if (IsDead || IsInvulnerable || amount <= 0f) return false;

            invulnerableUntil = Time.time + invulnerableSeconds;
            if (useInternalHealth) Health = Mathf.Max(0f, Health - amount);
            onDamaged.Invoke(amount);

            if (rb != null && knockbackImpulse > 0f)
            {
                float dir = transform.position.x >= sourcePosition.x ? 1f : -1f;
                rb.AddForce(new Vector2(dir * knockbackImpulse, knockbackImpulse * 0.5f), ForceMode2D.Impulse);
            }

            if (useInternalHealth && Health <= 0f) ReportDeath();
            return true;
        }

        /// <summary>Morte instantânea (queda no vazio). Ignora invulnerabilidade.</summary>
        public void Kill()
        {
            if (IsDead) return;
            if (useInternalHealth) Health = 0f;
            ReportDeath();
        }

        /// <summary>Chame isto quando o SEU sistema de vida decidir que o Player morreu.</summary>
        public void ReportDeath()
        {
            if (IsDead) return;
            IsDead = true;
            onDied.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            if (useInternalHealth) Health = Mathf.Min(maxHealth, Health + amount);
            onHealed.Invoke(amount);
        }

        public void ApplySlow(float multiplier, float seconds)
        {
            SpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
            slowUntil = Time.time + seconds;
        }

        public void Respawn(Vector3 position)
        {
            IsDead = false;
            Health = maxHealth;
            SpeedMultiplier = 1f;
            invulnerableUntil = Time.time + 1.5f;
            transform.position = position;
            if (rb != null) MTUtil.SetVelocity(rb, Vector2.zero);
            SetAlpha(1f);
            onRespawned.Invoke();
        }
    }
}
