using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Causa dano ao Player por contato (trigger ou colisão). O MTPlayerHook cuida da invulnerabilidade.</summary>
    public class MTContactDamage : MonoBehaviour
    {
        public float damage = 15f;
        public bool active = true;

        void OnTriggerEnter2D(Collider2D other) { Hit(other); }
        void OnTriggerStay2D(Collider2D other) { Hit(other); }
        void OnCollisionEnter2D(Collision2D c) { Hit(c.collider); }
        void OnCollisionStay2D(Collision2D c) { Hit(c.collider); }

        void Hit(Collider2D other)
        {
            if (!active || other == null) return;
            MTPlayerHook hook = other.GetComponentInParent<MTPlayerHook>();
            if (hook != null) hook.TakeDamage(damage, transform.position);
        }
    }
}
