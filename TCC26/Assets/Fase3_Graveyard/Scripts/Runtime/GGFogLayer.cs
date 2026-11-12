using System;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>Névoa: 3 cópias lado a lado que derivam devagar e se repetem para sempre.</summary>
    public class GGFogLayer : MonoBehaviour
    {
        [SerializeField] Transform[] tiles;
        [SerializeField] float tileWidth = 20f;
        [SerializeField] float parallax = 0.5f;
        [SerializeField] float driftSpeed = 0.35f;
        [SerializeField] float yOffsetFromCamera = -3f;

        Transform cam;

        public void Setup(Transform[] newTiles, float width, float newParallax, float drift, float yOffset)
        {
            tiles = newTiles;
            tileWidth = width;
            parallax = newParallax;
            driftSpeed = drift;
            yOffsetFromCamera = yOffset;
        }

        void LateUpdate()
        {
            if (cam == null)
            {
                if (Camera.main == null) return;
                cam = Camera.main.transform;
            }
            if (tiles == null || tiles.Length == 0 || tileWidth <= 0.01f) return;

            float camX = cam.position.x;
            float origin = camX * parallax + Time.time * driftSpeed;
            float shift = Mathf.Round((camX - origin) / tileWidth) * tileWidth;
            origin += shift;
            int n = tiles.Length;
            for (int i = 0; i < n; i++)
            {
                if (tiles[i] == null) continue;
                float x = origin + (i - (n - 1) * 0.5f) * tileWidth;
                tiles[i].position = new Vector3(x, cam.position.y + yOffsetFromCamera, tiles[i].position.z);
            }
        }
    }
}
