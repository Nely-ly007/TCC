using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Plataforma móvel sincronizada com a música: vai e volta entre a posição inicial e Offset B,
    /// levando "Beats Per Half Cycle" batidas por trajeto. Leva o Player junto quando ele está em cima.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MTMovingPlatform : MonoBehaviour
    {
        public Vector2 offsetB = new Vector2(4f, 0f);
        public int beatsPerHalfCycle = 4;
        public float phaseOffsetBeats = 0f;
        public bool carryRiders = true;

        Rigidbody2D rb;
        Vector2 origin;
        readonly List<Transform> riders = new List<Transform>();

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            origin = rb.position;
        }

        double Beats()
        {
            MTBeatClock c = MTBeatClock.Instance;
            if (c != null && c.Running) return c.BeatPosition;
            return Time.timeAsDouble / (60.0 / 64.0);
        }

        void FixedUpdate()
        {
            float cycle = Mathf.Max(1, beatsPerHalfCycle);
            float t = (float)((Beats() + phaseOffsetBeats) / cycle);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(t, 1f));
            Vector2 target = origin + offsetB * k;
            Vector2 delta = target - rb.position;
            rb.MovePosition(target);

            if (carryRiders)
            {
                for (int i = riders.Count - 1; i >= 0; i--)
                {
                    if (riders[i] == null) { riders.RemoveAt(i); continue; }
                    riders[i].position += (Vector3)delta;
                }
            }
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (!carryRiders) return;
            if (c.collider.GetComponentInParent<MTPlayerHook>() == null) return;
            if (c.collider.bounds.min.y >= c.otherCollider.bounds.max.y - 0.2f)
            {
                Transform t = c.collider.GetComponentInParent<MTPlayerHook>().transform;
                if (!riders.Contains(t)) riders.Add(t);
            }
        }

        void OnCollisionExit2D(Collision2D c)
        {
            MTPlayerHook hook = c.collider.GetComponentInParent<MTPlayerHook>();
            if (hook != null) riders.Remove(hook.transform);
        }
    }
}
