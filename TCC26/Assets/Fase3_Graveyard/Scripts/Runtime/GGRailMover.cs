using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Move o objeto em vai-e-volta, sincronizado com a música (um ciclo completo = beatsPerCycle batidas).
    /// Usado em plataformas suspensas, serras e plataformas de espinhos.
    /// Com Rigidbody2D (Kinematic) o movimento é físico e o Player pega carona (se houver GGRiderTracker).
    /// </summary>
    public class GGRailMover : MonoBehaviour
    {
        [SerializeField] Vector2 travel = new Vector2(4f, 0f);
        [SerializeField] float beatsPerCycle = 8f;
        [SerializeField] float offsetBeats = 0f;
        [SerializeField] bool smooth = true;

        Vector2 origin;
        Rigidbody2D rb;
        GGRiderTracker tracker;

        public void Configure(Vector2 newTravel, float newBeatsPerCycle, float newOffsetBeats)
        {
            travel = newTravel;
            beatsPerCycle = newBeatsPerCycle;
            offsetBeats = newOffsetBeats;
        }

        void Awake()
        {
            origin = transform.position;
            rb = GetComponent<Rigidbody2D>();
            tracker = GetComponent<GGRiderTracker>();
        }

        Vector2 Evaluate()
        {
            double ph = GGBeatClock.Mod((GGBeatClock.Position - offsetBeats) / Mathf.Max(0.1f, beatsPerCycle), 1.0);
            float t = (float)(ph < 0.5 ? ph * 2.0 : (1.0 - ph) * 2.0);
            if (smooth) t = Mathf.SmoothStep(0f, 1f, t);
            return origin + travel * t;
        }

        void FixedUpdate()
        {
            Vector2 target = Evaluate();
            Vector2 current = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 delta = target - current;
            if (rb != null) rb.MovePosition(target);
            else transform.position = new Vector3(target.x, target.y, transform.position.z);
            if (tracker != null) tracker.Carry(delta);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 o = Application.isPlaying ? (Vector3)origin : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(o, o + (Vector3)travel);
        }
    }
}
