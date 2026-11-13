using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Monta, por código, os projéteis e hazards temporizados dos ataques do boss.</summary>
    public static class MTHazardFactory
    {
        /// <summary>Posiciona um sprite pelo CENTRO e escala para o tamanho desejado (independe do pivô).</summary>
        public static void PlaceCentered(Transform t, Sprite s, Vector2 center, Vector2 size)
        {
            Vector2 ss = s.bounds.size;
            float sx = size.x / Mathf.Max(0.001f, ss.x);
            float sy = size.y / Mathf.Max(0.001f, ss.y);
            t.localScale = new Vector3(sx, sy, 1f);
            Vector2 off = new Vector2(s.bounds.center.x * sx, s.bounds.center.y * sy);
            t.position = new Vector3(center.x - off.x, center.y - off.y, t.position.z);
        }

        public static MTShot Shot(Vector2 position, Vector2 velocity, Sprite sprite, Vector2 size, float damage,
            float lifetime, Color tint, float spin = 0f, float sineAmplitude = 0f, float sineFrequency = 3f)
        {
            GameObject go = new GameObject("MT_Atk_Shot");
            go.SetActive(false);

            Sprite s = sprite != null ? sprite : MTUtil.DiscSprite;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = tint;
            sr.sortingOrder = 6;
            sr.flipX = velocity.x < 0f && sprite != null;
            PlaceCentered(go.transform, s, position, size);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            Vector2 ss = s.bounds.size;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = ss * 0.7f;
            col.offset = s.bounds.center;

            MTShot shot = go.AddComponent<MTShot>();
            shot.velocity = velocity;
            shot.damage = damage;
            shot.lifetime = lifetime;
            shot.spinDegrees = spin;
            shot.sineAmplitude = sineAmplitude;
            shot.sineFrequency = sineFrequency;
            go.SetActive(true);
            return shot;
        }

        /// <summary>Hazard com telegraph. Tudo em coordenadas de mundo (centro + tamanho).</summary>
        public static MTTimedHazard Timed(Vector2 telegraphCenter, Vector2 telegraphSize, Color telegraphColor,
            Sprite activeSprite, Color activeColor, Vector2 activeCenter, Vector2 activeSize, int activeOrder,
            Vector2 colliderCenter, Vector2 colliderSize,
            float telegraphSeconds, float activeSeconds, float damage, Vector2 slideFromOffset)
        {
            GameObject root = new GameObject("MT_Atk_Timed");
            root.SetActive(false);
            root.transform.position = Vector3.zero;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            GameObject tg = new GameObject("Telegraph");
            tg.transform.SetParent(root.transform, false);
            SpriteRenderer tsr = tg.AddComponent<SpriteRenderer>();
            tsr.sprite = MTUtil.WhiteSprite;
            tsr.color = telegraphColor;
            tsr.sortingOrder = 5;
            PlaceCentered(tg.transform, MTUtil.WhiteSprite, telegraphCenter, telegraphSize);

            Sprite s = activeSprite != null ? activeSprite : MTUtil.WhiteSprite;
            GameObject ac = new GameObject("Active");
            ac.transform.SetParent(root.transform, false);
            SpriteRenderer asr = ac.AddComponent<SpriteRenderer>();
            asr.sprite = s;
            asr.color = activeColor;
            asr.sortingOrder = activeOrder;
            PlaceCentered(ac.transform, s, activeCenter, activeSize);

            GameObject hit = new GameObject("Hit");
            hit.transform.SetParent(ac.transform, false);
            hit.transform.position = new Vector3(colliderCenter.x, colliderCenter.y, 0f);
            hit.transform.localRotation = Quaternion.identity;
            hit.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, ac.transform.localScale.x), 1f / Mathf.Max(0.0001f, ac.transform.localScale.y), 1f);
            BoxCollider2D box = hit.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = colliderSize;
            MTContactDamage cd = hit.AddComponent<MTContactDamage>();
            cd.damage = damage;
            cd.active = false;

            MTTimedHazard th = root.AddComponent<MTTimedHazard>();
            th.telegraphTime = telegraphSeconds;
            th.activeTime = activeSeconds;
            th.slideFromOffset = slideFromOffset;
            th.telegraphRenderer = tsr;
            th.activeRenderer = asr;
            th.hitCollider = box;
            th.contact = cd;
            root.SetActive(true);
            return th;
        }
    }
}
