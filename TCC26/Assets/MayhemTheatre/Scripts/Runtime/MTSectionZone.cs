using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Zona (trigger) de uma seção: ao entrar, toca o momento musical e salva o checkpoint.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class MTSectionZone : MonoBehaviour
    {
        public MTMoment moment = MTMoment.Fase1;
        public bool makeCheckpoint = true;
        public Transform checkpointPoint;
        public UnityEvent onEnter = new UnityEvent();

        void Reset()
        {
            BoxCollider2D b = GetComponent<BoxCollider2D>();
            if (b != null) b.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!MTUtil.IsPlayer(other)) return;

            if (MTMusicDirector.Instance != null) MTMusicDirector.Instance.PlayMoment(moment);
            if (makeCheckpoint && MTLevelController.Instance != null)
            {
                Vector3 p = checkpointPoint != null ? checkpointPoint.position : transform.position;
                MTLevelController.Instance.SetCheckpoint(p, moment);
            }
            onEnter.Invoke();
        }
    }
}
