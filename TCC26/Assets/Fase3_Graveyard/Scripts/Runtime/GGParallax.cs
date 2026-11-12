using System;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>Paralaxe: acompanha a câmera por uma fração (0 = fica parado no mundo, 1 = fixo na tela).</summary>
    public class GGParallax : MonoBehaviour
    {
        [SerializeField] Vector2 factor = new Vector2(0.8f, 0.8f);

        Transform cam;
        Vector3 camStart;
        Vector3 startPos;

        void LateUpdate()
        {
            if (cam == null)
            {
                if (Camera.main == null) return;
                cam = Camera.main.transform;
                camStart = cam.position;
                startPos = transform.position;
            }
            Vector3 d = cam.position - camStart;
            transform.position = new Vector3(startPos.x + d.x * factor.x, startPos.y + d.y * factor.y, startPos.z);
        }
    }
}
