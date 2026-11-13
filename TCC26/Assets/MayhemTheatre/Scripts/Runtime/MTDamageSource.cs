using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Coloque no prefab do projétil/nota do Player (MusicProjectile).
    /// Inimigos e boss da Fase 4 leem o dano daqui. (Sem este componente, a Fase 4 também
    /// aceita objetos cujo nome contenha "Projectile", "MusicNote" ou "Melody" com dano 10.)
    /// </summary>
    public class MTDamageSource : MonoBehaviour
    {
        public float damage = 10f;
        [Tooltip("Destroi o projétil quando ele acerta um inimigo/boss da Fase 4.")]
        public bool destroyOnHit = true;

        public void NotifyHit()
        {
            if (destroyOnHit) Destroy(gameObject);
        }
    }
}
