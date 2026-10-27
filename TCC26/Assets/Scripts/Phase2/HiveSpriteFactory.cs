using System;
using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Hive
{
    public enum HiveBlockStyle { Ground, Platform, Gold, Wax }

    /// <summary>
    /// Gera TODOS os sprites da Fase 2 em código (Texture2D + Sprite.Create).
    /// Nada de imagem para importar. Só a Tile Palette é desenhada por você.
    /// Cache por chave. Convenção: PPU 32 (1 unidade = 32 px) salvo indicação.
    /// </summary>
    public static class HiveSpriteFactory
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        private static Sprite Get(string key, Func<Sprite> make)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            s = make();
            s.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (s.texture != null) s.texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            cache[key] = s;
            return s;
        }

        // ============================================================ Canvas

        private sealed class Canvas
        {
            public readonly int W, H;
            public readonly Color[] P;

            public Canvas(int w, int h)
            {
                W = w; H = h;
                P = new Color[w * h];
            }

            public void Blend(int x, int y, Color s)
            {
                if (x < 0 || y < 0 || x >= W || y >= H || s.a <= 0f) return;
                int i = y * W + x;
                Color d = P[i];
                float a = s.a + d.a * (1f - s.a);
                if (a <= 0f) return;
                P[i] = new Color(
                    (s.r * s.a + d.r * d.a * (1f - s.a)) / a,
                    (s.g * s.a + d.g * d.a * (1f - s.a)) / a,
                    (s.b * s.a + d.b * d.a * (1f - s.a)) / a,
                    a);
            }

            /// <summary>Preenche a forma definida por uma SDF (distância com sinal em pixels).</summary>
            public void Shape(Func<float, float, float> sd, Color c)
            {
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float d = sd(x + 0.5f, y + 0.5f);
                        float a = Mathf.Clamp01(0.5f - d) * c.a;
                        if (a > 0f) Blend(x, y, new Color(c.r, c.g, c.b, a));
                    }
            }

            /// <summary>Forma com contorno (outline cresce para fora).</summary>
            public void Outlined(Func<float, float, float> sd, Color fill, Color outline, float ow)
            {
                Shape((x, y) => sd(x, y) - ow, outline);
                Shape(sd, fill);
            }

            public Sprite ToSprite(float ppu, Vector2 pivot)
            {
                var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
                t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                t.SetPixels(P);
                t.Apply(false, false);
                return Sprite.Create(t, new Rect(0, 0, W, H), pivot, ppu);
            }
        }

        // ============================================================ SDFs

        private static float Len(float x, float y) { return Mathf.Sqrt(x * x + y * y); }

        private static float SdCircle(float px, float py, float cx, float cy, float r)
        {
            return Len(px - cx, py - cy) - r;
        }

        private static float SdEllipse(float px, float py, float cx, float cy, float rx, float ry)
        {
            float k = Len((px - cx) / rx, (py - cy) / ry);
            return (k - 1f) * Mathf.Min(rx, ry);
        }

        private static float SdRoundBox(float px, float py, float cx, float cy, float hw, float hh, float r)
        {
            r = Mathf.Min(r, Mathf.Min(hw, hh));
            float qx = Mathf.Abs(px - cx) - (hw - r);
            float qy = Mathf.Abs(py - cy) - (hh - r);
            return Len(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float SdSegment(float px, float py, float ax, float ay, float bx, float by, float r)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            return Len(pax - bax * h, pay - bay * h) - r;
        }

        /// <summary>Hexágono "flat-top" (pontas à esquerda/direita). apothem = distância centro→aresta.</summary>
        private static float SdHex(float px, float py, float cx, float cy, float apothem)
        {
            float x = Mathf.Abs(px - cx), y = Mathf.Abs(py - cy);
            const float kx = -0.866025404f, ky = 0.5f, kz = 0.577350269f;
            float d = kx * x + ky * y;
            float m = 2f * Mathf.Min(d, 0f);
            x -= m * kx; y -= m * ky;
            x -= Mathf.Clamp(x, -kz * apothem, kz * apothem);
            y -= apothem;
            float len = Len(x, y);
            return y >= 0f ? len : -len;
        }

        private static float SdPoly(float px, float py, Vector2[] v)
        {
            float dx = px - v[0].x, dy = py - v[0].y;
            float d = dx * dx + dy * dy;
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                float ex = v[j].x - v[i].x, ey = v[j].y - v[i].y;
                float wx = px - v[i].x, wy = py - v[i].y;
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float bx = wx - ex * t, by = wy - ey * t;
                d = Mathf.Min(d, bx * bx + by * by);
                bool c1 = py >= v[i].y, c2 = py < v[j].y, c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        private static Vector2[] Scaled(Vector2[] src, float cx, float cy, float k)
        {
            var r = new Vector2[src.Length];
            for (int i = 0; i < src.Length; i++)
                r[i] = new Vector2(cx + (src[i].x - cx) * k, cy + (src[i].y - cy) * k);
            return r;
        }

        // ============================================================ Simples

        /// <summary>Quadrado branco de 1x1 unidade (tinte pelo SpriteRenderer.color).</summary>
        public static Sprite White()
        {
            return Get("white", () =>
            {
                var c = new Canvas(8, 8);
                for (int i = 0; i < c.P.Length; i++) c.P[i] = Color.white;
                return c.ToSprite(8f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Brilho circular suave de 1x1 unidade.</summary>
        public static Sprite Soft()
        {
            return Get("soft", () =>
            {
                var c = new Canvas(64, 64);
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float r = Len(x + 0.5f - 32f, y + 0.5f - 32f) / 32f;
                        float a = Mathf.Clamp01(1f - r);
                        c.P[y * 64 + x] = new Color(1f, 1f, 1f, a * a);
                    }
                return c.ToSprite(64f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Círculo sólido branco de 1x1 unidade.</summary>
        public static Sprite Dot()
        {
            return Get("dot", () =>
            {
                var c = new Canvas(32, 32);
                c.Shape((x, y) => SdCircle(x, y, 16f, 16f, 15f), Color.white);
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Hexágono só de contorno (branco), 1 unidade de largura. Para cenário.</summary>
        public static Sprite HexOutline(float stroke = 3f)
        {
            return Get("hexo" + stroke, () =>
            {
                int w = 128, h = Mathf.RoundToInt(128f * 0.8660254f);
                var c = new Canvas(w, h);
                float ap = h * 0.5f - 1f;
                c.Shape((x, y) => Mathf.Abs(SdHex(x, y, w * 0.5f, h * 0.5f, ap)) - stroke * 0.5f, Color.white);
                return c.ToSprite(w, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Hexágono cheio (branco), 1 unidade de largura.</summary>
        public static Sprite HexFill()
        {
            return Get("hexf", () =>
            {
                int w = 128, h = Mathf.RoundToInt(128f * 0.8660254f);
                var c = new Canvas(w, h);
                float ap = h * 0.5f - 1f;
                c.Shape((x, y) => SdHex(x, y, w * 0.5f, h * 0.5f, ap), Color.white);
                return c.ToSprite(w, new Vector2(0.5f, 0.5f));
            });
        }

        // ============================================================ Blocos / plataformas

        private static void HexCell(float x, float y, float s, out float cx, out float cy, out int id)
        {
            float q = (2f / 3f * x) / s;
            float r = (-1f / 3f * x + 0.57735027f * y) / s;
            float fx = q, fz = r, fy = -fx - fz;
            float rx = Mathf.Round(fx), ry = Mathf.Round(fy), rz = Mathf.Round(fz);
            float dx = Mathf.Abs(rx - fx), dy = Mathf.Abs(ry - fy), dz = Mathf.Abs(rz - fz);
            if (dx > dy && dx > dz) rx = -ry - rz;
            else if (dy > dz) ry = -rx - rz;
            else rz = -rx - ry;
            cx = s * 1.5f * rx;
            cy = s * (0.8660254f * rx + 1.7320508f * rz);
            id = HiveMath.Mod((int)rx - (int)rz, 3);
        }

        /// <summary>Bloco com padrão de favo. Largura/altura em unidades (PPU 32). Pivô no centro.</summary>
        public static Sprite Block(float wUnits, float hUnits, HiveBlockStyle style)
        {
            int w = Mathf.Max(8, Mathf.RoundToInt(wUnits * 32f));
            int h = Mathf.Max(8, Mathf.RoundToInt(hUnits * 32f));
            string key = "block" + w + "x" + h + (int)style;
            return Get(key, () =>
            {
                var c = new Canvas(w, h);
                Color border, a, b, d, line, top;
                switch (style)
                {
                    case HiveBlockStyle.Platform:
                        border = HivePalette.Black; a = HivePalette.Honey; b = new Color32(255, 196, 40, 255);
                        d = new Color32(240, 150, 0, 255); line = HivePalette.DeepAmber; top = HivePalette.Cream;
                        break;
                    case HiveBlockStyle.Gold:
                        border = HivePalette.Black; a = HivePalette.Gold; b = new Color32(255, 228, 110, 255);
                        d = HivePalette.Honey; line = HivePalette.Amber; top = Color.white;
                        break;
                    case HiveBlockStyle.Wax:
                        border = HivePalette.Black; a = new Color32(70, 48, 22, 255); b = new Color32(84, 58, 26, 255);
                        d = new Color32(60, 40, 18, 255); line = HivePalette.Pink; top = HivePalette.Pink;
                        break;
                    default: // Ground
                        border = HivePalette.Black; a = HivePalette.Comb; b = new Color32(48, 33, 14, 255);
                        d = new Color32(30, 20, 9, 255); line = HivePalette.Amber; top = HivePalette.Honey;
                        break;
                }

                float cr = (w < 24 || h < 24) ? 3f : 5f;
                float hs = 11f; // raio do hexágono do padrão
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float px = x + 0.5f, py = y + 0.5f;
                        float sd = SdRoundBox(px, py, w * 0.5f, h * 0.5f, w * 0.5f, h * 0.5f, cr);
                        float cov = Mathf.Clamp01(0.5f - sd);
                        if (cov <= 0f) continue;

                        Color col;
                        if (sd > -2.2f) col = border;
                        else
                        {
                            float ccx, ccy; int id;
                            HexCell(px, py, hs, out ccx, out ccy, out id);
                            float hd = SdHex(px, py, ccx, ccy, hs * 0.8660254f);
                            col = id == 0 ? a : (id == 1 ? b : d);
                            if (hd > -1.3f) col = Color.Lerp(col, line, 0.85f);
                            if (py > h - 6.5f) col = Color.Lerp(col, top, 0.85f);      // fio de luz no topo
                            else if (py < 5f && style != HiveBlockStyle.Wax) col = Color.Lerp(col, Color.black, 0.18f);
                        }
                        c.Blend(x, y, new Color(col.r, col.g, col.b, cov));
                    }
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Fileira de espinhos de cera. Pivô na base-centro.</summary>
        public static Sprite Thorns(float wUnits)
        {
            int teeth = Mathf.Max(1, Mathf.RoundToInt(wUnits * 2f));
            return Get("thorns" + teeth, () =>
            {
                int tw = 16, th = 20;
                var c = new Canvas(teeth * tw, th);
                for (int y = 0; y < th; y++)
                    for (int x = 0; x < teeth * tw; x++)
                    {
                        float px = x + 0.5f, py = y + 0.5f;
                        int ti = Mathf.Min(teeth - 1, x / tw);
                        float lx = px - ti * tw;
                        var tri = new[] { new Vector2(0.5f, 0f), new Vector2(tw - 0.5f, 0f), new Vector2(tw * 0.5f, th - 0.5f) };
                        float sd = SdPoly(lx, py, tri);
                        float cov = Mathf.Clamp01(0.5f - sd);
                        if (cov <= 0f) continue;
                        Color col = sd > -1.8f ? HivePalette.Black : Color.Lerp(HivePalette.Amber, HivePalette.Gold, py / th);
                        c.Blend(x, y, new Color(col.r, col.g, col.b, cov));
                    }
                return c.ToSprite(32f, new Vector2(0.5f, 0f));
            });
        }

        // ============================================================ Coletáveis

        /// <summary>Vinil preto e branco (moeda do jogo). 1 unidade.</summary>
        public static Sprite Vinyl()
        {
            return Get("vinyl", () =>
            {
                var c = new Canvas(32, 32);
                c.Shape((x, y) => SdCircle(x, y, 16f, 16f, 15.5f), new Color(0.04f, 0.04f, 0.05f, 1f));
                c.Shape((x, y) => Mathf.Abs(SdCircle(x, y, 16f, 16f, 12.2f)) - 0.4f, new Color(0.30f, 0.30f, 0.32f, 1f));
                c.Shape((x, y) => Mathf.Abs(SdCircle(x, y, 16f, 16f, 9.4f)) - 0.4f, new Color(0.30f, 0.30f, 0.32f, 1f));
                var wedge = new[] { new Vector2(16, 16), new Vector2(0, 30), new Vector2(8, 32), new Vector2(24, 20) };
                c.Shape((x, y) => Mathf.Max(SdCircle(x, y, 16f, 16f, 14f), SdPoly(x, y, wedge)), new Color(1f, 1f, 1f, 0.16f));
                c.Shape((x, y) => SdCircle(x, y, 16f, 16f, 5.6f), Color.white);
                c.Shape((x, y) => SdCircle(x, y, 16f, 16f, 1.5f), new Color(0.04f, 0.04f, 0.05f, 1f));
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Nota musical rosa (cura +15 HP). 1 unidade.</summary>
        public static Sprite Note()
        {
            return Get("note", () =>
            {
                var c = new Canvas(32, 32);
                Func<float, float, float> head = (x, y) => SdEllipse(x, y, 11f, 8f, 7f, 5f);
                Func<float, float, float> stem = (x, y) => SdRoundBox(x, y, 16.5f, 17.5f, 1.6f, 10f, 1f);
                Func<float, float, float> flag = (x, y) => SdSegment(x, y, 17f, 26f, 25f, 17f, 2f);
                Func<float, float, float> all = (x, y) => Mathf.Min(head(x, y), Mathf.Min(stem(x, y), flag(x, y)));
                c.Shape((x, y) => all(x, y) - 2f, HivePalette.Black);
                c.Shape(all, HivePalette.Pink);
                c.Shape((x, y) => SdEllipse(x, y, 9.5f, 9.5f, 2.2f, 1.4f), new Color(1f, 1f, 1f, 0.7f));
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Fragmento do Disco Dourado (fatia de disco). ~1.5 unidade.</summary>
        public static Sprite Fragment()
        {
            return Get("fragment", () =>
            {
                var c = new Canvas(48, 48);
                var wedge = new[]
                {
                    new Vector2(24, 24), new Vector2(24 + 40, 24), new Vector2(24 + 36, 24 + 18),
                    new Vector2(24 + 26, 24 + 30), new Vector2(24 + 10, 24 + 38), new Vector2(24 - 7, 24 + 40)
                };
                Func<float, float, float> sd = (x, y) => Mathf.Max(SdCircle(x, y, 24f, 24f, 21f), SdPoly(x, y, wedge));
                c.Outlined(sd, HivePalette.Gold, HivePalette.Black, 2f);
                c.Shape((x, y) => Mathf.Max(SdCircle(x, y, 24f, 24f, 17f), SdPoly(x, y, Scaled(wedge, 24f, 24f, 0.78f)))
                                  + 0f, HivePalette.Honey);
                c.Shape((x, y) => SdSegment(x, y, 30f, 28f, 38f, 36f, 1.4f), new Color(1f, 1f, 1f, 0.85f));
                c.Shape((x, y) => SdCircle(x, y, 24f, 24f, 3f), HivePalette.Black);
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Gota de mel. Pivô no centro.</summary>
        public static Sprite Drop()
        {
            return Get("drop", () =>
            {
                var c = new Canvas(24, 32);
                var tip = new[] { new Vector2(12, 30), new Vector2(4.5f, 13), new Vector2(19.5f, 13) };
                Func<float, float, float> sd = (x, y) => Mathf.Min(SdCircle(x, y, 12f, 10f, 8f), SdPoly(x, y, tip));
                c.Outlined(sd, HivePalette.Honey, HivePalette.Black, 1.8f);
                c.Shape((x, y) => SdEllipse(x, y, 9f, 11f, 2f, 3.2f), new Color(1f, 1f, 1f, 0.8f));
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        // ============================================================ Criaturas

        private static void DrawBee(Canvas c, float cx, float cy, float rx, float ry, Color body, Color stripe,
                                    Color eye, float stingerLen)
        {
            Func<float, float, float> abd = (x, y) => SdEllipse(x, y, cx - rx * 0.15f, cy, rx, ry);
            Func<float, float, float> head = (x, y) => SdCircle(x, y, cx + rx * 0.85f, cy + ry * 0.1f, ry * 0.72f);
            var sting = new[]
            {
                new Vector2(cx - rx * 1.05f, cy + ry * 0.22f), new Vector2(cx - rx * 1.05f - stingerLen, cy),
                new Vector2(cx - rx * 1.05f, cy - ry * 0.22f)
            };
            Func<float, float, float> sd = (x, y) => Mathf.Min(Mathf.Min(abd(x, y), head(x, y)), SdPoly(x, y, sting));
            c.Shape((x, y) => sd(x, y) - 1.8f, HivePalette.Black);
            c.Shape(sd, body);
            for (int i = 0; i < 2; i++)
            {
                float sx = cx - rx * 0.55f + i * rx * 0.55f;
                float hw = rx * 0.14f;
                c.Shape((x, y) => Mathf.Max(abd(x, y) + 0.5f, SdRoundBox(x, y, sx, cy, hw, ry + 2f, 0f)), stripe);
            }
            c.Shape((x, y) => SdCircle(x, y, cx + rx * 1.0f, cy + ry * 0.25f, ry * 0.34f), Color.white);
            c.Shape((x, y) => SdCircle(x, y, cx + rx * 1.06f, cy + ry * 0.25f, ry * 0.18f), eye);
            c.Shape((x, y) => SdSegment(x, y, cx + rx * 1.05f, cy + ry * 0.7f, cx + rx * 1.4f, cy + ry * 1.05f, 0.9f), HivePalette.Black);
        }

        /// <summary>Abelha operária (voadora), olhando para a direita. ~1.4 x 1 unidade.</summary>
        public static Sprite Bee()
        {
            return Get("bee", () =>
            {
                var c = new Canvas(44, 32);
                DrawBee(c, 19f, 15f, 12f, 9f, HivePalette.Gold, HivePalette.Black, HivePalette.Black, 5f);
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Vespa-guarda (terrestre, forte), olhando para a direita. ~1.8 x 1.3 unidade.</summary>
        public static Sprite Hornet()
        {
            return Get("hornet", () =>
            {
                var c = new Canvas(58, 42);
                DrawBee(c, 25f, 20f, 16f, 12f, new Color32(40, 28, 14, 255), HivePalette.Honey, HivePalette.Danger, 6f);
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>A Rainha Abelha, olhando para a direita. 4 x 4 unidades.</summary>
        public static Sprite Queen()
        {
            return Get("queen", () =>
            {
                var c = new Canvas(128, 128);
                DrawBee(c, 54f, 58f, 38f, 30f, HivePalette.Gold, HivePalette.Black, HivePalette.Black, 14f);
                // coroa
                var crown = new[]
                {
                    new Vector2(80, 80), new Vector2(76, 106), new Vector2(88, 94), new Vector2(98, 112),
                    new Vector2(108, 94), new Vector2(120, 106), new Vector2(116, 80)
                };
                c.Outlined((x, y) => SdPoly(x, y, crown), HivePalette.Pink, HivePalette.Black, 2f);
                c.Shape((x, y) => SdCircle(x, y, 98f, 88f, 4f), HivePalette.Cyan);
                c.Shape((x, y) => SdCircle(x, y, 84f, 86f, 2.6f), HivePalette.Gold);
                c.Shape((x, y) => SdCircle(x, y, 112f, 86f, 2.6f), HivePalette.Gold);
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Asa translúcida. Pivô na base esquerda (gira em torno do corpo).</summary>
        public static Sprite Wing()
        {
            return Get("wing", () =>
            {
                var c = new Canvas(40, 22);
                c.Shape((x, y) => SdEllipse(x, y, 20f, 11f, 19f, 9.5f), new Color(1f, 1f, 1f, 0.55f));
                c.Shape((x, y) => Mathf.Abs(SdEllipse(x, y, 20f, 11f, 19f, 9.5f)) - 0.8f, new Color(1f, 1f, 1f, 0.9f));
                return c.ToSprite(32f, new Vector2(0.05f, 0.5f));
            });
        }

        /// <summary>Ferrão (projétil), apontando para a direita. ~0.6 x 0.4 unidade.</summary>
        public static Sprite Stinger()
        {
            return Get("stinger", () =>
            {
                var c = new Canvas(20, 12);
                var dia = new[] { new Vector2(0.5f, 6f), new Vector2(8f, 11f), new Vector2(19.5f, 6f), new Vector2(8f, 1f) };
                c.Outlined((x, y) => SdPoly(x, y, dia), HivePalette.Pink, HivePalette.Black, 1.2f);
                c.Shape((x, y) => SdEllipse(x, y, 11f, 6f, 5f, 1.8f), Color.white);
                return c.ToSprite(32f, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Onda de choque (crista apontando para a direita). 1.25 x 0.75 unidade.</summary>
        public static Sprite Wave()
        {
            return Get("wave", () =>
            {
                var c = new Canvas(40, 24);
                var crest = new[] { new Vector2(0.5f, 0.5f), new Vector2(32f, 0.5f), new Vector2(39f, 9f), new Vector2(27f, 22f), new Vector2(14f, 10f) };
                c.Outlined((x, y) => SdPoly(x, y, crest), HivePalette.Pink, HivePalette.Black, 1.4f);
                c.Shape((x, y) => SdPoly(x, y, Scaled(crest, 16f, 6f, 0.55f)), HivePalette.Gold);
                return c.ToSprite(32f, new Vector2(0.5f, 0f));
            });
        }

        // ============================================================ Cenário interativo

        /// <summary>Lanterna de checkpoint. Pivô na base. 0.75 x 1.5 unidade.</summary>
        public static Sprite Lantern(bool active)
        {
            return Get(active ? "lantern1" : "lantern0", () =>
            {
                var c = new Canvas(24, 48);
                c.Outlined((x, y) => SdRoundBox(x, y, 12f, 14f, 2f, 13f, 1f), HivePalette.Comb, HivePalette.Black, 1.5f);
                c.Outlined((x, y) => SdRoundBox(x, y, 12f, 3f, 8f, 3f, 2f), HivePalette.Comb, HivePalette.Black, 1.5f);
                Color lamp = active ? HivePalette.Gold : new Color32(110, 90, 60, 255);
                c.Outlined((x, y) => SdHex(x, y, 12f, 36f, 9f), lamp, HivePalette.Black, 1.8f);
                c.Shape((x, y) => SdHex(x, y, 12f, 36f, 4.5f), active ? Color.white : new Color32(70, 56, 36, 255));
                if (active) c.Shape((x, y) => SdHex(x, y, 12f, 36f, 9f) + 4f, new Color(HivePalette.Pink.r, HivePalette.Pink.g, HivePalette.Pink.b, 0.25f));
                return c.ToSprite(32f, new Vector2(0.5f, 0f));
            });
        }

        /// <summary>Portal de saída (anel dourado + rosa). 2 unidades.</summary>
        public static Sprite Ring()
        {
            return Get("ring", () =>
            {
                var c = new Canvas(128, 128);
                c.Shape((x, y) => Mathf.Abs(SdCircle(x, y, 64f, 64f, 54f)) - 7f, HivePalette.Black);
                c.Shape((x, y) => Mathf.Abs(SdCircle(x, y, 64f, 64f, 54f)) - 5f, HivePalette.Gold);
                c.Shape((x, y) => Mathf.Abs(SdCircle(x, y, 64f, 64f, 40f)) - 2.5f, HivePalette.Pink);
                c.Shape((x, y) => SdCircle(x, y, 64f, 64f, 36f), new Color(1f, 0.85f, 0.3f, 0.18f));
                return c.ToSprite(64f, new Vector2(0.5f, 0.5f));
            });
        }
    }
}
