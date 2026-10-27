using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Lanterna de checkpoint. Ao tocar: vira o ponto de retorno (morte/abismo) e acende.</summary>
    public class HiveCheckpoint : MonoBehaviour
    {
        private SpriteRenderer lamp;
        private SpriteRenderer glow;
        private bool visited;
        private bool current;

        public Vector2 RespawnPoint { get { return (Vector2)transform.position + Vector2.up * 0.6f; } }

        public void Init()
        {
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 2.6f);
            col.offset = new Vector2(0f, 1.1f);

            var g = new GameObject("Glow");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            g.transform.localScale = Vector3.one * 2.4f;
            glow = g.AddComponent<SpriteRenderer>();
            glow.sprite = HiveSpriteFactory.Soft();
            glow.color = new Color(HivePalette.Pink.r, HivePalette.Pink.g, HivePalette.Pink.b, 0f);
            glow.sortingOrder = 3;

            var v = new GameObject("Lantern");
            v.transform.SetParent(transform, false);
            lamp = v.AddComponent<SpriteRenderer>();
            lamp.sprite = HiveSpriteFactory.Lantern(false);
            lamp.sortingOrder = 4;
        }

        public void SetCurrent(bool value)
        {
            current = value;
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (visited || !HiveBridge.IsPlayer(o)) return;
            visited = true;
            lamp.sprite = HiveSpriteFactory.Lantern(true);

            Vector2 top = (Vector2)transform.position + Vector2.up * 1.1f;
            HiveFx.Burst(top, HivePalette.Gold, 16, 5f, 0.7f, 0.2f, 2f);
            HiveFx.Ring(top, HivePalette.Pink, 2.2f, 0.5f);
            HiveSfx.Play(HiveSfxId.Checkpoint, 0.6f, 1f);
            HiveOverlay.Toast("CHECKPOINT", HivePalette.Pink, 1.4f);

            var b = HiveLevelBootstrap.Instance;
            if (b != null) b.SetCheckpoint(this);
        }

        private void Update()
        {
            if (glow == null) return;
            float a = 0f;
            if (visited) a = current ? 0.25f + 0.35f * HiveBeatDriver.Pulse : 0.12f;
            Color c = HivePalette.Pink;
            glow.color = new Color(c.r, c.g, c.b, a);
        }
    }
}
