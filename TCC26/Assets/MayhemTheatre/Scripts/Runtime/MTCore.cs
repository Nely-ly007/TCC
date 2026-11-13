using System;
using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Os 5 momentos musicais/seções da Fase 4.</summary>
    public enum MTMoment
    {
        Fase1 = 0,         // Disco Fever (reprise)
        Fase2 = 1,         // The Hive (reprise)
        Fase3 = 2,         // Graveyard Groove (reprise)
        Fase4 = 3,         // Mayhem Theatre (tema próprio)
        MixHarmonico = 4   // Luta final contra a Lady in Red
    }

    /// <summary>Tudo que leva dano do Player (inimigos, boss) implementa isto.</summary>
    public interface IMTDamageable
    {
        bool IsDead { get; }
        void TakeDamage(float amount, Vector2 hitPoint);
    }

    /// <summary>Configuração de um momento musical (usada pelo MTMusicDirector).</summary>
    [Serializable]
    public class MTMomentConfig
    {
        public MTMoment moment;
        public string label;
        public AudioClip clip;
        public float bpm = 64f;
        public bool loop = true;
        [Range(0f, 1f)] public float volume = 1f;
        public Color backgroundColor = Color.black;
    }

    public static class MTDamage
    {
        public const float DefaultPlayerShotDamage = 10f;

        /// <summary>
        /// Lê o dano de um projétil do Player. Usa o componente MTDamageSource se existir;
        /// senão aceita qualquer objeto cujo nome contenha "Projectile", "MusicNote" ou "Melody".
        /// </summary>
        public static bool TryReadPlayerShot(Collider2D other, out float damage)
        {
            damage = 0f;
            if (other == null) return false;
            if (other.GetComponentInParent<MTPlayerHook>() != null) return false;

            MTDamageSource src = other.GetComponentInParent<MTDamageSource>();
            if (src != null)
            {
                damage = src.damage;
                src.NotifyHit();
                return true;
            }

            string n = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject.name : other.gameObject.name;
            if (n.IndexOf("Projectile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("MusicNote", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Melody", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                damage = DefaultPlayerShotDamage;
                return true;
            }
            return false;
        }
    }

    public static class MTUtil
    {
        static Sprite whiteSprite;
        static Sprite discSprite;

        /// <summary>Sprite branco 1x1 unidade (para telegraphs, barras, placeholders).</summary>
        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    Texture2D t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    Color[] px = new Color[16];
                    for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                    t.SetPixels(px);
                    t.filterMode = FilterMode.Point;
                    t.Apply();
                    whiteSprite = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                    whiteSprite.name = "MT_White";
                }
                return whiteSprite;
            }
        }

        /// <summary>Disco/círculo com 1 unidade de diâmetro.</summary>
        public static Sprite DiscSprite
        {
            get
            {
                if (discSprite == null)
                {
                    const int s = 64;
                    Texture2D t = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    Color[] px = new Color[s * s];
                    float r = s * 0.5f;
                    for (int y = 0; y < s; y++)
                    {
                        for (int x = 0; x < s; x++)
                        {
                            float dx = x + 0.5f - r;
                            float dy = y + 0.5f - r;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = Mathf.Clamp01(r - d);
                            px[y * s + x] = new Color(1f, 1f, 1f, a);
                        }
                    }
                    t.SetPixels(px);
                    t.filterMode = FilterMode.Bilinear;
                    t.Apply();
                    discSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
                    discSprite.name = "MT_Disc";
                }
                return discSprite;
            }
        }

        // Compatível com Unity 2022 (velocity) e Unity 6 (linearVelocity)
        public static void SetVelocity(Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        public static Vector2 GetVelocity(Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static T FindFirst<T>() where T : UnityEngine.Object
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        public static T[] FindAll<T>() where T : UnityEngine.Object
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }

        public static bool IsPlayer(Collider2D c)
        {
            if (c == null) return false;
            return c.GetComponentInParent<MTPlayerHook>() != null || c.CompareTag("Player");
        }

        /// <summary>Transform do Player (MTPlayerHook, ou objeto com a tag Player).</summary>
        public static Transform FindPlayer()
        {
            if (MTPlayerHook.Instance != null) return MTPlayerHook.Instance.transform;
            GameObject go = GameObject.FindWithTag("Player");
            return go != null ? go.transform : null;
        }

        public static Bounds RendererBounds(GameObject go, out bool found)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            found = false;
            Bounds b = new Bounds(go.transform.position, Vector3.one * 2f);
            for (int i = 0; i < rs.Length; i++)
            {
                if (!found) { b = rs[i].bounds; found = true; }
                else b.Encapsulate(rs[i].bounds);
            }
            return b;
        }

        public static int Mod(int a, int n)
        {
            if (n <= 0) return 0;
            int r = a % n;
            return r < 0 ? r + n : r;
        }
    }

    /// <summary>Cria placeholders de pickups (nota que cura, vinil, fragmento).</summary>
    public static class MTPickupFactory
    {
        public static GameObject Create(MTPickup.PickupKind kind, Vector3 position, GameObject prefab, Sprite sprite, Color tint, float diameter)
        {
            GameObject go;
            if (prefab != null)
            {
                go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
                return go;
            }

            go = new GameObject("MT_Pickup_" + kind);
            go.transform.position = position;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Sprite s = sprite != null ? sprite : MTUtil.DiscSprite;
            sr.sprite = s;
            sr.color = tint;
            sr.sortingOrder = 4;
            float w = Mathf.Max(0.01f, s.bounds.size.x);
            go.transform.localScale = Vector3.one * (diameter / w);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = Mathf.Max(0.3f, w * 0.5f);

            MTPickup p = go.AddComponent<MTPickup>();
            p.kind = kind;
            return go;
        }
    }
}
