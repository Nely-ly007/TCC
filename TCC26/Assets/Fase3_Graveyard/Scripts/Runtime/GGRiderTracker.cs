using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Faz o Player "pegar carona" em plataformas que se movem (trilho, elevador, plataforma rítmica que sobe).
    /// Requer um Collider2D sólido (não trigger) na mesma plataforma.
    /// </summary>
    public class GGRiderTracker : MonoBehaviour
    {
        readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
        readonly HashSet<Rigidbody2D> nextRiders = new HashSet<Rigidbody2D>();
        Collider2D col;

        void Awake()
        {
            col = GetComponent<Collider2D>();
        }

        void OnCollisionStay2D(Collision2D c)
        {
            Rigidbody2D rb = c.rigidbody;
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic || col == null) return;
            // só quem está EM CIMA da plataforma
            if (c.collider.bounds.min.y >= col.bounds.max.y - 0.3f) nextRiders.Add(rb);
        }

        void OnCollisionExit2D(Collision2D c)
        {
            Rigidbody2D rb = c.rigidbody;
            if (rb == null) return;
            riders.Remove(rb);
            nextRiders.Remove(rb);
        }

        /// <summary>Chame no FixedUpdate da plataforma com o deslocamento que ela fará neste passo.</summary>
        public void Carry(Vector2 delta)
        {
            if (delta.sqrMagnitude > 1e-9f)
            {
                foreach (Rigidbody2D rb in riders)
                    if (rb != null) rb.position += delta;
            }
            riders.Clear();
            foreach (Rigidbody2D rb in nextRiders) riders.Add(rb);
            nextRiders.Clear();
        }
    }
}
