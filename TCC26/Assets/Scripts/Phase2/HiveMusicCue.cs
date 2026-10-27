using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Zona invisível: quando o Player entra, pede a troca de música para a seção escolhida
    /// (Loop principal ou Combate). A troca acontece no início do próximo compasso.
    /// Ignorada durante a luta do boss (a música do boss é controlada pela arena).
    /// </summary>
    public class HiveMusicCue : MonoBehaviour
    {
        private HiveMusicSection section = HiveMusicSection.Combat;

        public void Init(Vector2 size, HiveMusicSection target)
        {
            section = target;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (!HiveBridge.IsPlayer(o)) return;
            if (HiveLevelBootstrap.BossActive) return;
            HiveMusic.Request(section);
        }
    }
}
