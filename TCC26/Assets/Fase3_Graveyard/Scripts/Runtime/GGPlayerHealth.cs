using System;
using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Vida do Player na Fase 3 (HP 100, sem regeneração, cura só por notas musicais).
    /// Coloque no Player (tag "Player"). Se a vida já é controlada pelo seu PlayerController,
    /// desmarque "Use Internal Health" e ligue o evento onDamaged ao seu método de dano.
    /// </summary>
    [DisallowMultipleComponent]
    public class GGPlayerHealth : MonoBehaviour, IGGDamageable
    {
        [Header("Vida")]
        [SerializeField] int maxHealth = 100;
        [Tooltip("Desmarque se a vida é controlada pelo seu PlayerController (use onDamaged para repassar o dano).")]
        [SerializeField] bool useInternalHealth = true;
        [SerializeField] float invulnerabilitySeconds = 1f;
        [SerializeField] float knockbackImpulse = 5f;

        [Header("Feedback visual")]
        [Tooltip("Se vazio, usa todos os SpriteRenderers filhos.")]
        [SerializeField] SpriteRenderer[] flashRenderers;
        [SerializeField] Color hurtColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] float blinkRate = 7f;

        [Header("Eventos")]
        [Tooltip("Dano recebido (repasse ao SEU sistema de vida se Use Internal Health estiver desmarcado).")]
        public GGIntEvent onDamaged;
        [Tooltip("Cura recebida (nota musical / coração). Dispara mesmo com Use Internal Health desmarcado.")]
        public GGIntEvent onHealed;
        public GGIntIntEvent onHealthChanged;
        public UnityEvent onDied;

        public event Action Died;

        public int Current { get; private set; }
        public int Max { get { return maxHealth; } }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

        float invulnerableUntil;
        Rigidbody2D rb;
        Color[] baseColors;
        bool wasBlinking;

        void Awake()
        {
            Current = maxHealth;
            rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // OnTriggerStay precisa de corpo acordado
            if (flashRenderers == null || flashRenderers.Length == 0)
                flashRenderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
                baseColors[i] = flashRenderers[i] != null ? flashRenderers[i].color : Color.white;
        }

        void Start()
        {
            if (onHealthChanged != null) onHealthChanged.Invoke(Current, maxHealth);
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (IsDead || amount <= 0 || IsInvulnerable) return;
            invulnerableUntil = Time.time + invulnerabilitySeconds;

            if (onDamaged != null) onDamaged.Invoke(amount);

            if (useInternalHealth)
            {
                Current = Mathf.Max(0, Current - amount);
                if (onHealthChanged != null) onHealthChanged.Invoke(Current, maxHealth);
            }

            if (rb != null && knockbackImpulse > 0f)
            {
                float dir = transform.position.x >= sourcePosition.x ? 1f : -1f;
                rb.AddForce(new Vector2(dir, 0.6f).normalized * knockbackImpulse, ForceMode2D.Impulse);
            }

            GGCameraFollow.Shake(0.25f, 0.2f);

            if (useInternalHealth && Current <= 0) Die();
        }

        /// <summary>Dano que ignora a invulnerabilidade (ex.: cair no abismo).</summary>
        public void ForceDamage(int amount, Vector2 sourcePosition)
        {
            invulnerableUntil = 0f;
            TakeDamage(amount, sourcePosition);
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            if (useInternalHealth)
            {
                Current = Mathf.Min(maxHealth, Current + amount);
                if (onHealthChanged != null) onHealthChanged.Invoke(Current, maxHealth);
            }
            if (onHealed != null) onHealed.Invoke(amount);
        }

        /// <summary>
        /// Modo externo (Use Internal Health desmarcado): chame quando o SEU sistema de vida zerar o HP,
        /// para o GGLevelManager fazer o respawn no checkpoint.
        /// </summary>
        public void NotifyDied()
        {
            Die();
        }

        public void RestoreFull()
        {
            IsDead = false;
            Current = maxHealth;
            invulnerableUntil = Time.time + 1.5f;
            if (onHealthChanged != null) onHealthChanged.Invoke(Current, maxHealth);
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            if (onDied != null) onDied.Invoke();
            if (Died != null) Died();
        }

        void Update()
        {
            bool blinking = IsInvulnerable && !IsDead;
            if (blinking)
            {
                bool on = Mathf.Sin(Time.time * blinkRate * Mathf.PI * 2f) > 0f;
                for (int i = 0; i < flashRenderers.Length; i++)
                {
                    if (flashRenderers[i] == null) continue;
                    flashRenderers[i].color = on ? Color.Lerp(baseColors[i], hurtColor, 0.75f) : baseColors[i];
                }
                wasBlinking = true;
            }
            else if (wasBlinking)
            {
                for (int i = 0; i < flashRenderers.Length; i++)
                    if (flashRenderers[i] != null) flashRenderers[i].color = baseColors[i];
                wasBlinking = false;
            }
        }
    }
}
