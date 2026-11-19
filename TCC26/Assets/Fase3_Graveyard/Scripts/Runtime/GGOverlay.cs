using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toast na tela genérico — funciona em qualquer fase (Fase 1, 3, 4, Hub).
/// Uso: GameOverlay.Toast("CHECKPOINT", new Color(1f, 0.6f, 0f), 1.4f);
/// Cria automaticamente um Canvas overlay se não existir.
/// Sem dependência de namespace de fase.
/// </summary>
public class GameOverlay : MonoBehaviour
{
    private static GameOverlay inst;
    private Canvas canvas;

    private static GameOverlay Inst
    {
        get
        {
            if (inst == null)
            {
                var go = new GameObject("GameOverlay");
                DontDestroyOnLoad(go);
                inst = go.AddComponent<GameOverlay>();
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
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 80f);
        rt.anchoredPosition = new Vector2(0f, -10f);

        // Fundo semi-transparente
        var bg = new GameObject("BG");
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0f);

        // Texto
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var txt = textGo.AddComponent<Text>();
        txt.text = message;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 36;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(color.r, color.g, color.b, 0f);

        // Fade in + sobe
        float t = 0f;
        const float fadeIn = 0.15f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fadeIn);
            bgImg.color = new Color(0f, 0f, 0f, 0.55f * a);
            txt.color = new Color(color.r, color.g, color.b, a);
            rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(-10f, 0f, a));
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(Mathf.Max(0f, duration - fadeIn - 0.25f));

        // Fade out
        t = 0f;
        const float fadeOut = 0.25f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(1f - t / fadeOut);
            bgImg.color = new Color(0f, 0f, 0f, 0.55f * a);
            txt.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }

        Destroy(go);
    }
}