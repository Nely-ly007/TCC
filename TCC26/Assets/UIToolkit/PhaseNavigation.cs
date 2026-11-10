using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// POP ADVENTURE - PhaseNavigation
/// </summary>
public class PhaseNavigation : MonoBehaviour
{
    [Header("Botões de Fase (arraste na ordem 1-4)")]
    [SerializeField] private Button btnFase1;
    [SerializeField] private Button btnFase2;
    [SerializeField] private Button btnFase3;
    [SerializeField] private Button btnFase4;

    private void OnEnable()
    {
        RefreshButtons();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFragmentCollected += OnFragmentCollectedHandler;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFragmentCollected -= OnFragmentCollectedHandler;
        }
    }

    void Start()
    {
        btnFase1?.onClick.AddListener(() => LoadPhase(1));
        btnFase2?.onClick.AddListener(() => LoadPhase(2));
        btnFase3?.onClick.AddListener(() => LoadPhase(3));
        btnFase4?.onClick.AddListener(() => LoadPhase(4));

        RefreshButtons();
    }

    private void OnFragmentCollectedHandler(int fragmentId)
    {
        RefreshButtons();
    }

    private void LoadPhase(int phaseNumber)
    {
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.IsPhaseUnlocked(phaseNumber))
        {
            Debug.Log($"[PhaseNavigation] Fase {phaseNumber} ainda bloqueada.");
            return;
        }

        SceneController.Instance?.GoToPhase(phaseNumber);
    }

    public void RefreshButtons()
    {
        if (GameManager.Instance == null) return;

        SetButton(btnFase1, GameManager.Instance.IsPhaseUnlocked(1), GameManager.Instance.HasFragment(1));
        SetButton(btnFase2, GameManager.Instance.IsPhaseUnlocked(2), GameManager.Instance.HasFragment(2));
        SetButton(btnFase3, GameManager.Instance.IsPhaseUnlocked(3), GameManager.Instance.HasFragment(3));
        SetButton(btnFase4, GameManager.Instance.IsPhaseUnlocked(4), GameManager.Instance.HasFragment(4));
    }

    private void SetButton(Button btn, bool unlocked, bool hasFragment)
    {
        if (btn == null) return;

        btn.interactable = unlocked;

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = unlocked ? 1f : 0.5f;
        }

        // Procura a imagem do fragmento dentro do botão para ativar/desativar
        Transform fragmentIcon = btn.transform.Find("FragmentIcon");
        if (fragmentIcon != null)
        {
            fragmentIcon.gameObject.SetActive(hasFragment);
        }
    }
}