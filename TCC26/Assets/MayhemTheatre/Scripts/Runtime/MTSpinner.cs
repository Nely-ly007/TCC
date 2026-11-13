using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Gira continuamente (serra com máscara). Combine com MTContactDamage e, se quiser, MTMovingPlatform.</summary>
    public class MTSpinner : MonoBehaviour
    {
        public float degreesPerSecond = 180f;

        void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
        }
    }
}
