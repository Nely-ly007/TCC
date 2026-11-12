using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Barra de vida do boss em espaço de mundo (acima da cabeça) — funciona sem nenhuma UI.
    /// Para usar o seu HUD em UI Toolkit, ligue GGBoss.onHealthNormalized ao seu HUD e desative este objeto.
    /// </summary>
    public class GGWorldHealthBar : MonoBehaviour
    {
        [SerializeField] float width = 3.2f;
        [SerializeField] float height = 0.24f;
        [SerializeField] Color backColor = new Color(0.05f, 0.03f, 0.1f, 0.85f);
        [SerializeField] Color fillColor = new Color(1f, 0.3f, 0.55f, 1f);

        Transform fill;
        GameObject root;

        void Awake()
        {
            root = new GameObject("Bar");
            root.transform.SetParent(transform, false);

            GameObject back = new GameObject("Back");
            back.transform.SetParent(root.transform, false);
            back.transform.localScale = new Vector3(width + 0.1f, height + 0.1f, 1f);
            SpriteRenderer sb = back.AddComponent<SpriteRenderer>();
            sb.sprite = GGUtil.WhiteSprite;
            sb.color = backColor;
            sb.sortingOrder = 20;

            GameObject f = new GameObject("Fill");
            f.transform.SetParent(root.transform, false);
            fill = f.transform;
            SpriteRenderer sf = f.AddComponent<SpriteRenderer>();
            sf.sprite = GGUtil.WhiteSprite;
            sf.color = fillColor;
            sf.sortingOrder = 21;

            SetValue(1f);
            root.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }

        public void SetValue(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (fill == null) return;
            fill.localScale = new Vector3(Mathf.Max(0.0001f, width * normalized), height, 1f);
            fill.localPosition = new Vector3(-width * 0.5f + width * normalized * 0.5f, 0f, 0f);
        }
    }
}
