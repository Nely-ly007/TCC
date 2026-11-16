using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Toast na tela para Fase 3 (Graveyard Groove).
    /// Uso: GGOverlay.Toast("CHECKPOINT", GGPalette.Orange, 1.4f);
    /// Cria automaticamente um Canvas overlay se não existir.
    /// </summary>
    public class GGOverlay : MonoBehaviour
    {
        private static GGOverlay inst;
        private Canvas canvas;
        private static GGOverlay Inst
        {
            get
            {
                if (inst == null)
                {
                    var go = new GameObject("GGOverlay");
                    inst = go.AddComponent<GGOverlay>();
                    DontDestroyOnLoad(go);
                    inst.BuildCanvas();
                }
                return inst;
            }
        }

        private void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
        }

        public static void Toast(string message, Color color, float duration = 1.4f)
        {
            Inst.StartCoroutine(Inst.ShowToast(message, color, duration));
        }

        private IEnumerator ShowToast(string message, Color color, float duration)
        {
            // Container
            var go = new GameObject("Toast");
            go.transform.SetParent(canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.6f);
            rt.anchorMax = new Vector2(0.5f, 0.6f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 80f);

            // Fundo semi-transparente
            var bg = new GameObject("BG");
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);

            // Texto
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var txt = textGo.AddComponent<Text>();
            txt.text      = message;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 36;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = color;

            // Fade in
            float t = 0f;
            float fadeIn = 0.15f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / fadeIn);
                SetAlpha(go, bgImg, txt, a);
                // Sobe levemente
                rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(-10f, 0f, a));
                yield return null;
            }

            // Hold
            yield return new WaitForSeconds(duration - fadeIn - 0.25f);

            // Fade out
            t = 0f;
            float fadeOut = 0.25f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(1f - t / fadeOut);
                SetAlpha(go, bgImg, txt, a);
                yield return null;
            }

            Destroy(go);
        }

        private void SetAlpha(GameObject go, Image bg, Text txt, float a)
        {
            var c = bg.color; c.a = 0.55f * a; bg.color = c;
            var tc = txt.color; tc.a = a; txt.color = tc;
        }
    }
}
