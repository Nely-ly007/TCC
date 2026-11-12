using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>Abismo: cobra HP e devolve o Player ao último checkpoint. Trigger grande embaixo do nível.</summary>
    public class GGKillZone : MonoBehaviour
    {
        [SerializeField] int pitDamage = 15;

        void OnTriggerEnter2D(Collider2D other)
        {
            GGPlayerHealth h = other.GetComponentInParent<GGPlayerHealth>();
            if (h == null) return;
            h.ForceDamage(pitDamage, transform.position);
            if (!h.IsDead && GGLevelManager.Instance != null)
                GGLevelManager.Instance.TeleportPlayerToCheckpoint();
        }
    }
}
