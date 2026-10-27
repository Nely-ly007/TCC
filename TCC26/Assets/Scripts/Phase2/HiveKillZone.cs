using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Abismo: ao cair, o Player volta ao último checkpoint e perde 15 HP.</summary>
    public class HiveKillZone : MonoBehaviour
    {
        private float cooldown;

        public void Init(Vector2 size)
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (Time.time < cooldown || !HiveBridge.IsPlayer(o)) return;
            cooldown = Time.time + 0.8f;
            var b = HiveLevelBootstrap.Instance;
            if (b != null) b.PitFall();
        }
    }
}
