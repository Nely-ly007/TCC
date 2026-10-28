using System;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Plataforma que liga/desliga no ritmo. pattern: '1' = sólida, qualquer outro = some; 1 caractere = StepBeats batidas
    /// (padrão 2 = meio tempo; HiveLevelBootstrap ▸ Beat Platform Step. 1 = 1 caractere por batida, o modo difícil).
    /// Telegrafia: pisca na segunda metade da batida antes de sumir; fantasma brilha antes de aparecer.
    /// É "one-way": dá para pular de baixo para cima, então nunca prende o jogador.
    /// </summary>
    public class HiveBeatPlatform : MonoBehaviour
    {
        /// <summary>Batidas por caractere do pattern. Definido pelo HiveLevelBootstrap.</summary>
        public static int StepBeats = 2;

        private BoxCollider2D col;
        private SpriteRenderer sr;
        private Transform visual;
        private bool[] pat = { true };
        private int offset;
        private bool solid = true;
        private bool ready;
        private float pop;
        private float nextSfx;

        public void Init(Vector2 size, string pattern, int beatOffset, int tier)
        {
            offset = beatOffset;
            if (string.IsNullOrEmpty(pattern)) pattern = "1";
            int n = Mathf.Min(pattern.Length, 32);
            pat = new bool[n];
            for (int i = 0; i < n; i++) pat[i] = pattern[i] == '1';

            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = size;
            HivePhysics.MakeOneWay(gameObject, col);

            var v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            visual = v.transform;
            sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Block(size.x, size.y, tier == 0 ? HiveBlockStyle.Platform : HiveBlockStyle.Wax);
            sr.sortingOrder = 2;
            ready = true;

            solid = StateAt(0);
            col.enabled = solid;
            ApplyVisual(solid, solid, 0f);
        }

        private bool StateAt(long beatIndex)
        {
            return pat[HiveMath.Mod((int)((beatIndex + offset) % 100000), pat.Length)];
        }

        private void Update()
        {
            if (!ready) return;

            double sb = HiveBeatDriver.Running ? HiveBeatDriver.SongBeat : 0.0;
            sb /= Math.Max(1, StepBeats);
            long idx = (long)Math.Floor(sb);
            if (idx < 0) idx = 0;
            float frac = sb < 0.0 ? 0f : HiveMath.Frac(sb);

            bool cur = StateAt(idx);
            bool next = StateAt(idx + 1);

            if (cur != solid)
            {
                solid = cur;
                col.enabled = solid;
                if (solid)
                {
                    pop = 1f;
                    var p = HiveBridge.Player;
                    if (Time.time >= nextSfx && p != null &&
                        Vector2.Distance(p.transform.position, transform.position) < 12f)
                    {
                        HiveSfx.Play(HiveSfxId.Appear, 0.3f, 1f);
                        nextSfx = Time.time + 0.08f;
                    }
                }
            }

            pop = Mathf.MoveTowards(pop, 0f, Time.deltaTime * 6f);
            ApplyVisual(cur, next, frac);
        }

        private void ApplyVisual(bool cur, bool next, float frac)
        {
            float a;
            if (cur)
            {
                a = 1f;
                if (!next && frac > 0.55f)
                {
                    float k = (frac - 0.55f) / 0.45f;
                    a = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(k * 32f));
                }
            }
            else
            {
                a = 0.16f;
                if (next && frac > 0.6f) a = Mathf.Lerp(0.16f, 0.65f, (frac - 0.6f) / 0.4f);
            }
            sr.color = new Color(1f, 1f, 1f, a);
            float s = 1f + 0.14f * pop;
            visual.localScale = new Vector3(s, s, 1f);
        }
    }
}