using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// POP ADVENTURE - HubUpgradeShop
/// Loja de upgrades no Hub (Porão).
/// 3 upgrades disponíveis conforme GDD.
/// </summary>
public class HubUpgradeShop : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeButton
    {
        public Button button;
        public TextMeshProUGUI priceText;
        public Image purchasedOverlay;
        public TextMeshProUGUI descriptionText;
    }

    [Header("Botões de Upgrade")]
    [SerializeField] private UpgradeButton amplifierUpgrade;   // +2 dano, 25 vinis
    [SerializeField] private UpgradeButton jumpUpgrade;         // +10% pulo, 30 vinis
    [SerializeField] private UpgradeButton vitalityUpgrade;     // +20 HP, 40 vinis

    [Header("UI Geral")]
    [SerializeField] private TextMeshProUGUI vinylBalanceText;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private AudioClip purchaseSFX;
    [SerializeField] private AudioClip errorSFX;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Configura textos
        if (amplifierUpgrade.descriptionText != null)
            amplifierUpgrade.descriptionText.text =
                $"AMPLIFICADOR\n+2 Dano\n{GameManager.DAMAGE_UPGRADE_COST} Vinis";

        if (jumpUpgrade.descriptionText != null)
            jumpUpgrade.descriptionText.text =
                $"SALTO HARMÔNICO\n+10% Pulo\n{GameManager.JUMP_UPGRADE_COST} Vinis";

        if (vitalityUpgrade.descriptionText != null)
            vitalityUpgrade.descriptionText.text =
                $"VITALIDADE\n+20 HP Máx\n{GameManager.VITALITY_UPGRADE_COST} Vinis";

        // Associa botões
        amplifierUpgrade.button?.onClick.AddListener(BuyAmplifier);
        jumpUpgrade.button?.onClick.AddListener(BuyJump);
        vitalityUpgrade.button?.onClick.AddListener(BuyVitality);

        // Atualiza visual
        GameManager.Instance.OnVinylCountChanged += _ => RefreshUI();
        RefreshUI();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnVinylCountChanged -= _ => RefreshUI();
    }

    public void OpenShop() => shopPanel?.SetActive(true);
    public void CloseShop() => shopPanel?.SetActive(false);

    private void BuyAmplifier()
    {
        bool success = GameManager.Instance.BuyDamageUpgrade();
        HandlePurchaseResult(success);
    }

    private void BuyJump()
    {
        bool success = GameManager.Instance.BuyJumpUpgrade();
        HandlePurchaseResult(success);
    }

    private void BuyVitality()
    {
        bool success = GameManager.Instance.BuyVitalityUpgrade();
        HandlePurchaseResult(success);
    }

    private void HandlePurchaseResult(bool success)
    {
        if (success)
        {
            if (purchaseSFX != null) audioSource?.PlayOneShot(purchaseSFX);
            GameManager.Instance?.SaveGame();
        }
        else
        {
            if (errorSFX != null) audioSource?.PlayOneShot(errorSFX);
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (GameManager.Instance == null) return;

        int vinyls = GameManager.Instance.GetVinyls();

        if (vinylBalanceText != null)
            vinylBalanceText.text = $"Vinis: {vinyls}";

        // Amplificador
        bool ownsDamage = GameManager.Instance.HasDamageUpgrade;
        SetUpgradeState(amplifierUpgrade,
            ownsDamage,
            vinyls >= GameManager.DAMAGE_UPGRADE_COST);

        // Salto
        bool ownsJump = GameManager.Instance.HasJumpUpgrade;
        SetUpgradeState(jumpUpgrade,
            ownsJump,
            vinyls >= GameManager.JUMP_UPGRADE_COST);

        // Vitalidade
        bool ownsVitality = GameManager.Instance.HasVitalityUpgrade;
        SetUpgradeState(vitalityUpgrade,
            ownsVitality,
            vinyls >= GameManager.VITALITY_UPGRADE_COST);
    }

    private void SetUpgradeState(UpgradeButton upgrade, bool purchased, bool canAfford)
    {
        if (upgrade.button != null)
            upgrade.button.interactable = !purchased && canAfford;

        if (upgrade.purchasedOverlay != null)
            upgrade.purchasedOverlay.enabled = purchased;

        if (upgrade.priceText != null)
            upgrade.priceText.color = canAfford ?
                Color.white : new Color(1f, 0.4f, 0.4f);
    }
}
