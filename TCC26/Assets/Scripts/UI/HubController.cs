using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// POP ADVENTURE - HubController
/// Controla o Hub, interações via TECLAS (M = Mapa, B = Loja, E = Interagir), 
/// Maestro (Diálogos completos + Portrait) e UI Toolkit.
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

    // ── TOCA-DISCOS ───────────────────────────────────────────────
    [Header("Toca-discos")]
    [SerializeField] private Transform turntableTransform;
    [SerializeField] private float turntableInteractRadius = 3.5f;
    [SerializeField] private GameObject turntablePrompt;
    [SerializeField] private AudioClip turntableOpenSFX;

    // ── MAESTRO ───────────────────────────────────────────────────
    [Header("Maestro")]
    [SerializeField] private Transform maestroTransform;
    [SerializeField] private float maestroInteractRadius = 3.5f;
    [SerializeField] private GameObject maestroPrompt;

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
    [Header("Loja de Upgrades")]
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

    private static readonly string[] PhaseNames = { "Disco Fever", "The Hive", "Graveyard Groove", "Mayhem Theatre" };
    private static readonly string[] PhaseDescs = { "Luzes disco e ritmo contagiante.", "Colmeia urbana e batidas pesadas.", "Cemitério estilizado e ritmo soturno.", "Teatro gótico e som dramático." };
    private static readonly string[] BossNames = { "Boss: Donna", "Boss: Queen Bee", "Boss: Zombie Jack", "Boss: Lady in Red" };
    private static readonly string[] PhaseDifficultiesText = { "Fácil", "Médio", "Difícil", "Muito Difícil" };

    private static readonly string[][] MaestroLines =
    {
        new[]{ "Bem-vindo ao Porão! Use o toca-discos para escolher uma fase.", "O Disco Dourado foi partido em 4 fragmentos. Boa sorte!" },
        new[]{ "Um fragmento coletado! Você está no caminho certo." },
        new[]{ "Metade do caminho! Os próximos bosses são mais difíceis.", "Não esqueça de gastar seus vinis na loja!" },
        new[]{ "Quase lá! Só falta um fragmento." },
        new[]{ "Você fez isso! O Disco Dourado está restaurado!" }
    };

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (phaseMapPanel != null) phaseMapPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (turntablePrompt != null) turntablePrompt.SetActive(false);
        if (maestroPrompt != null) maestroPrompt.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
        
        if (phaseTooltip != null)
        {
            phaseTooltip.SetActive(false);
            
            // Garante que o Tooltip NÃO bloqueie o controle do rato
            CanvasGroup cg = phaseTooltip.GetComponent<CanvasGroup>() ?? phaseTooltip.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            Graphic[] graphics = phaseTooltip.GetComponentsInChildren<Graphic>();
            foreach (Graphic g in graphics)
            {
                g.raycastTarget = false;
            }
        }

        if (shopUI == null) shopUI = FindFirstObjectByType<ShopUIToolkit>();
    }

    private void Start()
    {
        FindPlayerReference();

        if (hubMusic != null && RhythmManager.Instance != null)
        {
            RhythmManager.Instance.StartMusic(hubMusic, hubBPM);
        }

        if (closeMapButton != null)
        {
            closeMapButton.onClick.RemoveAllListeners();
            closeMapButton.onClick.AddListener(CloseMap);
        }

        if (phaseButtons != null)
        {
            for (int i = 0; i < phaseButtons.Length; i++)
            {
                int index = i;
                int phaseNumber = i + 1;

                if (phaseButtons[i] != null)
                {
                    phaseButtons[i].onClick.RemoveAllListeners();
                    phaseButtons[i].onClick.AddListener(() => LoadPhase(phaseNumber));

                    EventTrigger trigger = phaseButtons[i].gameObject.GetComponent<EventTrigger>() 
                                        ?? phaseButtons[i].gameObject.AddComponent<EventTrigger>();
                    trigger.triggers.Clear();

                    EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    entryEnter.callback.AddListener((data) => { ShowTooltip(index, phaseButtons[index].transform as RectTransform); });
                    trigger.triggers.Add(entryEnter);

                    EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                    entryExit.callback.AddListener((data) => { HideTooltip(); });
                    trigger.triggers.Add(entryExit);
                }
            }
        }

        UpdateHUD();
        UpdatePhaseMapUI();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged += _ => UpdateHUD();
            GameManager.Instance.OnFragmentCollected += _ => { UpdateHUD(); UpdatePhaseMapUI(); };
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged -= _ => UpdateHUD();
            GameManager.Instance.OnFragmentCollected -= _ => { UpdateHUD(); UpdatePhaseMapUI(); };
        }
    }

    private void Update()
    {
        CheckInteractions();
    }

    private void FindPlayerReference()
    {
        if (player != null) return;

        player = PlayerController.Instance?.transform;
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
    }

    private void CheckInteractions()
    {
        FindPlayerReference();

        if (Input.GetKeyDown(KeyCode.M) && !dialogueOpen && !shopOpen)
        {
            if (mapOpen) CloseMap();
            else OpenMap();
        }

        if (Input.GetKeyDown(KeyCode.B) && !mapOpen && !dialogueOpen)
        {
            ToggleShop();
        }

        if (player == null) return;

        if (turntableTransform != null)
        {
            float dist = Vector2.Distance(player.position, turntableTransform.position);
            isNearTurntable = dist <= turntableInteractRadius;

            if (turntablePrompt != null)
                turntablePrompt.SetActive(isNearTurntable && !mapOpen && !dialogueOpen && !shopOpen);

            if (isNearTurntable && Input.GetKeyDown(KeyCode.E) && !mapOpen && !dialogueOpen && !shopOpen)
            {
                OpenMap();
            }
        }

        if (maestroTransform != null)
        {
            float dist = Vector2.Distance(player.position, maestroTransform.position);
            isNearMaestro = dist <= maestroInteractRadius;

            if (maestroPrompt != null)
                maestroPrompt.SetActive(isNearMaestro && !dialogueOpen && !mapOpen && !shopOpen);

            if (isNearMaestro && Input.GetKeyDown(KeyCode.E) && !mapOpen && !shopOpen)
            {
                if (!dialogueOpen) StartDialogue();
                else AdvanceDialogue();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mapOpen) CloseMap();
            if (shopOpen) CloseShop();
            if (dialogueOpen) EndDialogue();
        }
    }

    private void ToggleShop()
    {
        if (shopOpen) CloseShop();
        else OpenShop();
    }

    public void OpenShop()
    {
        if (shopUI == null) shopUI = FindFirstObjectByType<ShopUIToolkit>();

        if (shopUI != null)
        {
            shopOpen = true;
            shopUI.Show();
        }
    }

    public void CloseShop()
    {
        if (shopUI != null)
        {
            shopOpen = false;
            shopUI.Hide();
        }
    }

    private void LoadPhase(int phaseNumber)
    {
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.IsPhaseUnlocked(phaseNumber))
        {
            Debug.Log($"[HubController] Fase {phaseNumber} bloqueada.");
            return;
        }

        CloseMap();
        GameManager.Instance.LoadPhase(phaseNumber);
    }

    public void UpdatePhaseMapUI()
    {
        if (GameManager.Instance == null) return;

        for (int i = 0; i < 4; i++)
        {
            int phaseNumber = i + 1;

            bool isUnlocked  = GameManager.Instance.IsPhaseUnlocked(phaseNumber);
            bool hasFragment = GameManager.Instance.HasFragment(phaseNumber);

            // 1. Botões
            if (phaseButtons != null && i < phaseButtons.Length && phaseButtons[i] != null)
                phaseButtons[i].interactable = isUnlocked;

            // 2. Cadeados (Só mostra se estiver bloqueado)
            if (phaseLockIcons != null && i < phaseLockIcons.Length && phaseLockIcons[i] != null)
                phaseLockIcons[i].SetActive(!isUnlocked);

            // 3. Fragmentos nos Slots da UI
            if (phaseFragmentSlots != null && i < phaseFragmentSlots.Length && phaseFragmentSlots[i] != null)
            {
                phaseFragmentSlots[i].gameObject.SetActive(hasFragment);
                phaseFragmentSlots[i].enabled = hasFragment;
            }

            // 4. Fragmentos no ambiente 2D
            if (fragmentDisplayObjects != null && i < fragmentDisplayObjects.Length && fragmentDisplayObjects[i] != null)
            {
                SpriteRenderer sr = fragmentDisplayObjects[i].GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = hasFragment ? fragmentActiveColor : fragmentInactiveColor;
                else fragmentDisplayObjects[i].SetActive(hasFragment);
            }
        }
    }

    public void OpenMap()
    {
        mapOpen = true;
        if (phaseMapPanel != null) phaseMapPanel.SetActive(true);
        if (turntablePrompt != null) turntablePrompt.SetActive(false);
        if (turntableOpenSFX != null && audioSource != null) audioSource.PlayOneShot(turntableOpenSFX);

        UpdatePhaseMapUI();
    }

    public void CloseMap()
    {
        mapOpen = false;
        if (phaseMapPanel != null) phaseMapPanel.SetActive(false);
        HideTooltip();
    }

    private void ShowTooltip(int index, RectTransform buttonRect)
    {
        if (phaseTooltip == null || index < 0 || index >= PhaseNames.Length) return;

        if (tooltipTitle != null) tooltipTitle.text = PhaseNames[index];
        if (tooltipDesc != null) tooltipDesc.text = PhaseDescs[index];
        if (tooltipBossName != null) tooltipBossName.text = BossNames[index];
        if (tooltipDifficulty != null) tooltipDifficulty.text = $"Dificuldade:\n{PhaseDifficultiesText[index]}";

        if (buttonRect != null)
        {
            phaseTooltip.transform.position = buttonRect.position + (Vector3)tooltipOffset;
        }

        phaseTooltip.SetActive(true);
    }

    private void HideTooltip()
    {
        if (phaseTooltip != null) phaseTooltip.SetActive(false);
    }

    private void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        if (vinylCountText != null) vinylCountText.text = GameManager.Instance.GetVinyls().ToString();
        if (fragmentCountText != null) fragmentCountText.text = $"{GameManager.Instance.FragmentsCollected}/4";
    }

    // ── DIÁLOGO DO MAESTRO ───────────────────────────────────────
    private void StartDialogue()
    {
        dialogueOpen = true;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (maestroPrompt != null) maestroPrompt.SetActive(false);

        if (dialogueNameText != null) dialogueNameText.text = "Maestro";

        int frags = GameManager.Instance != null ? GameManager.Instance.FragmentsCollected : 0;
        frags = Mathf.Clamp(frags, 0, MaestroLines.Length - 1);

        if (dialoguePortrait != null)
        {
            dialoguePortrait.gameObject.SetActive(true);
            dialoguePortrait.sprite = (frags >= 2 && portraitHappy != null) ? portraitHappy : portraitNormal;
        }

        currentDialogueLine = 0;
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypeSentence(MaestroLines[frags][currentDialogueLine]));
    }

    private void AdvanceDialogue()
    {
        int frags = GameManager.Instance != null ? GameManager.Instance.FragmentsCollected : 0;
        frags = Mathf.Clamp(frags, 0, MaestroLines.Length - 1);

        if (isTyping)
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            if (dialogueBodyText != null) dialogueBodyText.text = MaestroLines[frags][currentDialogueLine];

            isTyping = false;
            if (continueIndicator != null) continueIndicator.SetActive(true);
            return;
        }

        currentDialogueLine++;

        if (currentDialogueLine < MaestroLines[frags].Length)
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypeSentence(MaestroLines[frags][currentDialogueLine]));
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        dialogueOpen = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);
        if (dialogueBodyText != null) dialogueBodyText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            if (dialogueBodyText != null) dialogueBodyText.text += letter;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }
}