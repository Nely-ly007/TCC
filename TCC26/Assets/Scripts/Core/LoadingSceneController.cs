using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// POP ADVENTURE - LoadingSceneController
/// Coloque este script num GameObject vazio na LoadingScene.
/// Lê GameManager.NextScene para saber qual cena carregar.
/// </summary>
public class LoadingSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Tempo")]
    [SerializeField] private float minLoadTime  = 1.5f;  // tempo mínimo visível
    [SerializeField] private float fadeInTime   = 0.3f;
    [SerializeField] private float fadeOutTime  = 0.3f;

    private string[] loadingMessages = new string[]
    {
        "Aquecendo o vinil...",
        "Afinando as notas...",
        "Preparando o palco...",
        "Ligando os amplificadores...",
        "O show vai começar..."
    };

    void Start()
    {
        if (loadingBar  != null) loadingBar.value = 0f;
        if (loadingText != null) loadingText.text = loadingMessages[Random.Range(0, loadingMessages.Length)];
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        StartCoroutine(LoadSequence());
    }

    private IEnumerator LoadSequence()
    {
        // Fade in da loading screen
        yield return StartCoroutine(FadeCanvas(0f, 1f, fadeInTime));

        // Descobre qual cena carregar
        string target = "MainMenu";
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.NextScene))
            target = GameManager.Instance.NextScene;

        // Carrega a cena em background
        AsyncOperation op = SceneManager.LoadSceneAsync(target);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        while (!op.isDone)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (loadingBar != null)
                loadingBar.value = progress;

            // Só ativa quando carregou E passou o tempo mínimo
            if (op.progress >= 0.9f && elapsed >= minLoadTime)
            {
                if (loadingBar != null) loadingBar.value = 1f;
                yield return StartCoroutine(FadeCanvas(1f, 0f, fadeOutTime));
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}