using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// POP ADVENTURE - PauseMenu
/// Abre com ESC em qualquer fase. Usa um Canvas criado por código.
/// Adicione este script a um GameObject vazio chamado "PauseMenu" na cena do GameManager
/// ou em cada cena de fase. Se o GameManager usar DontDestroyOnLoad, coloque lá.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Cenas")]
    [SerializeField] private string hubSceneName  = "Hub";

    [Header("Estilo")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color buttonColor      = new Color(0.9f, 0.1f, 0.5f, 1f);   // rosa disco
    [SerializeField] private Color buttonHover      = new Color(1f, 0.85f, 0.1f, 1f);    // dourado
    [SerializeField] private Color textColor        = Color.white;

    // Estado
    private bool isPaused = false;
    private bool initialized = false;

    // UI
    private Canvas         canvas;
    private GameObject     panel;
    private Slider         volumeSlider;

    // ── Ciclo de vida ────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        BuildUI();
        SetPanelVisible(false);
    }

    private void Update()
    {
        // Não abre pausa no Hub, Menu ou Ending
        string scene = SceneManager.GetActiveScene().name;
        if (scene == hubSceneName || scene == "MainMenu" || scene == "Ending") return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else          Pause();
        }
    }

    // ── Ações ────────────────────────────────────────────────────

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetPanelVisible(true);

        // Atualiza o slider com o volume atual
        if (volumeSlider != null)
            volumeSlider.value = AudioListener.volume;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetPanelVisible(false);
    }

    public void RestartPhase()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToHub()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(hubSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    // ── Construção da UI por código ───────────────────────────────

    private void BuildUI()
    {
        if (initialized) return;
        initialized = true;

        // Canvas
        var canvasGo = new GameObject("PauseCanvas");
        canvasGo.transform.SetParent(transform);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Painel de fundo
        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = backgroundColor;
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Título
        MakeLabel(panel.transform, "PAUSA", 72, new Vector2(0f, 160f));

        // Botões
        float startY = 60f;
        float gap    = 70f;
        MakeButton(panel.transform, "CONTINUAR",    new Vector2(0f, startY),          Resume);
        MakeButton(panel.transform, "REINICIAR FASE",new Vector2(0f, startY - gap),   RestartPhase);
        MakeButton(panel.transform, "VOLTAR AO HUB", new Vector2(0f, startY - gap*2f),GoToHub);
        MakeButton(panel.transform, "SAIR DO JOGO",  new Vector2(0f, startY - gap*3f),QuitGame);

        // Slider de volume
        MakeLabel(panel.transform, "VOLUME", 28, new Vector2(0f, startY - gap * 4f + 10f));
        volumeSlider = MakeSlider(panel.transform, new Vector2(0f, startY - gap * 4.8f));
        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void SetPanelVisible(bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    // ── Helpers de UI ─────────────────────────────────────────────

    private void MakeLabel(Transform parent, string text, int fontSize, Vector2 anchoredPos)
    {
        var go   = new GameObject("Label_" + text);
        go.transform.SetParent(parent, false);
        var txt  = go.AddComponent<Text>();
        txt.text      = text;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = textColor;
        txt.alignment = TextAnchor.MiddleCenter;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta     = new Vector2(600f, 80f);
        rect.anchoredPosition = anchoredPos;
    }

    private void MakeButton(Transform parent, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction action)
    {
        var go  = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = buttonColor;
        var btn = go.AddComponent<Button>();

        // Hover via ColorBlock
        var cb = btn.colors;
        cb.normalColor      = buttonColor;
        cb.highlightedColor = buttonHover;
        cb.pressedColor     = new Color(buttonHover.r * 0.8f, buttonHover.g * 0.8f, buttonHover.b * 0.8f);
        btn.colors = cb;

        btn.onClick.AddListener(action);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta        = new Vector2(400f, 54f);
        rect.anchoredPosition = anchoredPos;

        // Texto do botão
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txt   = txtGo.AddComponent<Text>();
        txt.text      = label;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 28;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = Color.black;
        txt.alignment = TextAnchor.MiddleCenter;
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
    }

    private Slider MakeSlider(Transform parent, Vector2 anchoredPos)
    {
        var go = new GameObject("VolumeSlider");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta        = new Vector2(400f, 30f);
        rect.anchoredPosition = anchoredPos;

        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgImg  = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin    = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax    = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin    = new Vector2(5f, 0f);
        fillAreaRect.offsetMax    = new Vector2(-5f, 0f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg  = fill.AddComponent<Image>();
        fillImg.color = buttonColor;
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;

        // Handle
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleImg  = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 30f);

        slider.handleRect     = handleRect;
        slider.targetGraphic  = handleImg;

        return slider;
    }
}
