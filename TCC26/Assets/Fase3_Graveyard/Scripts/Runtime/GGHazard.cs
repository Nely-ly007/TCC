using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Perigo de contato (espinhos, serra, bola de espinhos, plataforma de espinhos).
    /// Dano básico = 15 HP, forte = 30 HP. O collider precisa estar neste mesmo GameObject.
    /// </summary>
    public class GGHazard : MonoBehaviour
    {
        [SerializeField] protected int damage = 15;

        /// <summary>Subclasses decidem quando o perigo está "ligado".</summary>
        public virtual bool IsDangerous { get { return true; } }

        public void SetDamage(int value) { damage = value; }

        void OnTriggerStay2D(Collider2D other) { Hurt(other); }
        void OnCollisionStay2D(Collision2D collision) { Hurt(collision.collider); }

        protected virtual void Hurt(Collider2D other)
        {
            if (!IsDangerous || other == null) return;
            GGPlayerHealth h = other.GetComponentInParent<GGPlayerHealth>();
            if (h != null) h.TakeDamage(damage, transform.position);
        }
    }
}
