using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// POP ADVENTURE - EndingScene
/// Coloque este script em um GameObject vazio chamado "EndingController" na sua cena de Ending.
/// A cena deve ter apenas a Main Camera. O script cria tudo por código.
/// </summary>
public class EndingScene : MonoBehaviour
{
    [Header("Cenas")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Tempo")]
    [SerializeField] private float fadeInDuration  = 2.0f;
    [SerializeField] private float holdDuration    = 3.0f;
    [SerializeField] private float textFadeIn      = 1.5f;

    private Canvas    canvas;
    private Image     background;
    private Text      titleText;
    private Text      hintText;
    private bool      canContinue = false;

    private void Start()
    {
        // Garante que o TimeScale voltou ao normal (caso venha de uma pausa)
        Time.timeScale = 1f;

        BuildUI();
        StartCoroutine(EndingSequence());
    }

    private void Update()
    {
        if (canContinue && (
            Input.anyKeyDown ||
            Input.GetMouseButtonDown(0)))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // ── Sequência ─────────────────────────────────────────────────

    private IEnumerator EndingSequence()
    {
        // Fundo começa preto
        background.color = Color.black;
        titleText.color  = new Color(1f, 1f, 1f, 0f);
        hintText.color   = new Color(1f, 1f, 1f, 0f);

        // Aguarda um beat antes de começar
        yield return new WaitForSeconds(0.5f);

        // Fade in do texto principal
        float t = 0f;
        while (t < textFadeIn)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / textFadeIn);
            titleText.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        titleText.color = Color.white;

        // Segura por um tempo
        yield return new WaitForSeconds(holdDuration);

        // Fade in do hint
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            hintText.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t));
            yield return null;
        }

        // Pulsa o hint
        canContinue = true;
        StartCoroutine(PulseHint());
    }

    private IEnumerator PulseHint()
    {
        while (true)
        {
            float a = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f);
            hintText.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
    }

    // ── Construção da UI ──────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("EndingCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Fundo preto cobrindo tudo
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        background = bgGo.AddComponent<Image>();
        background.color = Color.black;
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Texto principal "continua..."
        var titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(canvasGo.transform, false);
        titleText = titleGo.AddComponent<Text>();
        titleText.text      = "continua...";
        titleText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize  = 80;
        titleText.fontStyle = FontStyle.Italic;
        titleText.color     = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin       = new Vector2(0f, 0.5f);
        titleRect.anchorMax       = new Vector2(1f, 0.5f);
        titleRect.sizeDelta       = new Vector2(0f, 120f);
        titleRect.anchoredPosition = new Vector2(0f, 20f);

        // Hint "pressione qualquer tecla"
        var hintGo = new GameObject("HintText");
        hintGo.transform.SetParent(canvasGo.transform, false);
        hintText = hintGo.AddComponent<Text>();
        hintText.text      = "pressione qualquer tecla";
        hintText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize  = 28;
        hintText.color     = new Color(1f, 1f, 1f, 0f);
        hintText.alignment = TextAnchor.MiddleCenter;
        var hintRect = hintGo.GetComponent<RectTransform>();
        hintRect.anchorMin        = new Vector2(0f, 0.5f);
        hintRect.anchorMax        = new Vector2(1f, 0.5f);
        hintRect.sizeDelta        = new Vector2(0f, 50f);
        hintRect.anchoredPosition = new Vector2(0f, -60f);
    }
}
