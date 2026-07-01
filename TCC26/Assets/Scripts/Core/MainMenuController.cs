using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// POP ADVENTURE - MainMenuController v2
/// - Botões conectados via código (não precisa configurar OnClick no Inspector)
/// - Painéis Settings e Credits com botão Return funcional
/// - Fade de entrada automático
/// </summary>
public class MainMenuController : MonoBehaviour
{
    // ── LOGO ─────────────────────────────────────────────────────
    [Header("Logo")]
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private CanvasGroup logoGroup;

    // ── BOTÕES PRINCIPAIS ─────────────────────────────────────────
    [Header("Botões Principais")]
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnCredits;
    [SerializeField] private Button btnQuit;
    [SerializeField] private CanvasGroup buttonGroupCanvas;

    // ── PAINEL SETTINGS ───────────────────────────────────────────
    [Header("Painel Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup settingsGroup;
    [SerializeField] private Button btnSettingsReturn; // botão Return dentro do painel
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    // ── PAINEL CREDITS ────────────────────────────────────────────
    [Header("Painel Credits")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private CanvasGroup creditsGroup;
    [SerializeField] private Button btnCreditsReturn; // botão Return dentro do painel

    // ── BEAT / MÚSICA ─────────────────────────────────────────────
    [Header("Beat")]
    [SerializeField] private RectTransform beatIndicator;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private float menuBPM = 120f;

    // ── VERSÃO ────────────────────────────────────────────────────
    [Header("Versão")]
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private string version = "v1.0";

    // ── ESTADO ────────────────────────────────────────────────────
    private bool inputBlocked     = true;
    private bool isTransitioning  = false;
    private bool settingsOpen     = false;
    private bool creditsOpen      = false;
    private Vector3 beatOrigScale;

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        // Fecha painéis no início
        SetPanelState(settingsPanel, settingsGroup, false, instant: true);
        SetPanelState(creditsPanel,  creditsGroup,  false, instant: true);

        // Logo começa fora da tela
        if (logoRect != null)
        {
            logoRect.anchoredPosition = new Vector2(0, 150f);
            if (logoGroup != null) logoGroup.alpha = 0f;
        }

        if (buttonGroupCanvas != null) buttonGroupCanvas.alpha = 0f;
        if (versionText != null) versionText.text = version;
    }

    void Start()
    {
        // ── Conecta botões via código ─────────────────────────────
        // Assim não importa se o OnClick no Inspector está vazio
        if (btnPlay     != null) btnPlay.onClick.AddListener(OnClickPlay);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnClickSettings);
        if (btnCredits  != null) btnCredits.onClick.AddListener(OnClickCredits);
        if (btnQuit     != null) btnQuit.onClick.AddListener(OnClickQuit);

        // Botões Return dos painéis
        if (btnSettingsReturn != null) btnSettingsReturn.onClick.AddListener(OnClickSettingsReturn);
        if (btnCreditsReturn  != null) btnCreditsReturn.onClick.AddListener(OnClickCreditsReturn);

        // Beat indicator
        if (beatIndicator != null) beatOrigScale = beatIndicator.localScale;

        // Preferências salvas
        LoadPreferences();

        // Música e animação de entrada
        if (menuMusic != null)
            RhythmManager.Instance?.StartMusic(menuMusic, menuBPM);

        SceneController.Instance?.FadeIn(0.5f);
        StartCoroutine(EntranceAnimation());

        RhythmManager.OnBeatStatic       += OnBeat;
        RhythmManager.OnBeatNumberStatic += OnBeatNumber;
    }

    void OnDestroy()
    {
        RhythmManager.OnBeatStatic       -= OnBeat;
        RhythmManager.OnBeatNumberStatic -= OnBeatNumber;
    }

    // ── ANIMAÇÃO DE ENTRADA ───────────────────────────────────────
    private IEnumerator EntranceAnimation()
    {
        yield return new WaitForSeconds(0.4f);

        // Logo desce + fade in
        float elapsed = 0f;
        float duration = 0.6f;
        Vector2 startPos = new Vector2(0, 200f);
        Vector2 endPos   = new Vector2(0, 190);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (logoRect  != null) logoRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            if (logoGroup != null) logoGroup.alpha = t;
            yield return null;
        }
        if (logoRect  != null) logoRect.anchoredPosition = endPos;
        if (logoGroup != null) logoGroup.alpha = 1f;

        yield return new WaitForSeconds(0.15f);

        // Botões fazem fade in
        yield return StartCoroutine(FadeGroup(buttonGroupCanvas, 0f, 1f, 0.4f));

        inputBlocked = false;
    }

    // ── BOTÃO PLAY ────────────────────────────────────────────────
    private void OnClickPlay()
    {
        if (inputBlocked || isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        // Fecha qualquer painel aberto antes de sair
        if (settingsOpen) yield return StartCoroutine(ClosePanel(settingsPanel, settingsGroup));
        if (creditsOpen)  yield return StartCoroutine(ClosePanel(creditsPanel,  creditsGroup));

        yield return StartCoroutine(LogoPunch());
        yield return new WaitForSeconds(0.2f);

        // ← Esta é a chamada que leva ao Hub
        SceneController.Instance?.GoToHub();
    }

    // ── BOTÃO SETTINGS ────────────────────────────────────────────
    private void OnClickSettings()
    {
        if (inputBlocked) return;
        if (creditsOpen) StartCoroutine(ClosePanel(creditsPanel, creditsGroup,
            onDone: () => StartCoroutine(OpenPanel(settingsPanel, settingsGroup))));
        else if (!settingsOpen) StartCoroutine(OpenPanel(settingsPanel, settingsGroup));
    }

    // ── BOTÃO RETURN DO SETTINGS ──────────────────────────────────
    private void OnClickSettingsReturn()
    {
        if (!settingsOpen) return;
        StartCoroutine(ClosePanel(settingsPanel, settingsGroup));
    }

    // ── BOTÃO CREDITS ─────────────────────────────────────────────
    private void OnClickCredits()
    {
        if (inputBlocked) return;
        if (settingsOpen) StartCoroutine(ClosePanel(settingsPanel, settingsGroup,
            onDone: () => StartCoroutine(OpenPanel(creditsPanel, creditsGroup))));
        else if (!creditsOpen) StartCoroutine(OpenPanel(creditsPanel, creditsGroup));
    }

    // ── BOTÃO RETURN DO CREDITS ───────────────────────────────────
    private void OnClickCreditsReturn()
    {
        if (!creditsOpen) return;
        StartCoroutine(ClosePanel(creditsPanel, creditsGroup));
    }

    // ── BOTÃO QUIT ────────────────────────────────────────────────
    private void OnClickQuit()
    {
        if (inputBlocked) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── ESC fecha painéis ─────────────────────────────────────────
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsOpen) OnClickSettingsReturn();
            else if (creditsOpen) OnClickCreditsReturn();
        }
    }

    // ── ABRIR / FECHAR PAINÉIS ────────────────────────────────────
    private IEnumerator OpenPanel(GameObject panel, CanvasGroup group)
    {
        if (panel == null) yield break;
        if (panel == settingsPanel) settingsOpen = true;
        if (panel == creditsPanel)  creditsOpen  = true;

        SetPanelState(panel, group, true, instant: false);
        yield return StartCoroutine(FadeGroup(group, 0f, 1f, 0.25f));
    }

    private IEnumerator ClosePanel(GameObject panel, CanvasGroup group,
                                   System.Action onDone = null)
    {
        if (panel == null) yield break;

        yield return StartCoroutine(FadeGroup(group, 1f, 0f, 0.2f));
        SetPanelState(panel, group, false, instant: true);

        if (panel == settingsPanel) settingsOpen = false;
        if (panel == creditsPanel)  creditsOpen  = false;

        onDone?.Invoke();
    }

    private void SetPanelState(GameObject panel, CanvasGroup group,
                                bool open, bool instant)
    {
        if (panel == null) return;
        panel.SetActive(open);
        if (group != null)
        {
            group.alpha          = instant ? (open ? 1f : 0f) : group.alpha;
            group.interactable   = open;
            group.blocksRaycasts = open;
        }
    }

    // ── PREFERÊNCIAS ─────────────────────────────────────────────
    private void LoadPreferences()
    {
        if (volumeSlider != null)
        {
            float vol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            volumeSlider.value = vol;
            AudioListener.volume = vol;
            volumeSlider.onValueChanged.AddListener(v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat("MasterVolume", v);
                PlayerPrefs.Save();
            });
        }

        if (fullscreenToggle != null)
        {
            bool fs = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            fullscreenToggle.isOn = fs;
            Screen.fullScreen = fs;
            fullscreenToggle.onValueChanged.AddListener(v =>
            {
                Screen.fullScreen = v;
                PlayerPrefs.SetInt("Fullscreen", v ? 1 : 0);
                PlayerPrefs.Save();
            });
        }
    }

    // ── BEAT SYNC ─────────────────────────────────────────────────
    private void OnBeat()
    {
        if (beatIndicator != null)
            StartCoroutine(BeatPunch(beatIndicator, beatOrigScale));
    }

    private void OnBeatNumber(int beat)
    {
        if (beat == 0 && logoRect != null && !settingsOpen && !creditsOpen)
            StartCoroutine(LogoPunch());
    }

    // ── ANIMAÇÕES ─────────────────────────────────────────────────
    private IEnumerator LogoPunch()
    {
        if (logoRect == null) yield break;
        Vector3 orig = logoRect.localScale;
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.07f, 1f, t / 0.12f);
            logoRect.localScale = orig * s;
            yield return null;
        }
        logoRect.localScale = orig;
    }

    private IEnumerator BeatPunch(RectTransform target, Vector3 origScale)
    {
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(origScale * 1.2f, origScale, t / 0.12f);
            yield return null;
        }
        target.localScale = origScale;
    }

    // ── UTILITÁRIO ────────────────────────────────────────────────
    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float dur)
    {
        if (group == null) yield break;
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / dur);
            yield return null;
        }
        group.alpha = to;
        group.interactable   = to >= 1f;
        group.blocksRaycasts = to >= 1f;
    }
}