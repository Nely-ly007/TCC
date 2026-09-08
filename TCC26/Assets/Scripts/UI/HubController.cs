using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// POP ADVENTURE - HubController v4
/// Loja migrada para UI Toolkit (ShopUIToolkit).
/// Remova os campos antigos de loja uGUI do Inspector.
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

    // ── TOOLTIP ───────────────────────────────────────────────────
    [Header("Tooltip da Fase")]
    [SerializeField] private GameObject phaseTooltip;
    [SerializeField] private TextMeshProUGUI tooltipTitle;
    [SerializeField] private TextMeshProUGUI tooltipDesc;
    [SerializeField] private TextMeshProUGUI tooltipBossName;
    [SerializeField] private TextMeshProUGUI tooltipDifficulty;
    [Header("Posição do Tooltip")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(20f, 0f);
    [SerializeField] private float tooltipScreenPadding = 15f;

    // ── TOCA-DISCOS ───────────────────────────────────────────────
    [Header("Toca-discos")]
    [SerializeField] private Transform turntableTransform;
    [SerializeField] private float turntableInteractRadius = 1.8f;
    [SerializeField] private GameObject turntablePrompt;
    [SerializeField] private AudioClip turntableOpenSFX;

    // ── MAESTRO ───────────────────────────────────────────────────
    [Header("Maestro")]
    [SerializeField] private Transform maestroTransform;
    [SerializeField] private float maestroInteractRadius = 2f;
    [SerializeField] private GameObject maestroPrompt;
    [SerializeField] private TextMeshProUGUI maestroPromptText;

    // ── DIÁLOGO ───────────────────────────────────────────────────
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

    // ── LOJA (UI TOOLKIT) ─────────────────────────────────────────
    [Header("Loja de Upgrades (UI Toolkit)")]
    [Tooltip("Arraste aqui o GameObject 'ShopUI' que tem o UIDocument + ShopUIToolkit")]
    [SerializeField] private ShopUIToolkit shopUI;

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
    private bool mapOpen      = false;
    private bool shopOpen     = false;
    private bool dialogueOpen = false;
    private bool isNearTurntable = false;
    private bool isNearMaestro   = false;
    private int  currentDialogueLine = 0;
    private bool isTyping = false;
    private Coroutine typewriterCoroutine;
    private AudioSource audioSource;
    private Transform player;
    private CanvasGroup tooltipCanvasGroup;

    // ── DADOS DAS FASES ───────────────────────────────────────────
    private static readonly string[] PhaseNames =
        { "Disco Fever", "The Hive", "Graveyard Groove", "Mayhem Theatre" };

    private static readonly string[] PhaseDescs =
    {
        "Luzes, cores quentes e ritmo disco.",
        "Colmeia urbana, dourado e preto.",
        "Cemitério estilizado, névoa e ritmo.",
        "Teatro gótico e dramático."
    };

    private static readonly string[] BossNames =
        { "Boss: Donna", "Boss: Queen Bee", "Boss: Zombie Jack", "Boss: Lady in Red" };

    private static readonly int[] PhaseDifficulties = { 2, 3, 4, 5 };

    private static readonly string[][] MaestroLines =
    {
        new[]{ "Bem-vindo ao Porão! Use o toca-discos para escolher uma fase.",
               "O Disco Dourado foi partido em 4 fragmentos. Boa sorte!" },
        new[]{ "Um fragmento coletado! Você está no caminho certo." },
        new[]{ "Metade do caminho! Os próximos bosses são mais difíceis.",
               "Não esqueça de gastar seus vinis na loja!" },
        new[]{ "Quase lá! Só falta um fragmento." },
        new[]{ "Você fez isso! O Disco Dourado está restaurado!" }
    };

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (phaseMapPanel  != null) phaseMapPanel.SetActive(false);
        if (dialoguePanel  != null) dialoguePanel.SetActive(false);
        if (turntablePrompt!= null) turntablePrompt.SetActive(false);
        if (maestroPrompt  != null) maestroPrompt.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);

        // Tooltip
        if (phaseTooltip != null)
        {
            phaseTooltip.SetActive(false);
            tooltipCanvasGroup = phaseTooltip.GetComponent<CanvasGroup>()
                              ?? phaseTooltip.AddComponent<CanvasGroup>();
            tooltipCanvasGroup.blocksRaycasts = false;
            tooltipCanvasGroup.interactable   = false;
        }

        // A loja UI Toolkit começa escondida via script
        // (ShopUIToolkit.OnEnable já chama Hide())
    }

    private void Start()
    {
        player = PlayerController.Instance?.transform;

        if (hubMusic != null)
            RhythmManager.Instance?.StartMusic(hubMusic, hubBPM);

        SetupPhaseButtons();
        RefreshFragmentDisplay();
        RefreshHUD();

        closeMapButton?.onClick.AddListener(ClosePhaseMap);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged += _ => RefreshHUD();
            GameManager.Instance.OnFragmentCollected += _ => RefreshFragmentDisplay();
        }

        SceneController.Instance?.FadeIn(0.5f);
    }

    private void Update()
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
            if (maestroPrompt != null) maestroPrompt.SetActive(near);
            if (maestroPromptText != null && near)
                maestroPromptText.text = "E — Falar     B — Upgrades";
            if (turntablePrompt != null && near)
                turntablePrompt.SetActive(false);
            else if (turntablePrompt != null && !near && isNearTurntable)
                turntablePrompt.SetActive(true);
        }
    }

    // ── INPUTS ───────────────────────────────────────────────────
    private void HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mapOpen)      ClosePhaseMap();
            if (shopOpen)     CloseShop();
            if (dialogueOpen) CloseDialogue();
            return;
        }

        // Toca-discos: E abre mapa
        if (Input.GetKeyDown(KeyCode.M) && isNearTurntable
            && !mapOpen && !shopOpen && !dialogueOpen)
        {
            OpenPhaseMap();
            return;
        }

        // Maestro
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
        HideTooltip();
        StartCoroutine(ClosePanelCoroutine(phaseMapPanel, phaseMapGroup));
    }

    private void SetupPhaseButtons()
    {
        if (phaseButtons == null) return;
        for (int i = 0; i < phaseButtons.Length; i++)
        {
            if (phaseButtons[i] == null) continue;
            int phaseIndex = i + 1;
            bool unlocked  = GameManager.Instance?.IsPhaseUnlocked(phaseIndex) ?? false;

            if (i < phaseLabels.Length    && phaseLabels[i] != null)
                phaseLabels[i].text = PhaseNames[i];
            if (i < phaseLockIcons.Length && phaseLockIcons[i] != null)
                phaseLockIcons[i].SetActive(!unlocked);
            if (i < phaseFragmentSlots.Length && phaseFragmentSlots[i] != null)
                phaseFragmentSlots[i].color = GameManager.Instance?.HasFragment(i) == true
                    ? fragmentActiveColor : fragmentInactiveColor;

            phaseButtons[i].interactable = unlocked;
            int idx = i;
            phaseButtons[i].onClick.AddListener(() => OnPhaseSelected(idx + 1));

            EventTrigger trigger = phaseButtons[i].gameObject.GetComponent<EventTrigger>()
                                ?? phaseButtons[i].gameObject.AddComponent<EventTrigger>();
            AddHoverEvent(trigger, idx);
        }
    }

    private void AddHoverEvent(EventTrigger trigger, int index)
    {
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowTooltip(index));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exit);
    }

    private void ShowTooltip(int i)
    {
        if (phaseTooltip == null || i < 0 || i >= PhaseNames.Length) return;
        phaseTooltip.SetActive(true);
        if (tooltipTitle      != null) tooltipTitle.text      = PhaseNames[i];
        if (tooltipDesc       != null) tooltipDesc.text       = PhaseDescs[i];
        if (tooltipBossName   != null) tooltipBossName.text   = BossNames[i];
        if (tooltipDifficulty != null) tooltipDifficulty.text = GetDifficultyText(PhaseDifficulties[i]);
        Canvas.ForceUpdateCanvases();
        if (i < phaseButtons.Length && phaseButtons[i] != null)
            PositionTooltip(phaseButtons[i].GetComponent<RectTransform>());
    }

    private void HideTooltip() => phaseTooltip?.SetActive(false);

    private string GetDifficultyText(int d)
    {
        d = Mathf.Clamp(d, 1, 5);
        string stars = "";
        for (int i = 0; i < 5; i++) stars += i < d ? "★" : "☆";
        string[] names = { "", "Muito Fácil", "Fácil", "Médio", "Difícil", "Muito Difícil" };
        return $"Dificuldade: {stars}\n{names[d]}";
    }

    private void PositionTooltip(RectTransform buttonRect)
    {
        if (phaseTooltip == null) return;
        RectTransform tooltipRect = phaseTooltip.GetComponent<RectTransform>();
        RectTransform parentRect  = tooltipRect?.parent as RectTransform;
        Canvas canvas = phaseTooltip.GetComponentInParent<Canvas>();
        if (tooltipRect == null || parentRect == null || canvas == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, buttonRect.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screenPos, cam, out Vector2 localPos)) return;

        Vector2 size   = tooltipRect.rect.size;
        Vector2 desired = localPos + new Vector2(buttonRect.rect.width * 0.5f, 0f) + tooltipOffset;
        Rect bounds    = parentRect.rect;

        if (desired.x > bounds.xMax - size.x * 0.5f - tooltipScreenPadding)
            desired = localPos - new Vector2(buttonRect.rect.width * 0.5f, 0f) - tooltipOffset;

        desired.x = Mathf.Clamp(desired.x, bounds.xMin + size.x * 0.5f + tooltipScreenPadding,
                                            bounds.xMax - size.x * 0.5f - tooltipScreenPadding);
        desired.y = Mathf.Clamp(desired.y, bounds.yMin + size.y * 0.5f + tooltipScreenPadding,
                                            bounds.yMax - size.y * 0.5f - tooltipScreenPadding);
        tooltipRect.anchoredPosition = desired;
    }

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

    // ── DIÁLOGO ───────────────────────────────────────────────────
    private void OpenDialogue()
    {
        dialogueOpen = true;
        currentDialogueLine = 0;
        int frags = Mathf.Clamp(GameManager.Instance?.FragmentsCollected ?? 0,
                                 0, MaestroLines.Length - 1);
        dialoguePanel?.SetActive(true);
        StartCoroutine(FadeGroup(dialogueGroup, 0f, 1f, 0.2f));
        if (dialogueNameText != null) dialogueNameText.text = "Maestro";
        if (dialoguePortrait != null) dialoguePortrait.sprite = portraitNormal;
        ShowDialogueLine(MaestroLines[frags][0]);
    }

    private void AdvanceDialogue()
    {
        if (isTyping) { SkipTypewriter(); return; }
        int frags = Mathf.Clamp(GameManager.Instance?.FragmentsCollected ?? 0,
                                 0, MaestroLines.Length - 1);
        currentDialogueLine++;
        if (currentDialogueLine < MaestroLines[frags].Length)
            ShowDialogueLine(MaestroLines[frags][currentDialogueLine]);
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
        int frags = Mathf.Clamp(GameManager.Instance?.FragmentsCollected ?? 0,
                                 0, MaestroLines.Length - 1);
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

    // ── LOJA (UI TOOLKIT) ─────────────────────────────────────────
    private void OpenShop()
    {
        if (shopUI == null)
        {
            Debug.LogWarning(
                "[HubController] ShopUI não atribuído no Inspector!"
            );
            return;
        }

        Debug.Log("[HubController] Abrindo Shop UI...");

        shopOpen = true;
        shopUI.Show();
    }


    private void CloseShop()
    {
        Debug.Log("[HubController] Fechando Shop UI...");

        shopOpen = false;
        shopUI?.Hide();
    }


    // ── FRAGMENTOS & HUD ─────────────────────────────────────────
    private void RefreshFragmentDisplay()
    {
        if (fragmentDisplayObjects != null)
            for (int i = 0; i < fragmentDisplayObjects.Length; i++)
            {
                if (fragmentDisplayObjects[i] == null) continue;
                var sr = fragmentDisplayObjects[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = GameManager.Instance?.HasFragment(i) == true
                        ? fragmentActiveColor : fragmentInactiveColor;
            }

        if (phaseFragmentSlots != null)
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
        while (t < dur) { t += Time.deltaTime; g.alpha = Mathf.Lerp(from, to, t / dur); yield return null; }
        g.alpha = to;
        g.interactable = g.blocksRaycasts = to >= 1f;
    }

    private IEnumerator ClosePanelCoroutine(GameObject panel, CanvasGroup group)
    {
        yield return StartCoroutine(FadeGroup(group, 1f, 0f, 0.2f));
        panel?.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (turntableTransform != null)
        { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(turntableTransform.position, turntableInteractRadius); }
        if (maestroTransform != null)
        { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(maestroTransform.position, maestroInteractRadius); }
    }
}