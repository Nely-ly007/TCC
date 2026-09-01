using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// POP ADVENTURE - HubController v3
///
/// - Toca-discos: proximidade + tecla E abre o mapa
/// - Maestro: "E para falar" e "B para upgrades"
/// - Loja integrada ao Maestro
/// - Mapa de fases com tooltip dinâmico
/// - Tooltip reposicionado automaticamente para cada fase
/// - Tooltip com nome, descrição, boss e dificuldade
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


    // ── TOOLTIP ──────────────────────────────────────────────────

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
    [Header("Loja UI Toolkit")]
    [SerializeField] private ShopUIToolkit shopUI;
    
    [SerializeField] private Button btnCloseShop;


    // ── FRAGMENTOS NO AMBIENTE ────────────────────────────────────

    [Header("Fragmentos no Ambiente")]
    [SerializeField] private GameObject[] fragmentDisplayObjects;

    [SerializeField] private Color fragmentInactiveColor =
        new Color(0.3f, 0.3f, 0.3f);

    [SerializeField] private Color fragmentActiveColor =
        new Color(1f, 0.85f, 0f);


    // ── HUD ───────────────────────────────────────────────────────

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI vinylCountText;
    [SerializeField] private TextMeshProUGUI fragmentCountText;


    // ── ÁUDIO ─────────────────────────────────────────────────────

    [Header("Áudio")]
    [SerializeField] private AudioClip hubMusic;
    [SerializeField] private float hubBPM = 95f;


    // ── ESTADO ───────────────────────────────────────────────────

    private bool mapOpen = false;
    private bool shopOpen = false;
    private bool dialogueOpen = false;

    private bool isNearTurntable = false;
    private bool isNearMaestro = false;

    private int currentDialogueLine = 0;

    private bool isTyping = false;
    private Coroutine typewriterCoroutine;

    private AudioSource audioSource;
    private Transform player;


    // ── TOOLTIP ──────────────────────────────────────────────────

    private CanvasGroup tooltipCanvasGroup;


    // ── DADOS DAS FASES ──────────────────────────────────────────

    private static readonly string[] PhaseNames =
    {
        "Disco Fever",
        "The Hive",
        "Graveyard Groove",
        "Mayhem Theatre"
    };


    private static readonly string[] PhaseDescs =
    {
        "Luzes, cores quentes e ritmo disco.",
        "Colmeia urbana, dourado e preto.",
        "Cemitério estilizado, névoa e ritmo.",
        "Teatro gótico e dramático."
    };


    private static readonly string[] BossNames =
    {
        "Boss: Donna",
        "Boss: Queen Bee",
        "Boss: Zombie Jack",
        "Boss: Lady in Red"
    };


    // Dificuldade de cada fase.
    //
    // 1 = muito fácil
    // 2 = fácil
    // 3 = médio
    // 4 = difícil
    // 5 = muito difícil
    //
    private static readonly int[] PhaseDifficulties =
    {
        2,
        3,
        4,
        5
    };


    // ── FALAS DO MAESTRO ─────────────────────────────────────────

    private static readonly string[][] MaestroLines =
    {
        new[]
        {
            "Bem-vindo ao Porão! Use o toca-discos para escolher uma fase.",
            "O Disco Dourado foi partido em 4 fragmentos. Boa sorte!"
        },

        new[]
        {
            "Um fragmento coletado! Você está no caminho certo."
        },

        new[]
        {
            "Metade do caminho! Os próximos bosses são mais difíceis.",
            "Não esqueça de gastar seus vinis na loja!"
        },

        new[]
        {
            "Quase lá! Só falta um fragmento."
        },

        new[]
        {
            "Você fez isso! O Disco Dourado está restaurado!"
        }
    };


    // ─────────────────────────────────────────────────────────────
    // AWAKE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (phaseMapPanel != null)
            phaseMapPanel.SetActive(false);

        if (shopUI != null)
            shopUI.Hide();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (turntablePrompt != null)
            turntablePrompt.SetActive(false);

        if (maestroPrompt != null)
            maestroPrompt.SetActive(false);

        if (continueIndicator != null)
            continueIndicator.SetActive(false);


        // Configura o tooltip.

        if (phaseTooltip != null)
        {
            phaseTooltip.SetActive(false);

            tooltipCanvasGroup =
                phaseTooltip.GetComponent<CanvasGroup>();

            if (tooltipCanvasGroup == null)
            {
                tooltipCanvasGroup =
                    phaseTooltip.AddComponent<CanvasGroup>();
            }

            // O tooltip não bloqueia o mouse.
            //
            // Isso é importante porque o tooltip pode ficar
            // em cima ou próximo do botão da fase.

            tooltipCanvasGroup.blocksRaycasts = false;
            tooltipCanvasGroup.interactable = false;
        }
    }


    // ─────────────────────────────────────────────────────────────
    // START
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        player = PlayerController.Instance?.transform;


        if (hubMusic != null)
        {
            RhythmManager.Instance?.StartMusic(
                hubMusic,
                hubBPM
            );
        }


        SetupPhaseButtons();
        SetupShopButtons();

        RefreshFragmentDisplay();
        RefreshHUD();


        if (closeMapButton != null)
        {
            closeMapButton.onClick.AddListener(
                ClosePhaseMap
            );
        }


        if (btnCloseShop != null)
        {
            btnCloseShop.onClick.AddListener(
                CloseShop
            );
        }


        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVinylCountChanged +=
                _ => RefreshHUD();

            GameManager.Instance.OnFragmentCollected +=
                _ => RefreshFragmentDisplay();
        }


        SceneController.Instance?.FadeIn(0.5f);
    }


    // ─────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (player == null)
            return;

        HandleTurntableProximity();
        HandleMaestroProximity();
        HandleInputs();
    }


    // ─────────────────────────────────────────────────────────────
    // TOCA-DISCOS
    // ─────────────────────────────────────────────────────────────

    private void HandleTurntableProximity()
    {
        if (turntableTransform == null)
            return;


        float dist =
            Vector2.Distance(
                player.position,
                turntableTransform.position
            );


        bool near =
            dist <= turntableInteractRadius;


        if (near != isNearTurntable)
        {
            isNearTurntable = near;


            // Só mostra o prompt do toca-discos
            // se não estiver perto do Maestro.

            if (turntablePrompt != null)
            {
                turntablePrompt.SetActive(
                    near && !isNearMaestro
                );
            }
        }
    }


    // ─────────────────────────────────────────────────────────────
    // MAESTRO
    // ─────────────────────────────────────────────────────────────

    private void HandleMaestroProximity()
    {
        if (maestroTransform == null)
            return;


        float dist =
            Vector2.Distance(
                player.position,
                maestroTransform.position
            );


        bool near =
            dist <= maestroInteractRadius;


        if (near != isNearMaestro)
        {
            isNearMaestro = near;


            if (maestroPrompt != null)
                maestroPrompt.SetActive(near);


            if (maestroPromptText != null && near)
            {
                maestroPromptText.text =
                    "E — Falar     B — Upgrades";
            }


            // Esconde prompt do toca-discos
            // quando o Maestro está próximo.

            if (turntablePrompt != null && near)
            {
                turntablePrompt.SetActive(false);
            }
            else if (
                turntablePrompt != null &&
                !near &&
                isNearTurntable)
            {
                turntablePrompt.SetActive(true);
            }
        }
    }


    // ─────────────────────────────────────────────────────────────
    // INPUTS
    // ─────────────────────────────────────────────────────────────

    private void HandleInputs()
    {
        // ESC fecha tudo.

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mapOpen)
                ClosePhaseMap();

            if (shopOpen)
                CloseShop();

            if (dialogueOpen)
                CloseDialogue();

            return;
        }


        // Toca-discos: E abre mapa.

        if (
            Input.GetKeyDown(KeyCode.E) &&
            isNearTurntable &&
            !mapOpen &&
            !shopOpen &&
            !dialogueOpen
        )
        {
            OpenPhaseMap();
            return;
        }


        // Maestro.

        if (isNearMaestro)
        {
            // E = falar

            if (
                Input.GetKeyDown(KeyCode.E) &&
                !shopOpen
            )
            {
                if (dialogueOpen)
                    AdvanceDialogue();
                else
                    OpenDialogue();

                return;
            }


            // B = upgrades

            if (Input.GetKeyDown(KeyCode.B) && !dialogueOpen)
            {
                shopUI.Toggle();
                return;
            }
            {
                if (shopOpen)
                    CloseShop();
                else
                    OpenShop();

                return;
            }
        }
    }


    // ─────────────────────────────────────────────────────────────
    // MAPA DE FASES
    // ─────────────────────────────────────────────────────────────

    private void OpenPhaseMap()
    {
        if (mapOpen)
            return;


        mapOpen = true;


        if (turntableOpenSFX != null)
        {
            audioSource?.PlayOneShot(
                turntableOpenSFX
            );
        }


        phaseMapPanel?.SetActive(true);


        StartCoroutine(
            FadeGroup(
                phaseMapGroup,
                0f,
                1f,
                0.3f
            )
        );
    }


    private void ClosePhaseMap()
    {
        if (!mapOpen)
            return;


        mapOpen = false;


        HideTooltip();


        StartCoroutine(
            ClosePanelCoroutine(
                phaseMapPanel,
                phaseMapGroup
            )
        );
    }


    // ─────────────────────────────────────────────────────────────
    // CONFIGURAÇÃO DOS BOTÕES DAS FASES
    // ─────────────────────────────────────────────────────────────

    private void SetupPhaseButtons()
    {
        if (phaseButtons == null)
            return;


        for (int i = 0; i < phaseButtons.Length; i++)
        {
            if (phaseButtons[i] == null)
                continue;


            int phaseIndex = i + 1;


            bool unlocked =
                GameManager.Instance?.IsPhaseUnlocked(
                    phaseIndex
                ) ?? false;


            // ── NOME ─────────────────────────────────────────────

            if (
                i < phaseLabels.Length &&
                phaseLabels[i] != null &&
                i < PhaseNames.Length
            )
            {
                phaseLabels[i].text =
                    PhaseNames[i];
            }


            // ── CADEADO ─────────────────────────────────────────

            if (
                i < phaseLockIcons.Length &&
                phaseLockIcons[i] != null
            )
            {
                phaseLockIcons[i].SetActive(
                    !unlocked
                );
            }


            // ── FRAGMENTO ───────────────────────────────────────

            if (
                i < phaseFragmentSlots.Length &&
                phaseFragmentSlots[i] != null
            )
            {
                phaseFragmentSlots[i].color =
                    GameManager.Instance?.HasFragment(i) == true
                        ? fragmentActiveColor
                        : fragmentInactiveColor;
            }


            // ── BOTÃO ───────────────────────────────────────────

            phaseButtons[i].interactable =
                unlocked;


            int idx = i;


            phaseButtons[i].onClick.AddListener(
                () => OnPhaseSelected(idx + 1)
            );


            // ── HOVER ───────────────────────────────────────────

            EventTrigger trigger =
                phaseButtons[i]
                    .gameObject
                    .GetComponent<EventTrigger>();


            if (trigger == null)
            {
                trigger =
                    phaseButtons[i]
                        .gameObject
                        .AddComponent<EventTrigger>();
            }


            AddHoverEvent(
                trigger,
                idx
            );
        }
    }


    // ─────────────────────────────────────────────────────────────
    // EVENTOS DE HOVER
    // ─────────────────────────────────────────────────────────────

    private void AddHoverEvent(
        EventTrigger trigger,
        int index
    )
    {
        // Pointer Enter

        EventTrigger.Entry enter =
            new EventTrigger.Entry();

        enter.eventID =
            EventTriggerType.PointerEnter;


        enter.callback.AddListener(
            _ => ShowTooltip(index)
        );


        trigger.triggers.Add(enter);


        // Pointer Exit

        EventTrigger.Entry exit =
            new EventTrigger.Entry();

        exit.eventID =
            EventTriggerType.PointerExit;


        exit.callback.AddListener(
            _ => HideTooltip()
        );


        trigger.triggers.Add(exit);
    }


    // ─────────────────────────────────────────────────────────────
    // MOSTRA TOOLTIP
    // ─────────────────────────────────────────────────────────────

    private void ShowTooltip(int i)
    {
        if (phaseTooltip == null)
            return;


        if (i < 0 || i >= PhaseNames.Length)
            return;


        // Mostra tooltip primeiro para que
        // o RectTransform tenha o tamanho correto.

        phaseTooltip.SetActive(true);


        // ── TÍTULO ──────────────────────────────────────────────

        if (tooltipTitle != null)
        {
            tooltipTitle.text =
                PhaseNames[i];
        }


        // ── DESCRIÇÃO ────────────────────────────────────────────

        if (tooltipDesc != null)
        {
            tooltipDesc.text =
                PhaseDescs[i];
        }


        // ── BOSS ────────────────────────────────────────────────

        if (tooltipBossName != null)
        {
            tooltipBossName.text =
                BossNames[i];
        }


        // ── DIFICULDADE ─────────────────────────────────────────

        if (tooltipDifficulty != null)
        {
            tooltipDifficulty.text =
                GetDifficultyText(
                    PhaseDifficulties[i]
                );
        }


        // Força o Unity a atualizar o layout
        // antes de calcular o tamanho.

        Canvas.ForceUpdateCanvases();


        // ── POSICIONAMENTO ───────────────────────────────────────

        if (
            i < phaseButtons.Length &&
            phaseButtons[i] != null
        )
        {
            PositionTooltip(
                phaseButtons[i].GetComponent<RectTransform>()
            );
        }
    }


    // ─────────────────────────────────────────────────────────────
    // TEXTO DA DIFICULDADE
    // ─────────────────────────────────────────────────────────────

    private string GetDifficultyText(int difficulty)
    {
        difficulty =
            Mathf.Clamp(
                difficulty,
                1,
                5
            );


        string stars = "";


        for (int i = 0; i < 5; i++)
        {
            if (i < difficulty)
                stars += "★";
            else
                stars += "☆";
        }


        string difficultyName;


        switch (difficulty)
        {
            case 1:
                difficultyName = "Muito Fácil";
                break;

            case 2:
                difficultyName = "Fácil";
                break;

            case 3:
                difficultyName = "Médio";
                break;

            case 4:
                difficultyName = "Difícil";
                break;

            case 5:
                difficultyName = "Muito Difícil";
                break;

            default:
                difficultyName = "Desconhecida";
                break;
        }


        return $"Dificuldade: {stars}\n{difficultyName}";
    }


    // ─────────────────────────────────────────────────────────────
    // POSICIONAMENTO DO TOOLTIP
    // ─────────────────────────────────────────────────────────────

    private void PositionTooltip(
        RectTransform buttonRect
    )
    {
        if (phaseTooltip == null)
            return;


        RectTransform tooltipRect =
            phaseTooltip.GetComponent<RectTransform>();


        if (tooltipRect == null)
            return;


        RectTransform parentRect =
            tooltipRect.parent as RectTransform;


        if (parentRect == null)
            return;


        Canvas canvas =
            phaseTooltip.GetComponentInParent<Canvas>();


        if (canvas == null)
            return;


        // Posição do centro do botão na tela.

        Vector2 buttonScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                GetCanvasCamera(canvas),
                buttonRect.position
            );


        // Converte a posição da tela
        // para o espaço local do painel.

        Vector2 localPosition;


        bool converted =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                buttonScreenPosition,
                GetCanvasCamera(canvas),
                out localPosition
            );


        if (!converted)
            return;


        Vector2 tooltipSize =
            tooltipRect.rect.size;


        // Começamos colocando o tooltip
        // à direita do botão.

        Vector2 desiredPosition =
            localPosition +
            new Vector2(
                buttonRect.rect.width * 0.5f,
                0f
            ) +
            tooltipOffset;


        // ── LIMITES DO PAINEL ───────────────────────────────────

        Rect parentBounds =
            parentRect.rect;


        float halfWidth =
            tooltipSize.x * 0.5f;


        float halfHeight =
            tooltipSize.y * 0.5f;


        float minX =
            parentBounds.xMin +
            halfWidth +
            tooltipScreenPadding;


        float maxX =
            parentBounds.xMax -
            halfWidth -
            tooltipScreenPadding;


        float minY =
            parentBounds.yMin +
            halfHeight +
            tooltipScreenPadding;


        float maxY =
            parentBounds.yMax -
            halfHeight -
            tooltipScreenPadding;


        // ── SE NÃO COUBER À DIREITA ──────────────────────────────
        //
        // Coloca o tooltip à esquerda do botão.

        if (desiredPosition.x > maxX)
        {
            desiredPosition =
                localPosition -
                new Vector2(
                    buttonRect.rect.width * 0.5f,
                    0f
                ) -
                tooltipOffset;
        }


        // ── CLAMP FINAL ─────────────────────────────────────────

        desiredPosition.x =
            Mathf.Clamp(
                desiredPosition.x,
                minX,
                maxX
            );


        desiredPosition.y =
            Mathf.Clamp(
                desiredPosition.y,
                minY,
                maxY
            );


        tooltipRect.anchoredPosition =
            desiredPosition;
    }


    // ─────────────────────────────────────────────────────────────
    // CÂMERA DO CANVAS
    // ─────────────────────────────────────────────────────────────

    private Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }


        return canvas.worldCamera;
    }


    // ─────────────────────────────────────────────────────────────
    // ESCONDE TOOLTIP
    // ─────────────────────────────────────────────────────────────

    private void HideTooltip()
    {
        if (phaseTooltip != null)
            phaseTooltip.SetActive(false);
    }


    // ─────────────────────────────────────────────────────────────
    // SELEÇÃO DA FASE
    // ─────────────────────────────────────────────────────────────

    private void OnPhaseSelected(int n)
    {
        if (
            GameManager.Instance?.IsPhaseUnlocked(n)
            != true
        )
        {
            return;
        }


        ClosePhaseMap();


        StartCoroutine(
            LoadPhaseDelayed(n)
        );
    }


    private IEnumerator LoadPhaseDelayed(int n)
    {
        yield return new WaitForSeconds(0.3f);

        SceneController.Instance?.GoToPhase(n);
    }


    // ─────────────────────────────────────────────────────────────
    // DIÁLOGO DO MAESTRO
    // ─────────────────────────────────────────────────────────────

    private void OpenDialogue()
    {
        dialogueOpen = true;
        currentDialogueLine = 0;


        int frags =
            GameManager.Instance?.FragmentsCollected
            ?? 0;


        frags =
            Mathf.Clamp(
                frags,
                0,
                MaestroLines.Length - 1
            );


        dialoguePanel?.SetActive(true);


        StartCoroutine(
            FadeGroup(
                dialogueGroup,
                0f,
                1f,
                0.2f
            )
        );


        if (dialogueNameText != null)
            dialogueNameText.text = "Maestro";


        if (dialoguePortrait != null)
            dialoguePortrait.sprite =
                portraitNormal;


        ShowDialogueLine(
            MaestroLines[frags][0]
        );
    }


    private void AdvanceDialogue()
    {
        if (isTyping)
        {
            SkipTypewriter();
            return;
        }


        int frags =
            Mathf.Clamp(
                GameManager.Instance?.FragmentsCollected
                ?? 0,
                0,
                MaestroLines.Length - 1
            );


        string[] lines =
            MaestroLines[frags];


        currentDialogueLine++;


        if (currentDialogueLine < lines.Length)
        {
            ShowDialogueLine(
                lines[currentDialogueLine]
            );
        }
        else
        {
            CloseDialogue();
        }
    }


    private void ShowDialogueLine(string text)
    {
        if (continueIndicator != null)
            continueIndicator.SetActive(false);


        if (typewriterCoroutine != null)
        {
            StopCoroutine(
                typewriterCoroutine
            );
        }


        typewriterCoroutine =
            StartCoroutine(
                Typewriter(text)
            );
    }


    private IEnumerator Typewriter(string text)
    {
        isTyping = true;


        if (dialogueBodyText != null)
            dialogueBodyText.text = "";


        foreach (char c in text)
        {
            if (dialogueBodyText != null)
                dialogueBodyText.text += c;


            yield return new WaitForSeconds(
                typewriterSpeed
            );
        }


        isTyping = false;


        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }


    private void SkipTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(
                typewriterCoroutine
            );
        }


        int frags =
            Mathf.Clamp(
                GameManager.Instance?.FragmentsCollected
                ?? 0,
                0,
                MaestroLines.Length - 1
            );


        if (dialogueBodyText != null)
        {
            dialogueBodyText.text =
                MaestroLines[frags][
                    currentDialogueLine
                ];
        }


        isTyping = false;


        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }


    private void CloseDialogue()
    {
        dialogueOpen = false;


        StartCoroutine(
            ClosePanelCoroutine(
                dialoguePanel,
                dialogueGroup
            )
        );
    }


    // ─────────────────────────────────────────────────────────────
    // LOJA
    // ─────────────────────────────────────────────────────────────

    private void OpenShop()
    {
        shopOpen = true;


        shopUI.Show();


        StartCoroutine(
            FadeGroup(
                shopUI,
                0f,
                1f,
                0.25f
            )
        );


        RefreshShopUI();
    }


    private void CloseShop()
    {
        shopOpen = false;


        StartCoroutine(
            ClosePanelCoroutine(
                shopUI
            )
        );
    }


    private void SetupShopButtons()
    {
        if (btnAmplifier != null)
        {
            btnAmplifier.onClick.AddListener(
                () =>
                {
                    if (
                        GameManager.Instance
                            .BuyDamageUpgrade()
                    )
                    {
                        RefreshShopUI();
                    }
                }
            );
        }


        if (btnJump != null)
        {
            btnJump.onClick.AddListener(
                () =>
                {
                    if (
                        GameManager.Instance
                            .BuyJumpUpgrade()
                    )
                    {
                        RefreshShopUI();
                    }
                }
            );
        }


        if (btnVitality != null)
        {
            btnVitality.onClick.AddListener(
                () =>
                {
                    if (
                        GameManager.Instance
                            .BuyVitalityUpgrade()
                    )
                    {
                        RefreshShopUI();
                    }
                }
            );
        }
    }


    private void RefreshShopUI()
    {
        if (GameManager.Instance == null)
            return;


        int v =
            GameManager.Instance.GetVinyls();


        if (vinylBalanceText != null)
        {
            vinylBalanceText.text =
                $"Vinis: {v}";
        }


        if (btnAmplifier != null)
        {
            btnAmplifier.interactable =
                !GameManager.Instance.HasDamageUpgrade &&
                v >= GameManager.DAMAGE_UPGRADE_COST;
        }


        if (btnJump != null)
        {
            btnJump.interactable =
                !GameManager.Instance.HasJumpUpgrade &&
                v >= GameManager.JUMP_UPGRADE_COST;
        }


        if (btnVitality != null)
        {
            btnVitality.interactable =
                !GameManager.Instance.HasVitalityUpgrade &&
                v >= GameManager.VITALITY_UPGRADE_COST;
        }
    }


    // ─────────────────────────────────────────────────────────────
    // FRAGMENTOS
    // ─────────────────────────────────────────────────────────────

    private void RefreshFragmentDisplay()
    {
        if (fragmentDisplayObjects == null)
            return;


        for (
            int i = 0;
            i < fragmentDisplayObjects.Length;
            i++
        )
        {
            if (
                fragmentDisplayObjects[i] == null
            )
            {
                continue;
            }


            SpriteRenderer sr =
                fragmentDisplayObjects[i]
                    .GetComponent<SpriteRenderer>();


            if (sr != null)
            {
                sr.color =
                    GameManager.Instance?.HasFragment(i) == true
                        ? fragmentActiveColor
                        : fragmentInactiveColor;
            }
        }


        // Atualiza slots do mapa.

        if (phaseFragmentSlots == null)
            return;


        for (
            int i = 0;
            i < phaseFragmentSlots.Length;
            i++
        )
        {
            if (
                phaseFragmentSlots[i] != null
            )
            {
                phaseFragmentSlots[i].color =
                    GameManager.Instance?.HasFragment(i) == true
                        ? fragmentActiveColor
                        : fragmentInactiveColor;
            }
        }
    }


    // ─────────────────────────────────────────────────────────────
    // HUD
    // ─────────────────────────────────────────────────────────────

    private void RefreshHUD()
    {
        if (vinylCountText != null)
        {
            vinylCountText.text =
                GameManager.Instance?
                    .GetVinyls()
                    .ToString()
                ?? "0";
        }


        if (fragmentCountText != null)
        {
            fragmentCountText.text =
                $"{GameManager.Instance?.FragmentsCollected ?? 0}/4";
        }
    }


    // ─────────────────────────────────────────────────────────────
    // UTILITÁRIOS
    // ─────────────────────────────────────────────────────────────

    private IEnumerator FadeGroup(
        ShopUIToolkit g,
        float from,
        float to,
        float dur
    )
    {
        if (g == null)
            yield break;


        float t = 0;


        g.alpha = from;


        g.interactable = false;
        g.blocksRaycasts = false;


        while (t < dur)
        {
            t += Time.deltaTime;


            g.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    t / dur
                );


            yield return null;
        }


        g.alpha = to;


        g.interactable =
            g.blocksRaycasts =
                to >= 1f;
    }


    private IEnumerator ClosePanelCoroutine(
        GameObject panel,
        CanvasGroup group
    )
    {
        yield return StartCoroutine(
            FadeGroup(
                group,
                1f,
                0f,
                0.2f
            )
        );


        panel?.SetActive(false);
    }


    // ─────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (turntableTransform != null)
        {
            Gizmos.color = Color.cyan;


            Gizmos.DrawWireSphere(
                turntableTransform.position,
                turntableInteractRadius
            );
        }


        if (maestroTransform != null)
        {
            Gizmos.color = Color.yellow;


            Gizmos.DrawWireSphere(
                maestroTransform.position,
                maestroInteractRadius
            );
        }
    }
}
