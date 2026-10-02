using UnityEngine;
using UnityEngine.UIElements;
using TMPro;
using System.Collections;

/// <summary>
/// POP ADVENTURE - MainMenuController v4
/// Settings e Credits migrados para UI Toolkit.
/// Música do menu configurada para loop.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Logo")]
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private CanvasGroup logoGroup;

    [Header("Botões (uGUI)")]
    [SerializeField] private UnityEngine.UI.Button btnPlay;
    [SerializeField] private UnityEngine.UI.Button btnSettings;
    [SerializeField] private UnityEngine.UI.Button btnCredits;
    [SerializeField] private UnityEngine.UI.Button btnQuit;
    [SerializeField] private CanvasGroup buttonGroupCanvas;

    [Header("UI Toolkit Panels")]
    [SerializeField] private SettingsUIToolkit settingsUI;
    [SerializeField] private CreditsUIToolkit creditsUI;

    [Header("Beat")]
    [SerializeField] private RectTransform beatIndicator;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private float menuBPM = 120f;
    [SerializeField] private bool loopMenuMusic = true;

    [Header("Versão")]
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private string version = "v1.0";

    private bool inputBlocked = true;
    private bool isTransitioning = false;
    private Vector3 beatOrigScale;

    // ============================================================
    // AWAKE
    // ============================================================

    void Awake()
    {
        if (logoRect != null)
        {
            logoRect.anchoredPosition = new Vector2(26, 201f);

            if (logoGroup != null)
                logoGroup.alpha = 0f;
        }

        if (buttonGroupCanvas != null)
            buttonGroupCanvas.alpha = 0f;

        if (versionText != null)
            versionText.text = version;
    }

    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        btnPlay?.onClick.AddListener(OnClickPlay);
        btnSettings?.onClick.AddListener(OnClickSettings);
        btnCredits?.onClick.AddListener(OnClickCredits);
        btnQuit?.onClick.AddListener(OnClickQuit);

        if (beatIndicator != null)
            beatOrigScale = beatIndicator.localScale;

        // ========================================================
        // MÚSICA DO MENU
        // ========================================================

        if (menuMusic != null)
        {
            if (RhythmManager.Instance != null)
            {
                RhythmManager.Instance.StartMusic(
                    menuMusic,
                    menuBPM
                );

                // Garante que a música fique em loop
                AudioSource rhythmAudio =
                    RhythmManager.Instance.GetComponent<AudioSource>();

                if (rhythmAudio != null)
                {
                    rhythmAudio.loop = loopMenuMusic;
                }
            }
        }

        SceneController.Instance?.FadeIn(0.5f);

        StartCoroutine(
            EntranceAnimation()
        );

        RhythmManager.OnBeatStatic += OnBeat;
        RhythmManager.OnBeatNumberStatic += OnBeatNumber;
    }

    // ============================================================
    // DESTROY
    // ============================================================

    void OnDestroy()
    {
        RhythmManager.OnBeatStatic -= OnBeat;
        RhythmManager.OnBeatNumberStatic -= OnBeatNumber;
    }

    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsUI != null &&
                settingsUI.IsOpen)
            {
                settingsUI.Hide();
            }
            else if (creditsUI != null &&
                     creditsUI.IsOpen)
            {
                creditsUI.Hide();
            }
        }
    }

    // ============================================================
    // ANIMAÇÃO DE ENTRADA
    // ============================================================

    private IEnumerator EntranceAnimation()
    {
        yield return new WaitForSeconds(0.4f);

        yield return StartCoroutine(
            AnimateLogoEntrance()
        );

        yield return new WaitForSeconds(0.15f);

        yield return StartCoroutine(
            FadeGroup(
                buttonGroupCanvas,
                0f,
                1f,
                0.4f
            )
        );

        inputBlocked = false;
    }

    private IEnumerator AnimateLogoEntrance()
    {
        if (logoRect == null)
            yield break;

        float elapsed = 0f;
        float duration = 0.6f;

        Vector2 start = new Vector2(21, 275f);
        Vector2 end = new Vector2(21, 200);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    elapsed / duration
                );

            logoRect.anchoredPosition =
                Vector2.Lerp(
                    start,
                    end,
                    t
                );

            if (logoGroup != null)
                logoGroup.alpha = t;

            yield return null;
        }

        logoRect.anchoredPosition = end;

        if (logoGroup != null)
            logoGroup.alpha = 1f;
    }

    // ============================================================
    // BOTÃO PLAY
    // ============================================================

    private void OnClickPlay()
    {
        if (inputBlocked ||
            isTransitioning)
            return;

        isTransitioning = true;

        StartCoroutine(
            PlaySequence()
        );
    }

    private IEnumerator PlaySequence()
    {
        settingsUI?.Hide();
        creditsUI?.Hide();

        yield return StartCoroutine(
            LogoPunch()
        );

        yield return new WaitForSeconds(0.2f);

        SceneController.Instance?.GoToHub();
    }

    // ============================================================
    // SETTINGS
    // ============================================================

    private void OnClickSettings()
    {
        if (inputBlocked)
            return;

        creditsUI?.Hide();

        settingsUI?.Toggle();
    }

    // ============================================================
    // CREDITS
    // ============================================================

    private void OnClickCredits()
    {
        if (inputBlocked)
            return;

        settingsUI?.Hide();

        creditsUI?.Toggle();
    }

    // ============================================================
    // QUIT
    // ============================================================

    private void OnClickQuit()
    {
        if (inputBlocked)
            return;

#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;

#else

        Application.Quit();

#endif
    }

    // ============================================================
    // BEAT
    // ============================================================

    private void OnBeat()
    {
        if (beatIndicator != null)
        {
            StartCoroutine(
                BeatPunch(
                    beatIndicator,
                    beatOrigScale
                )
            );
        }
    }

    private void OnBeatNumber(int beat)
    {
        if (beat == 0 &&
            logoRect != null)
        {
            StartCoroutine(
                LogoPunch()
            );
        }
    }

    // ============================================================
    // ANIMAÇÕES
    // ============================================================

    private IEnumerator LogoPunch()
    {
        if (logoRect == null)
            yield break;

        Vector3 orig =
            logoRect.localScale;

        float t = 0f;

        while (t < 0.12f)
        {
            t += Time.deltaTime;

            logoRect.localScale =
                Vector3.Lerp(
                    orig * 1.07f,
                    orig,
                    t / 0.12f
                );

            yield return null;
        }

        logoRect.localScale = orig;
    }

    private IEnumerator BeatPunch(
        RectTransform target,
        Vector3 origScale)
    {
        float t = 0f;

        while (t < 0.12f)
        {
            t += Time.deltaTime;

            target.localScale =
                Vector3.Lerp(
                    origScale * 1.2f,
                    origScale,
                    t / 0.12f
                );

            yield return null;
        }

        target.localScale = origScale;
    }

    // ============================================================
    // FADE
    // ============================================================

    private IEnumerator FadeGroup(
        CanvasGroup g,
        float from,
        float to,
        float dur)
    {
        if (g == null)
            yield break;

        float elapsed = 0f;

        g.alpha = from;

        g.interactable =
            g.blocksRaycasts = false;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;

            g.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    elapsed / dur
                );

            yield return null;
        }

        g.alpha = to;

        g.interactable =
            g.blocksRaycasts =
                to >= 1f;
    }
}
