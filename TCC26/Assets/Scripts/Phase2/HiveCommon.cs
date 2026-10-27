using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>Paleta da Fase 2 — dourado e preto com acentos Pop Art (rosa/ciano).</summary>
    public static class HivePalette
    {
        public static readonly Color Black = new Color32(14, 10, 6, 255);
        public static readonly Color Comb = new Color32(38, 26, 12, 255);
        public static readonly Color DeepAmber = new Color32(150, 88, 0, 255);
        public static readonly Color Amber = new Color32(214, 128, 0, 255);
        public static readonly Color Honey = new Color32(255, 176, 0, 255);
        public static readonly Color Gold = new Color32(255, 214, 64, 255);
        public static readonly Color Cream = new Color32(255, 243, 200, 255);
        public static readonly Color Pink = new Color32(255, 62, 165, 255);
        public static readonly Color Cyan = new Color32(0, 229, 255, 255);
        public static readonly Color Danger = new Color32(255, 70, 50, 255);
    }

    /// <summary>Leitura de teclas compatível com Input Manager antigo, novo, ou "Both".</summary>
    public static class HiveInput
    {
        public static bool ConfirmPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                return true;
#endif
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame ||
                              k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
                return true;
#endif
            return false;
        }
    }

    public static class HiveMath
    {
        public static int Mod(int a, int m)
        {
            if (m <= 0) return 0;
            int r = a % m;
            return r < 0 ? r + m : r;
        }

        public static float Mod(float a, float m)
        {
            float r = a % m;
            return r < 0f ? r + m : r;
        }

        public static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float Frac(double v)
        {
            return (float)(v - System.Math.Floor(v));
        }
    }

    /// <summary>Raycasts que ignoram triggers, o jogador e criaturas (só sobra o cenário sólido).</summary>
    public static class HivePhysics
    {
        private static readonly RaycastHit2D[] hits = new RaycastHit2D[12];
        private static ContactFilter2D filter;
        private static bool ready;

        private static void Init()
        {
            filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = false;
            ready = true;
        }

        private static bool Ignore(Collider2D c)
        {
            if (c == null) return true;
            if (HiveBridge.IsPlayer(c)) return true;
            if (c.GetComponentInParent<HiveDamageable>() != null) return true;
            return false;
        }

        public static bool Ray(Vector2 origin, Vector2 dir, float dist, out RaycastHit2D hit)
        {
            if (!ready) Init();
            int n = Physics2D.Raycast(origin, dir, filter, hits, dist);
            for (int i = 0; i < n; i++)
            {
                if (Ignore(hits[i].collider)) continue;
                hit = hits[i];
                return true;
            }
            hit = default(RaycastHit2D);
            return false;
        }

        public static bool CircleBlocked(Vector2 origin, float radius, Vector2 dir, float dist)
        {
            if (!ready) Init();
            int n = Physics2D.CircleCast(origin, radius, dir, filter, hits, dist);
            for (int i = 0; i < n; i++)
            {
                if (Ignore(hits[i].collider)) continue;
                return true;
            }
            return false;
        }

        /// <summary>Torna o collider uma plataforma "one-way" (dá pra pular de baixo para cima).</summary>
        public static void MakeOneWay(GameObject go, Collider2D col)
        {
            col.usedByEffector = true;
            var e = go.AddComponent<PlatformEffector2D>();
            e.useOneWay = true;
            e.surfaceArc = 170f;
            e.useSideFriction = false;
            e.useSideBounce = false;
        }

        /// <summary>Y do chão logo abaixo de um ponto (ou fallback se não achar).</summary>
        public static float GroundY(Vector2 from, float maxDist, float fallback)
        {
            RaycastHit2D h;
            return Ray(from, Vector2.down, maxDist, out h) ? h.point.y : fallback;
        }
    }
}
