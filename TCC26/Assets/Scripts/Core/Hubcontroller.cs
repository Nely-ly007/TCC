using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// POP ADVENTURE - HubController v2
/// - Toca-discos: apenas proximidade + tecla E abre o mapa (sem disco girando/braço)
/// - Maestro: "E para falar" e "B para upgrades" ao se aproximar
/// - Loja integrada ao Maestro
/// </summary>
public class HubController : MonoBehaviour
{
    // ── MAPA DE FASES ─────────────────────────────────────────────
    [Header("Mapa de Fases")]
    [SerializeField] private GameObject phaseMapPanel;
    [SerializeField] private CanvasGroup phaseMapGroup;
    [SerializeField] private Button[] phaseButtons;
    [SerializeField] private GameObject[] phaseLockIcons;
    [SerializeField] private Image[] phaseFragmentSlots;
    [SerializeField] private TextMeshProUGUI[] phaseLabels;
    [SerializeField] private Button closeMapButton;

    [Header("Tooltip da Fase")]
    [SerializeField] private GameObject phaseTooltip;
    [SerializeField] private TextMeshProUGUI tooltipTitle;
    [SerializeField] private TextMeshProUGUI tooltipDesc;
    [SerializeField] private TextMeshProUGUI tooltipBossName;

    // ── TOCA-DISCOS (simplificado) ────────────────────────────────
    [Header("Toca-discos")]
    [SerializeField] private Transform turntableTransform;  // posição do objeto na cena
    [SerializeField] private float turntableInteractRadius = 1.8f;
    [SerializeField] private GameObject turntablePrompt;    // "E — Abrir Mapa"
    [SerializeField] private AudioClip turntableOpenSFX;

    // ── MAESTRO ───────────────────────────────────────────────────
    [Header("Maestro")]
    [SerializeField] private Transform maestroTransform;
    [SerializeField] private float maestroInteractRadius = 2f;
    [SerializeField] private GameObject maestroPrompt;      // painel com as duas opções
    [SerializeField] private TextMeshProUGUI maestroPromptText; // "E — Falar  |  B — Upgrades"

    // ── DIÁLOGO DO MAESTRO ────────────────────────────────────────
    [Header("Diálogo")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private TextMeshProUGUI dialogueNameText;
    [SerializeField] private TextMeshProUGUI dialogueBodyText;
    [SerializeField] private Image dialoguePortrait;
    [SerializeField] private Sprite portraitNormal;
    [SerializeField] private Sprite portraitHappy;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private float typewriterSpeed = 0.04f;

    // ── LOJA ──────────────────────────────────────────────────────
    [Header("Loja de Upgrades")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private CanvasGroup shopGroup;
    [SerializeField] private Button btnAmplifier;
    [SerializeField] private Button btnJump;
    [SerializeField] private Button btnVitality;
    [SerializeField] private TextMeshProUGUI vinylBalanceText;
    [SerializeField] private Button btnCloseShop;

    // ── FRAGMENTOS NO AMBIENTE ────────────────────────────────────
    [Header("Fragmentos no Ambiente")]
    [SerializeField] private GameObject[] fragmentDisplayObjects;
    [SerializeField] private Color fragmentInactiveColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color fragmentActiveColor   = new Color(1f, 0.85f, 0f);

    // ── HUD ───────────────────────────────────────────────────────
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI vinylCountText;
    [SerializeField] private TextMeshProUGUI fragmentCountText;

    // ── ÁUDIO ─────────────────────────────────────────────────────
    [Header("Áudio")]
    [SerializeField] private AudioClip hubMusic;
    [SerializeField] private float hubBPM = 95f;

    // ── ESTADO ───────────────────────────────────────────────────
    private bool mapOpen          = false;
    private bool shopOpen         = false;
    private bool dialogueOpen     = false;
    private bool isNearTurntable  = false;
    private bool isNearMaestro    = false;
    private int  currentDialogueLine = 0;
    private bool isTyping         = false;
    private Coroutine typewriterCoroutine;
    private AudioSource audioSource;
    private Transform player;

    // Dados das fases
    private static readonly string[] PhaseNames = {
        "Disco Fever", "The Hive", "Graveyard Groove", "Mayhem Theatre" };
    private static readonly string[] PhaseDescs = {
        "Luzes, cores quentes e ritmo disco. Boss: Donna.",
        "Colmeia urbana, dourado e preto. Boss: Queen Bee.",
        "Cemitério estilizado, névoa e ritmo. Boss: Zombie Jack.",
        "Teatro gótico e dramático. Boss: Lady in Red." };
    private static readonly string[] BossNames = {
        "Boss: Donna", "Boss: Queen Bee", "Boss: Zombie Jack", "Boss: Lady in Red" };

    // Falas do Maestro por fragmento
    private static readonly string[][] MaestroLines = {
        new[]{ "Bem-vindo ao Porão! Use o toca-discos para escolher uma fase.",
               "O Disco Dourado foi partido em 4 fragmentos. Boa sorte!" },
        new[]{ "Um fragmento coletado! Você está no caminho certo." },
        new[]{ "Metade do caminho! Os próximos bosses são mais difíceis.",
               "Não esqueça de gastar seus vinis na loja!" },
        new[]{ "Quase lá! Só falta um fragmento." },
        new[]{ "Você fez isso! O Disco Dourado está restaurado!" }
    };

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (phaseMapPanel  != null) phaseMapPanel.SetActive(false);
        if (shopPanel      != null) shopPanel.SetActive(false);
        if (dialoguePanel  != null) dialoguePanel.SetActive(false);
        if (turntablePrompt!= null) turntablePrompt.SetActive(false);
        if (maestroPrompt  != null) maestroPrompt.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    void Start()
    {
        player = PlayerController.Instance?.transform;

        if (hubMusic != null)
            RhythmManager.Instance?.StartMusic(hubMusic, hubBPM);

        SetupPhaseButtons();
        SetupShopButtons();
        RefreshFragmentDisplay();
        RefreshHUD();

        if (closeMapButton != null) closeMapButton.onClick.AddListener(ClosePhaseMap);
        if (btnCloseShop   != null) btnCloseShop.onClick.AddListener(CloseShop);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged += _ => RefreshHUD();
            GameManager.Instance.OnFragmentCollected += _ => RefreshFragmentDisplay();
        }

        SceneController.Instance?.FadeIn(0.5f);
    }

    void Update()
    {
        if (player == null) return;
        HandleTurntableProximity();
        HandleMaestroProximity();
        HandleInputs();
    }

    // ── TOCA-DISCOS ───────────────────────────────────────────────
    private void HandleTurntableProximity()
    {
        if (turntableTransform == null) return;
        float dist = Vector2.Distance(player.position, turntableTransform.position);
        bool near  = dist <= turntableInteractRadius;

        if (near != isNearTurntable)
        {
            isNearTurntable = near;
            // Só mostra o prompt do toca-discos se não estiver perto do Maestro
            if (turntablePrompt != null)
                turntablePrompt.SetActive(near && !isNearMaestro);
        }
    }

    // ── MAESTRO ───────────────────────────────────────────────────
    private void HandleMaestroProximity()
    {
        if (maestroTransform == null) return;
        float dist = Vector2.Distance(player.position, maestroTransform.position);
        bool near  = dist <= maestroInteractRadius;

        if (near != isNearMaestro)
        {
            isNearMaestro = near;

            if (maestroPrompt != null)
                maestroPrompt.SetActive(near);

            // Atualiza o texto do prompt com as duas opções
            if (maestroPromptText != null && near)
                maestroPromptText.text = "E — Falar     B — Upgrades";

            // Esconde prompt do toca-discos se Maestro está perto
            if (turntablePrompt != null && near)
                turntablePrompt.SetActive(false);
            else if (turntablePrompt != null && !near && isNearTurntable)
                turntablePrompt.SetActive(true);
        }
    }

    // ── INPUTS ───────────────────────────────────────────────────
    private void HandleInputs()
    {
        // ESC fecha tudo
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mapOpen)      ClosePhaseMap();
            if (shopOpen)     CloseShop();
            if (dialogueOpen) CloseDialogue();
            return;
        }

        // Toca-discos: E abre o mapa
        if (Input.GetKeyDown(KeyCode.E) && isNearTurntable && !mapOpen
            && !shopOpen && !dialogueOpen)
        {
            OpenPhaseMap();
            return;
        }

        // Maestro: E fala, B abre loja
        if (isNearMaestro)
        {
            if (Input.GetKeyDown(KeyCode.E) && !shopOpen)
            {
                if (dialogueOpen) AdvanceDialogue();
                else              OpenDialogue();
                return;
            }

            if (Input.GetKeyDown(KeyCode.B) && !dialogueOpen)
            {
                if (shopOpen) CloseShop();
                else          OpenShop();
                return;
            }
        }
    }

    // ── MAPA DE FASES ─────────────────────────────────────────────
    private void OpenPhaseMap()
    {
        if (mapOpen) return;
        mapOpen = true;
        if (turntableOpenSFX != null) audioSource?.PlayOneShot(turntableOpenSFX);
        phaseMapPanel?.SetActive(true);
        StartCoroutine(FadeGroup(phaseMapGroup, 0f, 1f, 0.3f));
    }

    private void ClosePhaseMap()
    {
        if (!mapOpen) return;
        mapOpen = false;
        StartCoroutine(ClosePanelCoroutine(phaseMapPanel, phaseMapGroup));
        if (phaseTooltip != null) phaseTooltip.SetActive(false);
    }

    private void SetupPhaseButtons()
    {
        if (phaseButtons == null) return;
        for (int i = 0; i < phaseButtons.Length; i++)
        {
            if (phaseButtons[i] == null) continue;
            int phaseIndex = i + 1;
            bool unlocked  = GameManager.Instance?.IsPhaseUnlocked(phaseIndex) ?? false;

            if (i < phaseLabels.Length && phaseLabels[i] != null)
                phaseLabels[i].text = PhaseNames[i];

            if (i < phaseLockIcons.Length && phaseLockIcons[i] != null)
                phaseLockIcons[i].SetActive(!unlocked);

            if (i < phaseFragmentSlots.Length && phaseFragmentSlots[i] != null)
                phaseFragmentSlots[i].color = GameManager.Instance?.HasFragment(i) == true
                    ? fragmentActiveColor : fragmentInactiveColor;

            phaseButtons[i].interactable = unlocked;
            int idx = i;
            phaseButtons[i].onClick.AddListener(() => OnPhaseSelected(idx + 1));

            // Hover tooltip
            var trigger = phaseButtons[i].gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                       ?? phaseButtons[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            AddHoverEvent(trigger, idx);
        }
    }

    private void AddHoverEvent(UnityEngine.EventSystems.EventTrigger trigger, int index)
    {
        var enter = new UnityEngine.EventSystems.EventTrigger.Entry
            { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowTooltip(index));
        trigger.triggers.Add(enter);

        var exit = new UnityEngine.EventSystems.EventTrigger.Entry
            { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exit);
    }

    private void ShowTooltip(int i)
    {
        if (phaseTooltip == null) return;
        phaseTooltip.SetActive(true);
        if (tooltipTitle    != null) tooltipTitle.text    = PhaseNames[i];
        if (tooltipDesc     != null) tooltipDesc.text     = PhaseDescs[i];
        if (tooltipBossName != null) tooltipBossName.text = BossNames[i];
    }

    private void HideTooltip() => phaseTooltip?.SetActive(false);

    private void OnPhaseSelected(int n)
    {
        if (GameManager.Instance?.IsPhaseUnlocked(n) != true) return;
        ClosePhaseMap();
        StartCoroutine(LoadPhaseDelayed(n));
    }

    private IEnumerator LoadPhaseDelayed(int n)
    {
        yield return new WaitForSeconds(0.3f);
        SceneController.Instance?.GoToPhase(n);
    }

    // ── DIÁLOGO DO MAESTRO ────────────────────────────────────────
    private void OpenDialogue()
    {
        dialogueOpen     = true;
        currentDialogueLine = 0;

        int frags = GameManager.Instance?.FragmentsCollected ?? 0;
        frags = Mathf.Clamp(frags, 0, MaestroLines.Length - 1);

        dialoguePanel?.SetActive(true);
        StartCoroutine(FadeGroup(dialogueGroup, 0f, 1f, 0.2f));
        if (dialogueNameText != null) dialogueNameText.text = "Maestro";
        if (dialoguePortrait != null) dialoguePortrait.sprite = portraitNormal;

        ShowDialogueLine(MaestroLines[frags][0]);
    }

    private void AdvanceDialogue()
    {
        if (isTyping) { SkipTypewriter(); return; }

        int frags = Mathf.Clamp(
            GameManager.Instance?.FragmentsCollected ?? 0, 0, MaestroLines.Length - 1);
        string[] lines = MaestroLines[frags];
        currentDialogueLine++;

        if (currentDialogueLine < lines.Length)
            ShowDialogueLine(lines[currentDialogueLine]);
        else
            CloseDialogue();
    }

    private void ShowDialogueLine(string text)
    {
        if (continueIndicator != null) continueIndicator.SetActive(false);
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(Typewriter(text));
    }

    private IEnumerator Typewriter(string text)
    {
        isTyping = true;
        if (dialogueBodyText != null) dialogueBodyText.text = "";
        foreach (char c in text)
        {
            if (dialogueBodyText != null) dialogueBodyText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        isTyping = false;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    private void SkipTypewriter()
    {
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        int frags = Mathf.Clamp(
            GameManager.Instance?.FragmentsCollected ?? 0, 0, MaestroLines.Length - 1);
        if (dialogueBodyText != null)
            dialogueBodyText.text = MaestroLines[frags][currentDialogueLine];
        isTyping = false;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    private void CloseDialogue()
    {
        dialogueOpen = false;
        StartCoroutine(ClosePanelCoroutine(dialoguePanel, dialogueGroup));
    }

    // ── LOJA ──────────────────────────────────────────────────────
    private void OpenShop()
    {
        shopOpen = true;
        shopPanel?.SetActive(true);
        StartCoroutine(FadeGroup(shopGroup, 0f, 1f, 0.25f));
        RefreshShopUI();
    }

    private void CloseShop()
    {
        shopOpen = false;
        StartCoroutine(ClosePanelCoroutine(shopPanel, shopGroup));
    }

    private void SetupShopButtons()
    {
        if (btnAmplifier != null)
            btnAmplifier.onClick.AddListener(() =>
            { if (GameManager.Instance.BuyDamageUpgrade()) RefreshShopUI(); });

        if (btnJump != null)
            btnJump.onClick.AddListener(() =>
            { if (GameManager.Instance.BuyJumpUpgrade()) RefreshShopUI(); });

        if (btnVitality != null)
            btnVitality.onClick.AddListener(() =>
            { if (GameManager.Instance.BuyVitalityUpgrade()) RefreshShopUI(); });
    }

    private void RefreshShopUI()
    {
        if (GameManager.Instance == null) return;
        int v = GameManager.Instance.GetVinyls();
        if (vinylBalanceText != null) vinylBalanceText.text = $"Vinis: {v}";

        if (btnAmplifier != null)
            btnAmplifier.interactable = !GameManager.Instance.HasDamageUpgrade
                                     && v >= GameManager.DAMAGE_UPGRADE_COST;
        if (btnJump != null)
            btnJump.interactable      = !GameManager.Instance.HasJumpUpgrade
                                     && v >= GameManager.JUMP_UPGRADE_COST;
        if (btnVitality != null)
            btnVitality.interactable  = !GameManager.Instance.HasVitalityUpgrade
                                     && v >= GameManager.VITALITY_UPGRADE_COST;
    }

    // ── FRAGMENTOS & HUD ─────────────────────────────────────────
    private void RefreshFragmentDisplay()
    {
        if (fragmentDisplayObjects == null) return;
        for (int i = 0; i < fragmentDisplayObjects.Length; i++)
        {
            if (fragmentDisplayObjects[i] == null) continue;
            var sr = fragmentDisplayObjects[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = GameManager.Instance?.HasFragment(i) == true
                    ? fragmentActiveColor : fragmentInactiveColor;
        }
        // Atualiza slots do mapa também
        if (phaseFragmentSlots == null) return;
        for (int i = 0; i < phaseFragmentSlots.Length; i++)
            if (phaseFragmentSlots[i] != null)
                phaseFragmentSlots[i].color = GameManager.Instance?.HasFragment(i) == true
                    ? fragmentActiveColor : fragmentInactiveColor;
    }

    private void RefreshHUD()
    {
        if (vinylCountText    != null)
            vinylCountText.text    = GameManager.Instance?.GetVinyls().ToString() ?? "0";
        if (fragmentCountText != null)
            fragmentCountText.text = $"{GameManager.Instance?.FragmentsCollected ?? 0}/4";
    }

    // ── UTILITÁRIOS ───────────────────────────────────────────────
    private IEnumerator FadeGroup(CanvasGroup g, float from, float to, float dur)
    {
        if (g == null) yield break;
        float t = 0; g.alpha = from;
        g.interactable = g.blocksRaycasts = false;
        while (t < dur) { t += Time.deltaTime; g.alpha = Mathf.Lerp(from, to, t/dur); yield return null; }
        g.alpha = to;
        g.interactable = g.blocksRaycasts = to >= 1f;
    }

    private IEnumerator ClosePanelCoroutine(GameObject panel, CanvasGroup group)
    {
        yield return StartCoroutine(FadeGroup(group, 1f, 0f, 0.2f));
        panel?.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (turntableTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(turntableTransform.position, turntableInteractRadius);
        }
        if (maestroTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(maestroTransform.position, maestroInteractRadius);
        }
    }
}