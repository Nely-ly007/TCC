using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// POP ADVENTURE - SceneController (simplificado)
/// Agora apenas delega para GameManager + LoadingScene.
/// Mantém a API pública para não quebrar chamadas existentes.
/// </summary>
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API PÚBLICA ───────────────────────────────────────────────

    public void GoToMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
        else
            SceneManager.LoadScene("LoadingScene");
    }

    public void GoToHub()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadHub();
        else
            SceneManager.LoadScene("LoadingScene");
    }

    public void GoToPhase(int n)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadPhase(n);
        else
            SceneManager.LoadScene("LoadingScene");
    }

    public void GoToEnding()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NextScene = "Ending";
            SceneManager.LoadScene("LoadingScene");
        }
        else
        {
            SceneManager.LoadScene("Ending");
        }
    }

    // Mantido para compatibilidade com MainMenuController
    public void FadeIn(float duration = 0.5f)
    {
        // Fade agora é feito pela LoadingScene — este método não precisa fazer nada
    }

    public void LoadScene(string sceneName)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NextScene = sceneName;
            SceneManager.LoadScene("LoadingScene");
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}