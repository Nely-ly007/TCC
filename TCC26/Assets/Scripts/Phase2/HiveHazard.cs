using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Zona que machuca o Player ao encostar (espinhos de cera). Dano básico = 15.</summary>
    public class HiveHazard : MonoBehaviour
    {
        private int damage = 15;

        public void Init(Vector2 size, int dmg)
        {
            damage = dmg;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
        }

        private void OnTriggerEnter2D(Collider2D o) { Touch(o, damage, transform.position); }
        private void OnTriggerStay2D(Collider2D o) { Touch(o, damage, transform.position); }

        public static void Touch(Collider2D o, int dmg, Vector2 source)
        {
            if (!HiveBridge.IsPlayer(o)) return;
            HiveBridge.DamagePlayer(dmg, source, 8f);
        }
    }
}
