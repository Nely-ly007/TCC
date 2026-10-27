using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Poça de mel: zona trigger que limita a velocidade horizontal e o impulso de pulo do Player.</summary>
    public class HiveHoneyPool : MonoBehaviour
    {
        [SerializeField] private float maxSpeedX = 2.4f;   // velocidade normal do Player = 5
        [SerializeField] private float maxUpSpeed = 9f;    // reduz a altura do pulo

        private Vector2 size;
        private Transform surface;
        private SpriteRenderer body;
        private bool ready;

        public void Init(Vector2 poolSize)
        {
            size = poolSize;

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            var b = new GameObject("Body");
            b.transform.SetParent(transform, false);
            b.transform.localScale = new Vector3(size.x, size.y, 1f);
            body = b.AddComponent<SpriteRenderer>();
            body.sprite = HiveSpriteFactory.White();
            body.color = new Color(HivePalette.Honey.r, HivePalette.Honey.g, HivePalette.Honey.b, 0.72f);
            body.sortingOrder = 6;

            var s = new GameObject("Surface");
            s.transform.SetParent(transform, false);
            s.transform.localScale = new Vector3(size.x, 0.12f, 1f);
            surface = s.transform;
            var sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.White();
            sr.color = new Color(HivePalette.Cream.r, HivePalette.Cream.g, HivePalette.Cream.b, 0.9f);
            sr.sortingOrder = 7;

            ready = true;
        }

        private void OnEnable() { HiveBeatDriver.OnBeat += HandleBeat; }
        private void OnDisable() { HiveBeatDriver.OnBeat -= HandleBeat; }

        private void HandleBeat(int beat, int inBar)
        {
            if (!ready || inBar % 2 != 0) return;
            Vector2 p = (Vector2)transform.position +
                        new Vector2(Random.Range(-size.x * 0.45f, size.x * 0.45f), size.y * 0.5f - 0.1f);
            HiveFx.Burst(p, new Color(1f, 0.85f, 0.35f, 0.9f), 2, 1.1f, 0.8f, 0.14f, -1.5f, HiveSpriteFactory.Dot());
        }

        private void Update()
        {
            if (!ready) return;
            double sb = HiveBeatDriver.Running ? HiveBeatDriver.SongBeat : 0.0;
            float wob = Mathf.Sin((float)(sb * Mathf.PI)) * 0.03f;
            surface.localPosition = new Vector3(0f, size.y * 0.5f - 0.06f + wob, 0f);
            float p = HiveBeatDriver.Pulse * 0.12f;
            body.color = new Color(HivePalette.Honey.r + p, HivePalette.Honey.g + p, HivePalette.Honey.b, 0.72f);
        }

        private void OnTriggerStay2D(Collider2D o)
        {
            if (!HiveBridge.IsPlayer(o)) return;
            var st = HivePlayerStatus.For(HiveBridge.Player);
            if (st != null) st.Honey(maxSpeedX, maxUpSpeed);
        }
    }
}
