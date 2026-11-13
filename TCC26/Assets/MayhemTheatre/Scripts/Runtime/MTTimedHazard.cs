using System.Collections;
using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Hazard com aviso: fase 1 "telegraph" (área piscando), fase 2 "ativo" (sprite entra e causa dano), depois some.
    /// Usado pelos holofotes, cortina que cai e mãos do chão do boss. Montado pelo MTHazardFactory.
    /// </summary>
    public class MTTimedHazard : MonoBehaviour
    {
        public float telegraphTime = 1f;
        public float activeTime = 1f;
        public float slideTime = 0.12f;
        public Vector2 slideFromOffset = Vector2.zero;
        public SpriteRenderer telegraphRenderer;
        public SpriteRenderer activeRenderer;
        public Collider2D hitCollider;
        public MTContactDamage contact;

        void Start()
        {
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            if (activeRenderer != null) activeRenderer.enabled = false;
            if (hitCollider != null) hitCollider.enabled = false;
            if (contact != null) contact.active = false;

            Color tc = telegraphRenderer != null ? telegraphRenderer.color : Color.white;
            float t = 0f;
            while (t < telegraphTime)
            {
                t += Time.deltaTime;
                if (telegraphRenderer != null)
                {
                    Color c = tc;
                    c.a = tc.a * (0.55f + 0.45f * Mathf.Sin(t * 22f));
                    telegraphRenderer.color = c;
                }
                yield return null;
            }
            if (telegraphRenderer != null) telegraphRenderer.enabled = false;

            Vector3 end = Vector3.zero;
            if (activeRenderer != null)
            {
                activeRenderer.enabled = true;
                end = activeRenderer.transform.localPosition;
                Vector3 start = end + (Vector3)slideFromOffset;
                t = 0f;
                while (t < slideTime)
                {
                    t += Time.deltaTime;
                    activeRenderer.transform.localPosition = Vector3.Lerp(start, end, Mathf.Clamp01(t / Mathf.Max(0.01f, slideTime)));
                    yield return null;
                }
                activeRenderer.transform.localPosition = end;
            }

            if (hitCollider != null) hitCollider.enabled = true;
            if (contact != null) contact.active = true;
            yield return new WaitForSeconds(activeTime);

            if (contact != null) contact.active = false;
            if (hitCollider != null) hitCollider.enabled = false;

            if (activeRenderer != null)
            {
                Vector3 from = activeRenderer.transform.localPosition;
                Vector3 to = from + (Vector3)slideFromOffset;
                t = 0f;
                while (t < slideTime)
                {
                    t += Time.deltaTime;
                    activeRenderer.transform.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(0.01f, slideTime)));
                    yield return null;
                }
            }
            Destroy(gameObject);
        }
    }
}
