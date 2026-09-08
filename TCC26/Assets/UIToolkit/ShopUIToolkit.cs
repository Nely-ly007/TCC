using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// POP ADVENTURE - ShopUIToolkit
/// Controla a loja usando UI Toolkit.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ShopUIToolkit : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // REFERÊNCIAS
    // ─────────────────────────────────────────────────────────────

    private UIDocument uiDocument;
    private VisualElement root;

    private Label vinylCountLabel;

    private Button btnAmplifier;
    private Button btnJump;
    private Button btnVitality;

    private Label priceAmplifier;
    private Label priceJump;
    private Label priceVitality;

    private VisualElement purchasedAmplifier;
    private VisualElement purchasedJump;
    private VisualElement purchasedVitality;


    // ─────────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────────

    public bool IsOpen { get; private set; }


    // ─────────────────────────────────────────────────────────────
    // INICIALIZAÇÃO
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        InitializeUI();

        if (GameManager.Instance != null)
            GameManager.Instance.OnVinylCountChanged += OnVinylChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnVinylCountChanged -= OnVinylChanged;

        UnregisterButtons();
    }


    // ─────────────────────────────────────────────────────────────
    // INICIALIZAR UI
    // ─────────────────────────────────────────────────────────────

    private void InitializeUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[ShopUIToolkit] UIDocument não encontrado!");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("[ShopUIToolkit] rootVisualElement é null!");
            return;
        }

        FindElements();

        // Começa fechado.
        root.style.display = DisplayStyle.None;
        IsOpen = false;
    }


    // ─────────────────────────────────────────────────────────────
    // ENCONTRAR ELEMENTOS
    // ─────────────────────────────────────────────────────────────

    private void FindElements()
    {
        vinylCountLabel = root.Q<Label>("vinyl-count");

        btnAmplifier = root.Q<Button>("btn-amplifier");
        btnJump      = root.Q<Button>("btn-jump");
        btnVitality  = root.Q<Button>("btn-vitality");

        priceAmplifier = root.Q<Label>("price-amplifier");
        priceJump      = root.Q<Label>("price-jump");
        priceVitality  = root.Q<Label>("price-vitality");

        purchasedAmplifier = root.Q<VisualElement>("purchased-amplifier");
        purchasedJump      = root.Q<VisualElement>("purchased-jump");
        purchasedVitality  = root.Q<VisualElement>("purchased-vitality");

        // Verificação para facilitar encontrar erro no UXML.
        Debug.Log(
            $"[ShopUIToolkit] UI encontrada: " +
            $"Vinyl={vinylCountLabel != null}, " +
            $"Amplifier={btnAmplifier != null}, " +
            $"Jump={btnJump != null}, " +
            $"Vitality={btnVitality != null}"
        );

        RegisterButtons();
    }


    // ─────────────────────────────────────────────────────────────
    // BOTÕES
    // ─────────────────────────────────────────────────────────────

    private void RegisterButtons()
    {
        btnAmplifier?.RegisterCallback<ClickEvent>(OnAmplifierClicked);
        btnJump?.RegisterCallback<ClickEvent>(OnJumpClicked);
        btnVitality?.RegisterCallback<ClickEvent>(OnVitalityClicked);
    }

    private void UnregisterButtons()
    {
        btnAmplifier?.UnregisterCallback<ClickEvent>(OnAmplifierClicked);
        btnJump?.UnregisterCallback<ClickEvent>(OnJumpClicked);
        btnVitality?.UnregisterCallback<ClickEvent>(OnVitalityClicked);
    }

    private void OnAmplifierClicked(ClickEvent evt)
    {
        BuyAmplifier();
    }

    private void OnJumpClicked(ClickEvent evt)
    {
        BuyJump();
    }

    private void OnVitalityClicked(ClickEvent evt)
    {
        BuyVitality();
    }


    // ─────────────────────────────────────────────────────────────
    // ABRIR / FECHAR
    // ─────────────────────────────────────────────────────────────

    public void Show()
    {
        Debug.Log("[ShopUIToolkit] Show() chamado.");

        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[ShopUIToolkit] UIDocument não encontrado!");
            return;
        }

        if (root == null)
        {
            root = uiDocument.rootVisualElement;

            if (root == null)
            {
                Debug.LogError(
                    "[ShopUIToolkit] Não foi possível obter rootVisualElement."
                );
                return;
            }

            FindElements();
        }

        IsOpen = true;

        root.style.display = DisplayStyle.Flex;

        Refresh();

        Debug.Log("[ShopUIToolkit] Loja ABERTA.");
    }


    public void Hide()
    {
        Debug.Log("[ShopUIToolkit] Hide() chamado.");

        IsOpen = false;

        if (root != null)
            root.style.display = DisplayStyle.None;
    }


    // ─────────────────────────────────────────────────────────────
    // REFRESH
    // ─────────────────────────────────────────────────────────────

    private void OnVinylChanged(int _)
    {
        if (IsOpen)
            Refresh();
    }


    public void Refresh()
    {
        if (GameManager.Instance == null)
            return;

        int vinyls = GameManager.Instance.GetVinyls();

        if (vinylCountLabel != null)
            vinylCountLabel.text = vinyls.ToString();

        RefreshCard(
            btnAmplifier,
            priceAmplifier,
            purchasedAmplifier,
            GameManager.Instance.HasDamageUpgrade,
            vinyls >= GameManager.DAMAGE_UPGRADE_COST,
            $"{GameManager.DAMAGE_UPGRADE_COST} Vinis"
        );

        RefreshCard(
            btnJump,
            priceJump,
            purchasedJump,
            GameManager.Instance.HasJumpUpgrade,
            vinyls >= GameManager.JUMP_UPGRADE_COST,
            $"{GameManager.JUMP_UPGRADE_COST} Vinis"
        );

        RefreshCard(
            btnVitality,
            priceVitality,
            purchasedVitality,
            GameManager.Instance.HasVitalityUpgrade,
            vinyls >= GameManager.VITALITY_UPGRADE_COST,
            $"{GameManager.VITALITY_UPGRADE_COST} Vinis"
        );
    }


    private void RefreshCard(
        Button btn,
        Label priceLabel,
        VisualElement purchasedOverlay,
        bool purchased,
        bool canAfford,
        string priceText)
    {
        if (btn == null)
            return;

        btn.SetEnabled(!purchased && canAfford);

        if (priceLabel != null)
        {
            priceLabel.text = priceText;

            priceLabel.RemoveFromClassList("card-price--cant-afford");
            priceLabel.RemoveFromClassList("card-price--purchased");

            if (purchased)
            {
                priceLabel.AddToClassList("card-price--purchased");
            }
            else if (!canAfford)
            {
                priceLabel.AddToClassList("card-price--cant-afford");
            }
        }

        if (purchasedOverlay != null)
        {
            if (purchased)
            {
                purchasedOverlay.AddToClassList(
                    "purchased-overlay--visible"
                );
            }
            else
            {
                purchasedOverlay.RemoveFromClassList(
                    "purchased-overlay--visible"
                );
            }
        }
    }


    // ─────────────────────────────────────────────────────────────
    // COMPRAS
    // ─────────────────────────────────────────────────────────────

    private void BuyAmplifier()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.BuyDamageUpgrade())
        {
            GameManager.Instance.SaveGame();
            Refresh();
        }
    }


    private void BuyJump()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.BuyJumpUpgrade())
        {
            GameManager.Instance.SaveGame();
            Refresh();
        }
    }


    private void BuyVitality()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.BuyVitalityUpgrade())
        {
            GameManager.Instance.SaveGame();
            Refresh();
        }
    }
}
