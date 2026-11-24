using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>Bandeira de checkpoint. Ao tocar, vira o ponto de reinício (HP = 0 → volta aqui).</summary>
    public class GGCheckpoint : MonoBehaviour
    {
        [SerializeField] Transform respawnPoint;
        [SerializeField] SpriteRenderer flag;
        [SerializeField] Color inactiveColor = new Color(0.42f, 0.42f, 0.5f, 1f);
        [SerializeField] Color activeColor = Color.white;

        public bool IsActive { get; private set; }

        public Vector3 RespawnPosition
        {
            get { return respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up * 0.5f; }
        }

        Vector3 flagScale;

        void Start()
        {
            if (flag != null)
            {
                flagScale = flag.transform.localScale;
                flag.color = inactiveColor;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive) return;
            if (other.GetComponentInParent<GGPlayerHealth>() == null) return;
            IsActive = true;
            if (GGLevelManager.Instance != null) GGLevelManager.Instance.SetCheckpoint(this);
            GGCameraFollow.Shake(0.12f, 0.15f);
        }

        void Update()
        {
            if (!IsActive || flag == null) return;
            float p = Mathf.Pow(1f - GGBeatClock.Phase, 3f);
            flag.transform.localScale = flagScale * (1f + 0.06f * p);
            flag.color = Color.Lerp(activeColor, Color.white, p * 0.3f);
        }
    }
}
