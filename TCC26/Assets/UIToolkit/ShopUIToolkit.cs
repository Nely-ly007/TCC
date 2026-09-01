using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// POP ADVENTURE - ShopUIToolkit
/// Controla o painel da loja via UI Toolkit.
/// 
/// Setup:
/// 1. Crie um GameObject "ShopUI" na cena Hub
/// 2. Add Component: UIDocument
/// 3. No UIDocument, arraste ShopPanel.uxml no campo "Source Asset"
/// 4. Add Component: ShopUIToolkit (este script)
/// 5. No HubController, chame ShopUIToolkit.Show() e .Hide()
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ShopUIToolkit : MonoBehaviour
{
    // ── REFERÊNCIAS ───────────────────────────────────────────────
    private UIDocument   uiDocument;
    private VisualElement root;

    // Elementos da UI
    private Label         vinylCountLabel;
    private Button        btnAmplifier;
    private Button        btnJump;
    private Button        btnVitality;
    private Label         priceAmplifier;
    private Label         priceJump;
    private Label         priceVitality;
    private VisualElement purchasedAmplifier;
    private VisualElement purchasedJump;
    private VisualElement purchasedVitality;

    // ── ESTADO ───────────────────────────────────────────────────
    public bool IsOpen { get; private set; } = false;

    // ── INICIALIZAÇÃO ─────────────────────────────────────────────
    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        // Busca elementos pelo name do UXML
        vinylCountLabel    = root.Q<Label>("vinyl-count");
        btnAmplifier       = root.Q<Button>("btn-amplifier");
        btnJump            = root.Q<Button>("btn-jump");
        btnVitality        = root.Q<Button>("btn-vitality");
        priceAmplifier     = root.Q<Label>("price-amplifier");
        priceJump          = root.Q<Label>("price-jump");
        priceVitality      = root.Q<Label>("price-vitality");
        purchasedAmplifier = root.Q<VisualElement>("purchased-amplifier");
        purchasedJump      = root.Q<VisualElement>("purchased-jump");
        purchasedVitality  = root.Q<VisualElement>("purchased-vitality");

        // Conecta botões
        btnAmplifier?.RegisterCallback<ClickEvent>(_ => BuyAmplifier());
        btnJump?.RegisterCallback<ClickEvent>(_      => BuyJump());
        btnVitality?.RegisterCallback<ClickEvent>(_  => BuyVitality());

        // Subscreve ao GameManager para atualizar em tempo real
        if (GameManager.Instance != null)
            GameManager.Instance.OnVinylCountChanged += OnVinylChanged;

        // Começa escondido
        Hide();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnVinylCountChanged -= OnVinylChanged;
    }

    // ── SHOW / HIDE ───────────────────────────────────────────────
    public void Show()
    {
        IsOpen = true;
        root.style.display = DisplayStyle.Flex;
        Refresh();
    }

    public void Hide()
    {
        IsOpen = false;
        root.style.display = DisplayStyle.None;
    }

    public void Toggle()
    {
        if (IsOpen) Hide();
        else        Show();
    }

    // ── REFRESH ───────────────────────────────────────────────────
    private void OnVinylChanged(int _) => Refresh();

    public void Refresh()
    {
        if (GameManager.Instance == null) return;

        int vinyls = GameManager.Instance.GetVinyls();

        // Saldo
        if (vinylCountLabel != null)
            vinylCountLabel.text = vinyls.ToString();

        // Amplificador
        RefreshCard(
            btnAmplifier, priceAmplifier, purchasedAmplifier,
            GameManager.Instance.HasDamageUpgrade,
            vinyls >= GameManager.DAMAGE_UPGRADE_COST,
            $"{GameManager.DAMAGE_UPGRADE_COST} Vinis");

        // Salto
        RefreshCard(
            btnJump, priceJump, purchasedJump,
            GameManager.Instance.HasJumpUpgrade,
            vinyls >= GameManager.JUMP_UPGRADE_COST,
            $"{GameManager.JUMP_UPGRADE_COST} Vinis");

        // Vitalidade
        RefreshCard(
            btnVitality, priceVitality, purchasedVitality,
            GameManager.Instance.HasVitalityUpgrade,
            vinyls >= GameManager.VITALITY_UPGRADE_COST,
            $"{GameManager.VITALITY_UPGRADE_COST} Vinis");
    }

    private void RefreshCard(
        Button btn, Label priceLabel, VisualElement purchasedOverlay,
        bool purchased, bool canAfford, string priceText)
    {
        if (btn == null) return;

        // Botão
        btn.SetEnabled(!purchased && canAfford);

        // Preço — remove classes antigas e aplica a correta
        if (priceLabel != null)
        {
            priceLabel.text = priceText;
            priceLabel.RemoveFromClassList("card-price--cant-afford");
            priceLabel.RemoveFromClassList("card-price--purchased");

            if (purchased)
                priceLabel.AddToClassList("card-price--purchased");
            else if (!canAfford)
                priceLabel.AddToClassList("card-price--cant-afford");
        }

        // Overlay "COMPRADO"
        if (purchasedOverlay != null)
        {
            if (purchased)
                purchasedOverlay.AddToClassList("purchased-overlay--visible");
            else
                purchasedOverlay.RemoveFromClassList("purchased-overlay--visible");
        }
    }

    // ── COMPRAS ───────────────────────────────────────────────────
    private void BuyAmplifier()
    {
        if (GameManager.Instance.BuyDamageUpgrade())
        {
            GameManager.Instance.SaveGame();
            Refresh();
        }
    }

    private void BuyJump()
    {
        if (GameManager.Instance.BuyJumpUpgrade())
        {
            GameManager.Instance.SaveGame();
            Refresh();
        }
    }

    private void BuyVitality()
    {
        if (GameManager.Instance.BuyVitalityUpgrade())
        {
            GameManager.Instance.SaveGame();
            Refresh();
        }
    }
}
