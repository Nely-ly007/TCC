using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Checkpoint intermediário (trigger).</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class MTCheckpoint : MonoBehaviour
    {
        public MTMoment moment = MTMoment.Fase1;

        void Reset()
        {
            BoxCollider2D b = GetComponent<BoxCollider2D>();
            if (b != null) b.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!MTUtil.IsPlayer(other) || MTLevelController.Instance == null) return;
            MTLevelController.Instance.SetCheckpoint(transform.position, moment);
        }
    }
}
