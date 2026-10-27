using UnityEngine;
using UnityEngine.Tilemaps;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Coloque no Tilemap "Hazards": qualquer tile pintado ali machuca o Player (espinhos/cera).
    /// O TilemapCollider2D vira trigger automaticamente.
    /// </summary>
    [RequireComponent(typeof(TilemapCollider2D))]
    public class HiveTilemapHazard : MonoBehaviour
    {
        [SerializeField] private int damage = 15;

        private void Awake()
        {
            var tc = GetComponent<TilemapCollider2D>();
            tc.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D o) { Hit(o); }
        private void OnTriggerStay2D(Collider2D o) { Hit(o); }

        private void Hit(Collider2D o)
        {
            if (!HiveBridge.IsPlayer(o)) return;
            Vector2 src = o.bounds.center;
            Rigidbody2D prb = HiveBridge.PlayerRb;
            if (prb != null)
            {
                float vx = HiveBridge.GetVelocity(prb).x;
                src = prb.position + new Vector2(Mathf.Abs(vx) > 0.1f ? Mathf.Sign(vx) : 0.5f, 0f);
            }
            HiveHazard.Touch(o, damage, src);
        }
    }
}
