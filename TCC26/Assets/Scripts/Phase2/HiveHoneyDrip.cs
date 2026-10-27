using System;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Goteira no teto. A gota incha durante as batidas e se solta na batida 0 do ciclo
    /// (a cada "everyBeats" batidas, deslocado por "offset"). Telegrafia visual + sonora.
    /// </summary>
    public class HiveHoneyDrip : MonoBehaviour
    {
        private int every = 4;
        private int offset;
        private Transform bulge;
        private SpriteRenderer bulgeSr;
        private bool ready;

        public void Init(int everyBeats, int beatOffset)
        {
            every = Mathf.Max(1, everyBeats);
            offset = beatOffset;

            // pequeno bocal no teto
            var cap = new GameObject("Cap");
            cap.transform.SetParent(transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            cap.transform.localScale = new Vector3(0.7f, 0.22f, 1f);
            var csr = cap.AddComponent<SpriteRenderer>();
            csr.sprite = HiveSpriteFactory.White();
            csr.color = HivePalette.Black;
            csr.sortingOrder = 4;

            var b = new GameObject("Bulge");
            b.transform.SetParent(transform, false);
            bulge = b.transform;
            bulgeSr = b.AddComponent<SpriteRenderer>();
            bulgeSr.sprite = HiveSpriteFactory.Drop();
            bulgeSr.sortingOrder = 4;
            ready = true;
        }

        private void OnEnable() { HiveBeatDriver.OnBeat += HandleBeat; }
        private void OnDisable() { HiveBeatDriver.OnBeat -= HandleBeat; }

        private void HandleBeat(int beat, int inBar)
        {
            if (!ready) return;
            if (HiveMath.Mod(beat + offset, every) != 0) return;
            HiveHoneyDrop.Create((Vector2)transform.position + Vector2.down * 0.45f, 9f, 15);
        }

        private void Update()
        {
            if (!ready) return;
            double sb = HiveBeatDriver.Running ? Math.Max(0.0, HiveBeatDriver.SongBeat) : 0.0;
            long idx = (long)Math.Floor(sb);
            float phase = (HiveMath.Mod((int)(idx % 100000) + offset, every) + HiveMath.Frac(sb)) / every;

            float s = Mathf.Lerp(0.25f, 0.9f, phase * phase);
            bulge.localScale = new Vector3(s, s, 1f);
            bulge.localPosition = new Vector3(0f, -0.28f * s, 0f);

            float warn = Mathf.Clamp01((phase - 0.7f) / 0.3f);
            bulgeSr.color = Color.Lerp(Color.white, HivePalette.Pink, warn * (0.5f + 0.5f * Mathf.Sin(Time.time * 40f)));
        }
    }
}
