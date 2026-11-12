using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Adicione ao prefab do projétil de nota musical do Player (o MusicProjectile).
    /// Repassa o dano para qualquer coisa da Fase 3 que implemente IGGDamageable
    /// (inimigos, boss, orbs, alvos de ressonância). Não mexe no seu script do projétil.
    /// </summary>
    public class GGProjectileRelay : MonoBehaviour
    {
        [SerializeField] int damage = 10;
        [Tooltip("Destroi o projétil depois de atingir algo da Fase 3.")]
        [SerializeField] bool destroyOnHit = false;

        readonly HashSet<int> alreadyHit = new HashSet<int>();

        public void SetDamage(int value) { damage = value; }

        void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
        void OnCollisionEnter2D(Collision2D collision) { TryHit(collision.collider); }

        void TryHit(Collider2D other)
        {
            if (other == null) return;
            if (other.GetComponentInParent<GGPlayerHealth>() != null) return;

            IGGDamageable target = other.GetComponentInParent<IGGDamageable>();
            if (target == null) return;

            Component c = target as Component;
            // Substituído GetInstanceID() por GetHashCode() para evitar o aviso/erro de obsolescência
            int id = c != null ? c.GetHashCode() : other.GetHashCode();
            if (!alreadyHit.Add(id)) return;

            target.TakeDamage(damage, transform.position);
            if (destroyOnHit) Destroy(gameObject);
        }
    }
}