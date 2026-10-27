using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Cenário de fundo da Fase 2, todo gerado em código (nenhuma imagem):
    /// céu âmbar-preto → colmeia gigante em 2 camadas de hexágonos com parallax, ondas de brilho no ritmo,
    /// e partículas de mel flutuando. Pulsa no beat ("elementos do cenário piscam na música").
    /// Fica atrás de todos os tilemaps (sortingOrder -100 .. -60).
    /// </summary>
    public class HiveBackdrop : MonoBehaviour
    {
        private sealed class Layer
        {
            public SpriteRenderer[] sr;
            public Vector2[] local;
            public float[] phase;
            public float parallax, w, h, pulse;
            public Color baseColor, pulseColor;
        }

        private Camera cam;
        private Transform skyRoot;
        private SpriteRenderer sky, glow;
        private readonly List<Layer> layers = new List<Layer>();
        private Transform[] motes;
        private Vector2[] moteVel;
        private float[] moteSeed;
        private bool built;

        public static HiveBackdrop Create()
        {
            var go = new GameObject("HiveBackdrop");
            return go.AddComponent<HiveBackdrop>();
        }

        private void LateUpdate()
        {
            if (!built)
            {
                cam = Camera.main;
                if (cam == null) return;
                Build();
            }
            if (cam == null) return;
            UpdateAll();
        }

        private void Build()
        {
            built = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HivePalette.Black;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float viewW = halfW * 2f;
            float viewH = halfH * 2f;

            // céu
            skyRoot = new GameObject("Sky").transform;
            skyRoot.SetParent(transform, false);
            sky = skyRoot.gameObject.AddComponent<SpriteRenderer>();
            sky.sprite = HiveSpriteFactory.White();
            sky.color = new Color32(22, 14, 6, 255);
            sky.sortingOrder = -100;

            var g = new GameObject("SkyGlow");
            g.transform.SetParent(skyRoot, false);
            glow = g.AddComponent<SpriteRenderer>();
            glow.sprite = HiveSpriteFactory.Soft();
            glow.color = new Color(HivePalette.Amber.r, HivePalette.Amber.g, HivePalette.Amber.b, 0.35f);
            glow.sortingOrder = -99;

            // colmeia gigante (2 camadas)
            AddHexLayer(HiveSpriteFactory.HexOutline(4f), 3.4f, 0.12f, new Color(0.85f, 0.5f, 0f, 0.14f),
                        new Color(1f, 0.78f, 0.2f, 0.34f), -90, viewW, viewH);
            AddHexLayer(HiveSpriteFactory.HexOutline(3f), 2.0f, 0.30f, new Color(0.9f, 0.55f, 0.05f, 0.18f),
                        new Color(1f, 0.85f, 0.35f, 0.5f), -80, viewW, viewH);
            AddHexLayer(HiveSpriteFactory.HexFill(), 2.0f, 0.30f, new Color(1f, 0.7f, 0.1f, 0.0f),
                        new Color(1f, 0.75f, 0.15f, 0.16f), -85, viewW, viewH);

            // partículas de mel
            int n = 36;
            motes = new Transform[n];
            moteVel = new Vector2[n];
            moteSeed = new float[n];
            Vector2 c = cam.transform.position;
            for (int i = 0; i < n; i++)
            {
                var m = new GameObject("Mote");
                m.transform.SetParent(transform, false);
                var sr = m.AddComponent<SpriteRenderer>();
                sr.sprite = HiveSpriteFactory.Dot();
                sr.color = new Color(1f, 0.82f, 0.3f, Random.Range(0.25f, 0.6f));
                sr.sortingOrder = -60;
                float s = Random.Range(0.06f, 0.2f);
                m.transform.localScale = new Vector3(s, s, 1f);
                m.transform.position = c + new Vector2(Random.Range(-halfW * 1.2f, halfW * 1.2f), Random.Range(-halfH * 1.2f, halfH * 1.2f));
                motes[i] = m.transform;
                moteVel[i] = new Vector2(0f, Random.Range(0.3f, 0.9f));
                moteSeed[i] = Random.value * 10f;
            }
        }

        private void AddHexLayer(Sprite sprite, float size, float parallax, Color baseColor, Color pulseColor,
                                 int order, float viewW, float viewH)
        {
            int cols = Mathf.CeilToInt(viewW * 1.7f / (0.75f * size));
            if (cols % 2 == 1) cols++;
            int rows = Mathf.CeilToInt(viewH * 1.7f / (0.8660254f * size));

            var l = new Layer();
            l.parallax = parallax;
            l.w = cols * 0.75f * size;
            l.h = rows * 0.8660254f * size;
            l.baseColor = baseColor;
            l.pulseColor = pulseColor;
            l.sr = new SpriteRenderer[cols * rows];
            l.local = new Vector2[cols * rows];
            l.phase = new float[cols * rows];

            var root = new GameObject("HexLayer" + order).transform;
            root.SetParent(transform, false);

            int k = 0;
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                {
                    var t = new GameObject("h");
                    t.transform.SetParent(root, false);
                    t.transform.localScale = new Vector3(size, size, 1f);
                    var sr = t.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = order;
                    l.sr[k] = sr;
                    l.local[k] = new Vector2(i * 0.75f * size, j * 0.8660254f * size + (i % 2) * 0.4330127f * size);
                    l.phase[k] = (l.local[k].x + l.local[k].y) * 0.035f + Random.value * 0.4f;
                    k++;
                }
            layers.Add(l);
        }

        private void UpdateAll()
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 cp = cam.transform.position;
            Vector2 c = new Vector2(cp.x, cp.y);

            // céu acompanha a câmera
            skyRoot.position = new Vector3(c.x, c.y, 0f);
            skyRoot.localScale = new Vector3(halfW * 2f + 4f, halfH * 2f + 4f, 1f);
            // brilho do fundo (a escala do pai distorce; compensa)
            glow.transform.localScale = new Vector3(1.6f, 1.2f, 1f);
            glow.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            float p0 = HiveBeatDriver.Pulse;
            glow.color = new Color(HivePalette.Amber.r, HivePalette.Amber.g, HivePalette.Amber.b, 0.30f + 0.18f * p0);

            double sb = HiveBeatDriver.Running ? HiveBeatDriver.SongBeat : Time.timeAsDouble * 1.8;

            for (int li = 0; li < layers.Count; li++)
            {
                var l = layers[li];
                for (int i = 0; i < l.sr.Length; i++)
                {
                    float x = HiveMath.Mod(l.local[i].x - c.x * l.parallax + l.w * 0.5f, l.w) - l.w * 0.5f;
                    float y = HiveMath.Mod(l.local[i].y - c.y * l.parallax + l.h * 0.5f, l.h) - l.h * 0.5f;
                    l.sr[i].transform.position = new Vector3(c.x + x, c.y + y, 0f);

                    float k = Mathf.Pow(1f - HiveMath.Frac(sb - l.phase[i]), 3f);
                    l.sr[i].color = Color.Lerp(l.baseColor, l.pulseColor, k);
                }
            }

            // partículas de mel
            float dt = Time.deltaTime;
            for (int i = 0; i < motes.Length; i++)
            {
                Vector3 mp = motes[i].position;
                mp.y += moteVel[i].y * dt;
                mp.x += Mathf.Sin(Time.time * 0.8f + moteSeed[i]) * 0.25f * dt;
                if (mp.y > c.y + halfH * 1.2f) { mp.y -= halfH * 2.4f; mp.x = c.x + Random.Range(-halfW * 1.2f, halfW * 1.2f); }
                if (mp.x > c.x + halfW * 1.3f) mp.x -= halfW * 2.6f;
                if (mp.x < c.x - halfW * 1.3f) mp.x += halfW * 2.6f;
                if (mp.y < c.y - halfH * 1.3f) mp.y += halfH * 2.6f;
                motes[i].position = mp;
            }
        }
    }
}
