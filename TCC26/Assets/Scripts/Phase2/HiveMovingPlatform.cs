using System;
using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Plataforma que vai de A até B e volta, com a posição calculada a partir da batida da música
    /// (SongBeat) — nunca sai de sincronia. Carrega o jogador (aplica o deslocamento a quem está em cima).
    /// </summary>
    public class HiveMovingPlatform : MonoBehaviour
    {
        private Rigidbody2D rb;
        private BoxCollider2D col;
        private Vector2 a, b;
        private float beatsPerLeg = 4f;
        private int offset;
        private bool ready;
        private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();

        public void Init(Vector2 size, Vector2 travel, int beatsLeg, int beatOffset)
        {
            a = transform.position;
            b = a + travel;
            beatsPerLeg = Mathf.Max(1, beatsLeg);
            offset = beatOffset;

            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.useFullKinematicContacts = false;

            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = size;
            HivePhysics.MakeOneWay(gameObject, col);

            var v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Block(size.x, size.y, HiveBlockStyle.Gold);
            sr.sortingOrder = 2;

            ready = true;
        }

        private Vector2 Evaluate()
        {
            double sb = HiveBeatDriver.Running ? HiveBeatDriver.SongBeat : 0.0;
            if (sb < 0.0) sb = 0.0;
            double t = (sb + offset) / beatsPerLeg;
            float tt = (float)(t % 2.0);
            if (tt < 0f) tt += 2f;
            float pp = tt <= 1f ? tt : 2f - tt;
            return Vector2.Lerp(a, b, HiveMath.SmoothStep01(pp));
        }

        private void FixedUpdate()
        {
            if (!ready) return;

            Vector2 target = Evaluate();
            Vector2 delta = target - rb.position;
            rb.MovePosition(target);

            riders.RemoveWhere(IsDead);
            foreach (var r in riders)
            {
                Vector2 d = new Vector2(delta.x, delta.y < 0f ? delta.y : 0f);
                r.position += d;
            }
        }

        private static bool IsDead(Rigidbody2D r)
        {
            return r == null;
        }

        private void OnCollisionEnter2D(Collision2D c) { Track(c); }
        private void OnCollisionStay2D(Collision2D c) { Track(c); }

        private void OnCollisionExit2D(Collision2D c)
        {
            if (c.rigidbody != null) riders.Remove(c.rigidbody);
        }

        private void Track(Collision2D c)
        {
            Rigidbody2D r = c.rigidbody;
            if (r == null || !HiveBridge.IsPlayer(c.collider)) return;
            bool above = c.collider.bounds.min.y >= col.bounds.max.y - 0.25f;
            if (above) riders.Add(r); else riders.Remove(r);
        }
    }
}
