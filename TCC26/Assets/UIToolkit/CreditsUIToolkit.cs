using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// POP ADVENTURE - CreditsUIToolkit
/// Controla o painel de créditos via UI Toolkit.
///
/// Setup:
/// 1. Crie um GameObject "CreditsUI"
/// 2. Add Component: UIDocument → Source Asset: CreditsPanel.uxml
/// 3. Add Component: CreditsUIToolkit
/// 4. Preencha os campos no Inspector com seus dados
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class CreditsUIToolkit : MonoBehaviour
{
    [Header("Dados do Projeto — preencha aqui")]
    [SerializeField] private string teamName       = "Lily Studio";
    [SerializeField] private string disciplineName = "Nome da Disciplina";
    [SerializeField] private string institution    = "IFRN";
    [SerializeField] private string year           = "2026";

    private VisualElement root;
    private Button        btnClose;

    public bool IsOpen { get; private set; } = false;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        // Preenche os campos com os dados do Inspector
        SetLabel("team-name",          teamName);
        SetLabel("discipline-value",   disciplineName);
        SetLabel("institution-value",  institution);
        SetLabel("year-value",         year);

        btnClose = root.Q<Button>("btn-close-credits");
        btnClose?.RegisterCallback<ClickEvent>(_ => Hide());

        Hide();
    }

    // ── SHOW / HIDE ───────────────────────────────────────────────
    public void Show()
    {
        IsOpen = true;
        if (root == null) root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        IsOpen = false;
        if (root == null) return;
        root.style.display = DisplayStyle.None;
    }

    public void Toggle()
    {
        if (IsOpen) Hide(); else Show();
    }

    // ── HELPER ───────────────────────────────────────────────────
    private void SetLabel(string elementName, string text)
    {
        var label = root?.Q<Label>(elementName);
        if (label != null) label.text = text;
    }
}
