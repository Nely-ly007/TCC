using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Colocado automaticamente na instância do prefab do boss: repassa contatos ao MTBossController.</summary>
    public class MTBossBody : MonoBehaviour
    {
        public MTBossController owner;

        void OnTriggerEnter2D(Collider2D other) { if (owner != null) owner.HandleContact(other, true); }
        void OnTriggerStay2D(Collider2D other) { if (owner != null) owner.HandleContact(other, false); }
        void OnCollisionEnter2D(Collision2D c) { if (owner != null) owner.HandleContact(c.collider, true); }
        void OnCollisionStay2D(Collision2D c) { if (owner != null) owner.HandleContact(c.collider, false); }
    }
}
