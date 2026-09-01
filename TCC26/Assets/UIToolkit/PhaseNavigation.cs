using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// POP ADVENTURE - PhaseNavigation
/// Conecta os botões de fase do PhaseMapPanel ao SceneController.
/// Adicione este script no HubManager ou em qualquer GameObject ativo na cena Hub.
/// Arraste os 4 botões no Inspector.
/// </summary>
public class PhaseNavigation : MonoBehaviour
{
    [Header("Botões de Fase (arraste na ordem 1-4)")]
    [SerializeField] private Button btnFase1;
    [SerializeField] private Button btnFase2;
    [SerializeField] private Button btnFase3;
    [SerializeField] private Button btnFase4;

    void Start()
    {
        // Conecta cada botão à cena correspondente
        btnFase1?.onClick.AddListener(() => LoadPhase(1));
        btnFase2?.onClick.AddListener(() => LoadPhase(2));
        btnFase3?.onClick.AddListener(() => LoadPhase(3));
        btnFase4?.onClick.AddListener(() => LoadPhase(4));

        // Atualiza estado inicial (bloqueado/desbloqueado)
        RefreshButtons();

        // Atualiza quando um fragmento é coletado (nova fase desbloqueada)
        if (GameManager.Instance != null)
            GameManager.Instance.OnFragmentCollected += _ => RefreshButtons();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFragmentCollected -= _ => RefreshButtons();
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

    private void RefreshButtons()
    {
        if (GameManager.Instance == null) return;

        SetButton(btnFase1, GameManager.Instance.IsPhaseUnlocked(1));
        SetButton(btnFase2, GameManager.Instance.IsPhaseUnlocked(2));
        SetButton(btnFase3, GameManager.Instance.IsPhaseUnlocked(3));
        SetButton(btnFase4, GameManager.Instance.IsPhaseUnlocked(4));
    }

    private void SetButton(Button btn, bool unlocked)
    {
        if (btn == null) return;
        btn.interactable = unlocked;

        // Muda a transparência visual para indicar bloqueado
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = unlocked ? 1f : 0.5f;
    }
}
