using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Espinhos rítmicos: avisam (tremem e sobem um pouco) 1 batida antes, saem no beat e recolhem.
    /// Hierarquia: raiz (collider trigger) → Visual (sprite). Posicione a raiz na superfície do chão.
    /// </summary>
    public class GGSpikeTrap : GGHazard
    {
        [SerializeField] Transform visual;
        [SerializeField] int periodBeats = 4;
        [SerializeField] int activeBeats = 1;
        [SerializeField] float offsetBeats = 0f;
        [SerializeField] float retractDistance = 0.9f;

        float extension;

        public override bool IsDangerous { get { return extension > 0.7f; } }

        public void Configure(int newPeriod, int newActive, float newOffset)
        {
            periodBeats = Mathf.Max(2, newPeriod);
            activeBeats = Mathf.Clamp(newActive, 1, periodBeats - 1);
            offsetBeats = newOffset;
        }

        void Update()
        {
            if (visual == null) return;
            float cycle = (float)GGBeatClock.Mod(GGBeatClock.Position - offsetBeats, periodBeats);
            float activeStart = periodBeats - activeBeats;
            float target = 0f;
            bool telegraph = false;
            if (cycle >= activeStart) target = 1f;
            else if (cycle >= activeStart - 1f) { target = 0.22f; telegraph = true; }

            float speed = target > extension ? 14f : 5f;
            extension = Mathf.MoveTowards(extension, target, speed * Time.deltaTime);
            float shake = telegraph ? Mathf.Sin(Time.time * 60f) * 0.025f : 0f;
            Vector3 p = visual.localPosition;
            p.x = shake;
            p.y = Mathf.Lerp(-retractDistance, 0f, extension);
            visual.localPosition = p;
        }
    }
}
