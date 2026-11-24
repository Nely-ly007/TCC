using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Adicione ao prefab do projétil de nota musical do Player (MusicProjectile).
    /// Repassa dano para objetos da Fase 3 que implementem IGGDamageable.
    /// </summary>
    public class GGProjectileRelay : MonoBehaviour
    {
        [SerializeField] private int damage = 10;

        [Tooltip("Destroi o projétil depois de atingir algo da Fase 3.")]
        [SerializeField] private bool destroyOnHit = false;

        // Guarda os componentes já atingidos sem usar GetInstanceID().
        private readonly HashSet<Component> alreadyHit = new HashSet<Component>();

        public void SetDamage(int value)
        {
            damage = value;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHit(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryHit(collision.collider);
        }

        private void TryHit(Collider2D other)
        {
            if (other == null) return;

            // Não causar dano ao Player.
            if (other.GetComponentInParent<GGPlayerHealth>() != null)
                return;

            IGGDamageable target = other.GetComponentInParent<IGGDamageable>();
            if (target == null) return;

            Component targetComponent = target as Component;

            if (targetComponent == null)
                return;

            // Evita acertar o mesmo alvo várias vezes.
            if (!alreadyHit.Add(targetComponent))
                return;

            target.TakeDamage(damage, transform.position);

            if (destroyOnHit)
                Destroy(gameObject);
        }
    }
}