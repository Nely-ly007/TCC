using System;
using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.Fase3
{
    // ------------------------------------------------------------------
    // Eventos serializáveis (funcionam em qualquer versão do Unity)
    // ------------------------------------------------------------------
    [Serializable] public class GGIntEvent : UnityEvent<int> { }
    [Serializable] public class GGIntIntEvent : UnityEvent<int, int> { }
    [Serializable] public class GGFloatEvent : UnityEvent<float> { }

    /// <summary>
    /// Contrato de dano da Fase 3. Tudo que pode ser atingido (inimigos, boss, alvos de ressonância,
    /// o próprio Player) implementa esta interface.
    /// </summary>
    public interface IGGDamageable
    {
        void TakeDamage(int amount, Vector2 sourcePosition);
    }

    /// <summary>Compatibilidade Rigidbody2D.velocity (Unity 2022) x linearVelocity (Unity 6).</summary>
    public static class GGRb2DCompat
    {
        public static Vector2 GetVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }

    public static class GGUtil
    {
        public const string PlayerTag = "Player";

        static Transform cachedPlayer;
        static Sprite whiteSprite;
        static Sprite circleSprite;
        static readonly RaycastHit2D[] rayBuffer = new RaycastHit2D[10];

        /// <summary>Transform do Player (GameObject com a tag "Player").</summary>
        public static Transform Player
        {
            get
            {
                if (cachedPlayer == null)
                {
                    GameObject go = GameObject.FindGameObjectWithTag(PlayerTag);
                    cachedPlayer = go != null ? go.transform : null;
                }
                return cachedPlayer;
            }
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    t.SetPixel(0, 0, Color.white);
                    t.Apply();
                    t.filterMode = FilterMode.Point;
                    whiteSprite = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                    whiteSprite.name = "GG_White";
                }
                return whiteSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite == null)
                {
                    const int size = 64;
                    Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    float r = size * 0.5f;
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                            float a = Mathf.Clamp01((1f - d) * 4f);
                            t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                        }
                    }
                    t.Apply();
                    t.filterMode = FilterMode.Bilinear;
                    circleSprite = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                    circleSprite.name = "GG_Circle";
                }
                return circleSprite;
            }
        }

        /// <summary>
        /// Raycast que ignora triggers, o Player e corpos dinâmicos. Serve para "chão" e "parede".
        /// </summary>
        public static bool GroundRaycast(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D hit)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            int n = Physics2D.Raycast(origin, direction, filter, rayBuffer, distance);
            for (int i = 0; i < n; i++)
            {
                Collider2D c = rayBuffer[i].collider;
                if (c == null) continue;
                if (c.GetComponentInParent<GGPlayerHealth>() != null) continue;
                if (c.attachedRigidbody != null && c.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
                hit = rayBuffer[i];
                return true;
            }
            hit = default(RaycastHit2D);
            return false;
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
