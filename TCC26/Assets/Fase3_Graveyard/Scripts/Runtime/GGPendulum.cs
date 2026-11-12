using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Pêndulo no ritmo (bola de espinhos): o "arm" balança e uma corrente liga o pivô à bola.
    /// Hierarquia: raiz (pivô) → Arm (este script gira o Arm) → Ball (GGHazard + collider).
    /// </summary>
    public class GGPendulum : MonoBehaviour
    {
        [SerializeField] Transform arm;
        [SerializeField] Transform ball;
        [SerializeField] LineRenderer chain;
        [SerializeField] float maxAngle = 55f;
        [SerializeField] float periodBeats = 4f;
        [SerializeField] float offsetBeats = 0f;

        public void Configure(float newMaxAngle, float newPeriodBeats, float newOffsetBeats)
        {
            maxAngle = newMaxAngle;
            periodBeats = newPeriodBeats;
            offsetBeats = newOffsetBeats;
        }

        void Awake()
        {
            if (chain != null)
            {
                if (chain.sharedMaterial == null)
                    chain.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                chain.useWorldSpace = true;
                chain.positionCount = 2;
            }
        }

        void Update()
        {
            if (arm == null) return;
            float t = (float)GGBeatClock.Mod((GGBeatClock.Position - offsetBeats) / Mathf.Max(0.5f, periodBeats), 1.0);
            arm.localRotation = Quaternion.Euler(0f, 0f, maxAngle * Mathf.Sin(t * Mathf.PI * 2f));
        }

        void LateUpdate()
        {
            if (chain != null && arm != null && ball != null)
            {
                chain.SetPosition(0, arm.position);
                chain.SetPosition(1, ball.position);
            }
        }
    }
}
