using UnityEngine;
using UnityEngine.Tilemaps;

namespace PopAdventure.Hive
{
    /// <summary>Faz um Tilemap pulsar de cor no beat ("elementos do cenário piscam no ritmo").</summary>
    [RequireComponent(typeof(Tilemap))]
    public class HiveTilemapPulse : MonoBehaviour
    {
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color pulseColor = new Color(1f, 0.82f, 0.35f, 1f);
        [SerializeField, Range(0f, 1f)] private float intensity = 0.3f;
        [SerializeField, Tooltip("Só pulsa na batida 1 do compasso (mais sutil).")]
        private bool downbeatBoost = true;

        private Tilemap tm;

        private void Awake() { tm = GetComponent<Tilemap>(); }

        private void Update()
        {
            if (tm == null) return;
            float p = HiveBeatDriver.Pulse;
            if (downbeatBoost && HiveBeatDriver.Running)
            {
                int inBar = HiveMath.Mod(HiveBeatDriver.CurrentBeat, HiveBeatDriver.BeatsPerBar);
                if (inBar == 0) p *= 1.5f;
            }
            tm.color = Color.Lerp(baseColor, pulseColor, Mathf.Clamp01(p * intensity));
        }
    }
}
