using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Portal de saída. Fica selado (cinza) enquanto a Rainha não for derrotada, se a fase tiver arena.</summary>
    public class HiveLevelExit : MonoBehaviour
    {
        private SpriteRenderer ring;
        private SpriteRenderer glow;
        private float nextHint;
        private bool used;

        public void Init()
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.9f;

            var g = new GameObject("Glow");
            g.transform.SetParent(transform, false);
            g.transform.localScale = Vector3.one * 4f;
            glow = g.AddComponent<SpriteRenderer>();
            glow.sprite = HiveSpriteFactory.Soft();
            glow.sortingOrder = 3;

            var r = new GameObject("Ring");
            r.transform.SetParent(transform, false);
            ring = r.AddComponent<SpriteRenderer>();
            ring.sprite = HiveSpriteFactory.Ring();
            ring.sortingOrder = 4;
        }

        private bool IsOpen
        {
            get
            {
                var b = HiveLevelBootstrap.Instance;
                return b == null || b.CanExit;
            }
        }

        private void Update()
        {
            if (ring == null) return;
            bool open = IsOpen;
            ring.transform.Rotate(0f, 0f, (open ? 40f : 6f) * Time.deltaTime);
            float s = 1f + (open ? 0.08f : 0.02f) * HiveBeatDriver.Pulse;
            ring.transform.localScale = Vector3.one * s;
            ring.color = open ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.8f);
            Color gc = open ? HivePalette.Gold : new Color(0.3f, 0.3f, 0.3f, 1f);
            glow.color = new Color(gc.r, gc.g, gc.b, open ? 0.35f : 0.12f);
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (used || !HiveBridge.IsPlayer(o)) return;
            var b = HiveLevelBootstrap.Instance;
            if (b == null) return;

            if (!b.CanExit)
            {
                if (Time.time >= nextHint)
                {
                    nextHint = Time.time + 2f;
                    HiveOverlay.Toast("SAÍDA SELADA — derrote a Rainha", HivePalette.Honey, 1.8f);
                    HiveSfx.Play(HiveSfxId.Warn, 0.4f, 0.8f);
                }
                return;
            }
            used = true;
            b.CompleteLevel();
        }
    }
}
