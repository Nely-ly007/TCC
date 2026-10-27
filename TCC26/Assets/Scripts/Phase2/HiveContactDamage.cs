using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Causa dano ao Player por contato (inimigos, boss). Básico = 15, forte = 30.</summary>
    public class HiveContactDamage : MonoBehaviour
    {
        public int damage = 15;
        public float knockback = 7f;
        public bool active = true;

        private void OnTriggerEnter2D(Collider2D o) { Hit(o); }
        private void OnTriggerStay2D(Collider2D o) { Hit(o); }
        private void OnCollisionEnter2D(Collision2D c) { Hit(c.collider); }
        private void OnCollisionStay2D(Collision2D c) { Hit(c.collider); }

        private void Hit(Collider2D o)
        {
            if (!active || !HiveBridge.IsPlayer(o)) return;
            HiveBridge.DamagePlayer(damage, transform.position, knockback);
        }
    }
}
