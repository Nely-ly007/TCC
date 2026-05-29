using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// POP ADVENTURE - SceneController
/// Gerencia todas as transições entre cenas com fade preto.
/// Adicione este script em um GameObject persistente (junto ao GameManager).
/// Precisa de um Canvas filho com um Image (painel preto full-screen).
/// </summary>
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Image fadePanel;           // Image preta que cobre a tela
    [SerializeField] private float fadeDuration = 0.6f; // duração do fade in/out

    [Header("Loading Screen (opcional)")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private float minLoadTime = 0.5f;  // evita flash rápido demais

    // Estado
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Garante que o fade começa transparente
        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
            fadePanel.gameObject.SetActive(true);
            fadePanel.raycastTarget = false;
        }
    }

    // ── API PÚBLICA ───────────────────────────────────────────────

    /// <summary>Carrega uma cena pelo nome com fade.</summary>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionTo(sceneName));
    }

    /// <summary>Carrega uma cena pelo índice com fade.</summary>
    public void LoadScene(int sceneIndex)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionTo(sceneIndex));
    }

    /// <summary>Atalhos semânticos para o GameManager chamar.</summary>
    public void GoToMainMenu()  => LoadScene("MainMenu");
    public void GoToHub()       => LoadScene("Hub");
    public void GoToPhase(int n) => LoadScene($"Phase{n}");
    public void GoToEnding()    => LoadScene("Ending");

    /// <summary>Fade-in apenas (usado ao acordar numa cena).</summary>
    public void FadeIn(float duration = -1f)
    {
        StartCoroutine(DoFade(1f, 0f, duration < 0 ? fadeDuration : duration));
    }

    // ── COROUTINES ────────────────────────────────────────────────

    private IEnumerator TransitionTo(string sceneName)
    {
        isTransitioning = true;
        fadePanel.raycastTarget = true;

        // Fade OUT (escurece)
        yield return StartCoroutine(DoFade(0f, 1f, fadeDuration));

        // Carrega a cena
        yield return StartCoroutine(LoadSceneAsync(sceneName));

        // Fade IN (clareia)
        yield return StartCoroutine(DoFade(1f, 0f, fadeDuration));

        fadePanel.raycastTarget = false;
        isTransitioning = false;
    }

    private IEnumerator TransitionTo(int sceneIndex)
    {
        isTransitioning = true;
        fadePanel.raycastTarget = true;

        yield return StartCoroutine(DoFade(0f, 1f, fadeDuration));
        yield return StartCoroutine(LoadSceneAsync(sceneIndex));
        yield return StartCoroutine(DoFade(1f, 0f, fadeDuration));

        fadePanel.raycastTarget = false;
        isTransitioning = false;
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        while (!op.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (loadingBar != null) loadingBar.value = progress;

            // Só ativa a cena quando carregou E passou o tempo mínimo
            if (op.progress >= 0.9f && elapsed >= minLoadTime)
                op.allowSceneActivation = true;

            yield return null;
        }

        if (loadingScreen != null) loadingScreen.SetActive(false);

        // Notifica que a cena foi carregada (para FadeIn automático)
        yield return null; // aguarda 1 frame para a cena inicializar
    }

    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneIndex);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        while (!op.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (loadingBar != null) loadingBar.value = progress;
            if (op.progress >= 0.9f && elapsed >= minLoadTime)
                op.allowSceneActivation = true;
            yield return null;
        }

        if (loadingScreen != null) loadingScreen.SetActive(false);
        yield return null;
    }

    private IEnumerator DoFade(float fromAlpha, float toAlpha, float duration)
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        Color c = fadePanel.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled: funciona mesmo com Time.timeScale = 0
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            fadePanel.color = c;
            yield return null;
        }

        c.a = toAlpha;
        fadePanel.color = c;
    }

    // ── FADE AUTOMÁTICO AO ENTRAR EM CENA ────────────────────────
    // Chame SceneController.Instance.FadeIn() no Start() de qualquer cena
    // para garantir que ela começa escura e clareia automaticamente.
}