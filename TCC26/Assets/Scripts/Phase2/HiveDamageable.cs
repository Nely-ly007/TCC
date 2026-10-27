using System;
using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Vida + receber dano dos projéteis de nota do jogador (MusicProjectile) + flash de dano.
    /// Funciona com projétil trigger ou sólido. Se o seu MusicProjectile já causar dano por conta própria,
    /// desative "consumeProjectiles" e chame TakeHit(...) no seu script.
    /// </summary>
    public class HiveDamageable : MonoBehaviour
    {
        public float maxHealth = 20f;
        public float health = 20f;
        public bool invulnerable;
        [Tooltip("Multiplica o dano recebido (o boss usa 1.5 quando está atordoado no chão).")]
        public float damageMultiplier = 1f;
        public bool consumeProjectiles = true;

        public event Action<HiveDamageable, float, Vector2> Damaged;   // (self, dano aplicado, origem)
        public event Action<HiveDamageable> Died;

        public bool IsDead { get; private set; }
        public float Normalized { get { return maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth); } }

        private SpriteRenderer[] renderers;
        private Color[] baseColors;
        private float flashT;
        private readonly HashSet<int> seen = new HashSet<int>();
        private float seenClearAt;

        public void Setup(float hp)
        {
            maxHealth = hp;
            health = hp;
            IsDead = false;
        }

        public void TakeHit(float dmg, Vector2 from)
        {
            if (IsDead || invulnerable) return;
            float applied = dmg * damageMultiplier;
            health -= applied;
            flashT = 0.12f;
            HiveSfx.Play(HiveSfxId.Hit, 0.5f, UnityEngine.Random.Range(0.9f, 1.2f));
            var h = Damaged;
            if (h != null) h(this, applied, from);
            if (health <= 0f) Kill();
        }

        public void Kill()
        {
            if (IsDead) return;
            IsDead = true;
            health = 0f;
            RestoreColors();
            var d = Died;
            if (d != null) d(this);
        }

        private void OnTriggerEnter2D(Collider2D o) { HandleProjectile(o); }
        private void OnCollisionEnter2D(Collision2D c) { HandleProjectile(c.collider); }

        [Obsolete("Obsolete")]
        private void HandleProjectile(Collider2D o)
        {
            if (IsDead) return;
            GameObject proj;
            if (!HiveBridge.TryGetPlayerProjectile(o, out proj)) return;

            if (Time.time > seenClearAt) { seen.Clear(); seenClearAt = Time.time + 5f; }
            if (!seen.Add(proj.GetInstanceID())) return;

            Vector2 at = proj.transform.position;
            TakeHit(HiveBridge.GetProjectileDamage(proj), at);
            HiveFx.Burst(at, HivePalette.Pink, 6, 3f, 0.3f, 0.14f);
            if (consumeProjectiles) Destroy(proj);
        }

        private void Start()
        {
            CacheRenderers();
        }

        private void CacheRenderers()
        {
            if (renderers != null) return;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        }

        private void RestoreColors()
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = baseColors[i];
        }

        private void LateUpdate()
        {
            if (flashT <= 0f) return;
            CacheRenderers();
            flashT -= Time.deltaTime;
            float k = Mathf.Clamp01(flashT / 0.12f);
            Color hot = new Color(1f, 0.3f, 0.55f, 1f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color b = baseColors[i];
                Color c = Color.Lerp(b, hot, k);
                c.a = b.a;
                renderers[i].color = c;
            }
            if (flashT <= 0f) RestoreColors();
        }
    }
}
