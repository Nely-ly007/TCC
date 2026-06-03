using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// POP ADVENTURE - HubController
/// Gerencia a cena Hub (O Porão):
///   - Mapa de fases interativo (4 portais + Hub central)
///   - Toca-discos dourado: animado, abre o mapa ao interagir
///   - NPC Maestro: dá dicas, reage aos fragmentos coletados
///   - Feedback visual: fragmentos do disco exibidos no ambiente
///   - Música ambiente sincronizada com o BPM do hub
/// </summary>
public class HubController : MonoBehaviour
{
    // ── MAPA DE FASES ─────────────────────────────────────────────
    [Header("Mapa de Fases")]
    [SerializeField] private GameObject phaseMapPanel;
    [SerializeField] private CanvasGroup phaseMapGroup;
    [SerializeField] private Button[] phaseButtons;         // 4 botões, um por fase
    [SerializeField] private GameObject[] phaseLockIcons;   // ícone de cadeado por fase
    [SerializeField] private Image[] phaseFragmentSlots;    // slot dourado quando coletado
    [SerializeField] private TextMeshProUGUI[] phaseLabels; // nome de cada fase
    [SerializeField] private Button closeMapButton;

    [Header("Descrição da Fase (tooltip)")]
    [SerializeField] private GameObject phaseTooltip;
    [SerializeField] private TextMeshProUGUI tooltipTitle;
    [SerializeField] private TextMeshProUGUI tooltipDesc;
    [SerializeField] private TextMeshProUGUI tooltipBossName;

    // ── TOCA-DISCOS ───────────────────────────────────────────────
    [Header("Toca-discos")]
    [SerializeField] private Transform turntableDisc;       // disco que gira
    [SerializeField] private Transform turntableArm;        // braço do toca-discos
    [SerializeField] private float discSpinSpeed = 45f;     // graus/segundo
    [SerializeField] private float armPlayAngle  = -18f;    // ângulo quando tocando
    [SerializeField] private float armIdleAngle  =   0f;

    [Header("Interação com Toca-discos")]
    [SerializeField] private GameObject interactPrompt;     // "E - Ver Mapa"
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] private AudioClip turntableStartSFX;
    [SerializeField] private AudioClip hubMusic;
    [SerializeField] private float hubBPM = 95f;

    // ── NPC MAESTRO ───────────────────────────────────────────────
    [Header("NPC Maestro")]
    [SerializeField] private GameObject maestroObject;
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private TextMeshProUGUI speechText;
    [SerializeField] private float speechDuration = 4f;
    [SerializeField] private float interactRadiusMaestro = 1.8f;

    // ── FRAGMENTOS DO DISCO NO AMBIENTE ───────────────────────────
    [Header("Display de Fragmentos (ambiente)")]
    [SerializeField] private GameObject[] fragmentDisplayObjects; // 4 objetos na cena
    [SerializeField] private Color fragmentInactiveColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color fragmentActiveColor   = new Color(1f, 0.85f, 0f);

    // ── LOJA ──────────────────────────────────────────────────────
    [Header("Loja de Upgrades")]
    [SerializeField] private GameObject shopTriggerZone;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private HubUpgradeShop upgradeShop;

    // ── HUD DO HUB ────────────────────────────────────────────────
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI vinylCountText;
    [SerializeField] private TextMeshProUGUI fragmentCountText;

    // ── ESTADO ────────────────────────────────────────────────────
    private bool mapOpen        = false;
    private bool isNearTurntable = false;
    private bool isNearMaestro  = false;
    private bool turntablePlaying = true;
    private int  hoveredPhase   = -1;
    private Transform player;
    private AudioSource audioSource;
    private Coroutine speechCoroutine;

    // ── DADOS DAS FASES ───────────────────────────────────────────
    private static readonly string[] PhaseNames = {
        "Disco Fever", "The Hive", "Graveyard Groove", "Mayhem Theatre"
    };
    private static readonly string[] PhaseDescs = {
        "Uma boate abandonada pulsa com vida. Donna e suas dançarinas guardam o primeiro fragmento.",
        "Uma colmeia gigante zumbe no ritmo. A Rainha das Abelhas protege o segundo fragmento.",
        "Um cemitério que dança toda noite. Zombie Jack não deixa os mortos descansarem.",
        "O palco é uma armadilha. Lady in Red atua para um público que nunca vai embora."
    };
    private static readonly string[] BossNames = {
        "Boss: Donna", "Boss: Queen Bee", "Boss: Zombie Jack", "Boss: Lady in Red"
    };

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (phaseTooltip != null) phaseTooltip.SetActive(false);
        if (phaseMapPanel != null) phaseMapPanel.SetActive(false);
        if (speechBubble  != null) speechBubble.SetActive(false);
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void Start()
    {
        player = PlayerController.Instance?.transform;

        // Inicia música do Hub
        if (hubMusic != null)
            RhythmManager.Instance?.StartMusic(hubMusic, hubBPM);

        // Braço do toca-discos na posição de play
        if (turntableArm != null)
            turntableArm.localRotation = Quaternion.Euler(0, 0, armPlayAngle);

        // Configura botões do mapa
        SetupPhaseButtons();

        // Atualiza estado visual com os dados salvos
        RefreshFragmentDisplay();
        RefreshHUD();

        // Subscreve eventos
        GameManager.Instance.OnVinylCountChanged  += _ => RefreshHUD();
        GameManager.Instance.OnFragmentCollected  += _ => RefreshFragmentDisplay();
        closeMapButton?.onClick.AddListener(ClosePhaseMap);

        // Fecha mapa com ESC
        SceneController.Instance?.FadeIn(0.5f);

        // Fala de boas-vindas do Maestro
        string welcome = GameManager.Instance.FragmentsCollected == 0
            ? "Bem-vindo ao Porão! Use o toca-discos para escolher uma fase."
            : $"De volta! Você tem {GameManager.Instance.FragmentsCollected}/4 fragmentos.";
        StartCoroutine(ShowSpeechDelayed(welcome, 1.5f));
    }

    void Update()
    {
        if (player == null) return;

        HandleTurntableProximity();
        HandleMaestroProximity();
        HandleTurntableRotation();

        if (Input.GetKeyDown(KeyCode.Escape) && mapOpen)
            ClosePhaseMap();
    }

    // ── TOCA-DISCOS ───────────────────────────────────────────────

    private void HandleTurntableRotation()
    {
        if (turntableDisc == null) return;
        if (turntablePlaying)
            turntableDisc.Rotate(Vector3.forward, -discSpinSpeed * Time.deltaTime);
    }

    private void HandleTurntableProximity()
    {
        if (turntableDisc == null) return;
        float dist = Vector2.Distance(player.position, turntableDisc.position);
        bool near  = dist < interactRadius;

        if (near != isNearTurntable)
        {
            isNearTurntable = near;
            interactPrompt?.SetActive(near);
        }

        if (near && Input.GetKeyDown(KeyCode.E))
            TogglePhaseMap();
    }

    private void TogglePhaseMap()
    {
        if (mapOpen) ClosePhaseMap();
        else         OpenPhaseMap();
    }

    // ── MAPA DE FASES ─────────────────────────────────────────────

    private void OpenPhaseMap()
    {
        if (mapOpen) return;
        mapOpen = true;
        phaseMapPanel?.SetActive(true);
        StartCoroutine(FadeGroup(phaseMapGroup, 0f, 1f, 0.3f));

        // Para o toca-discos
        if (turntableArm != null)
            StartCoroutine(RotateTo(turntableArm, armIdleAngle, 0.4f));

        if (turntableStartSFX != null)
            audioSource?.PlayOneShot(turntableStartSFX);
    }

    private void ClosePhaseMap()
    {
        if (!mapOpen) return;
        mapOpen = false;
        StartCoroutine(CloseMapCoroutine());

        // Retoma o toca-discos
        if (turntableArm != null)
            StartCoroutine(RotateTo(turntableArm, armPlayAngle, 0.4f));
    }

    private IEnumerator CloseMapCoroutine()
    {
        if (phaseTooltip != null) phaseTooltip.SetActive(false);
        yield return StartCoroutine(FadeGroup(phaseMapGroup, 1f, 0f, 0.2f));
        phaseMapPanel?.SetActive(false);
    }

    private void SetupPhaseButtons()
    {
        for (int i = 0; i < phaseButtons.Length; i++)
        {
            int phaseIndex = i + 1; // fases 1–4
            bool unlocked  = GameManager.Instance.IsPhaseUnlocked(phaseIndex);

            // Configura texto
            if (i < phaseLabels.Length && phaseLabels[i] != null)
                phaseLabels[i].text = PhaseNames[i];

            // Cadeado
            if (i < phaseLockIcons.Length && phaseLockIcons[i] != null)
                phaseLockIcons[i].SetActive(!unlocked);

            // Fragmento coletado
            if (i < phaseFragmentSlots.Length && phaseFragmentSlots[i] != null)
                phaseFragmentSlots[i].color = GameManager.Instance.HasFragment(i)
                    ? fragmentActiveColor : fragmentInactiveColor;

            // Botão
            if (phaseButtons[i] != null)
            {
                phaseButtons[i].interactable = unlocked;

                int capturedIndex = i;
                phaseButtons[i].onClick.AddListener(() => OnPhaseSelected(capturedIndex + 1));

                // Hover: mostra tooltip
                var trigger = phaseButtons[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                AddHoverEvent(trigger, capturedIndex);
            }
        }
    }

    private void AddHoverEvent(UnityEngine.EventSystems.EventTrigger trigger, int index)
    {
        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => ShowPhaseTooltip(index));
        trigger.triggers.Add(enterEntry);

        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => HidePhaseTooltip());
        trigger.triggers.Add(exitEntry);
    }

    private void ShowPhaseTooltip(int index)
    {
        if (phaseTooltip == null) return;
        phaseTooltip.SetActive(true);
        if (tooltipTitle    != null) tooltipTitle.text    = PhaseNames[index];
        if (tooltipDesc     != null) tooltipDesc.text     = PhaseDescs[index];
        if (tooltipBossName != null) tooltipBossName.text = BossNames[index];
    }

    private void HidePhaseTooltip()
    {
        phaseTooltip?.SetActive(false);
    }

    private void OnPhaseSelected(int phaseNumber)
    {
        if (!GameManager.Instance.IsPhaseUnlocked(phaseNumber)) return;
        ClosePhaseMap();
        StartCoroutine(LoadPhaseWithDelay(phaseNumber));
    }

    private IEnumerator LoadPhaseWithDelay(int phaseNumber)
    {
        yield return new WaitForSeconds(0.3f);
        SceneController.Instance?.GoToPhase(phaseNumber);
    }

    // ── NPC MAESTRO ───────────────────────────────────────────────

    private void HandleMaestroProximity()
    {
        if (maestroObject == null) return;
        float dist = Vector2.Distance(player.position, maestroObject.transform.position);
        bool near  = dist < interactRadiusMaestro;

        if (near != isNearMaestro)
        {
            isNearMaestro = near;
            if (near && Input.GetKeyDown(KeyCode.E))
                TriggerMaestroDialogue();
        }
    }

    private void TriggerMaestroDialogue()
    {
        int fragments  = GameManager.Instance.FragmentsCollected;
        string[] lines = {
            "O Disco Dourado foi partido em 4! Cada fase esconde um fragmento.",
            $"Você já tem {fragments} de 4 fragmentos. Continue assim!",
            "Gaste seus vinis na loja — os upgrades fazem diferença!",
            "O toca-discos abre o mapa. Escolha sua próxima fase com sabedoria."
        };
        ShowSpeech(lines[fragments < lines.Length ? fragments : lines.Length - 1]);
    }

    private IEnumerator ShowSpeechDelayed(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowSpeech(text);
    }

    private void ShowSpeech(string text)
    {
        if (speechCoroutine != null) StopCoroutine(speechCoroutine);
        speechCoroutine = StartCoroutine(SpeechCoroutine(text));
    }

    private IEnumerator SpeechCoroutine(string text)
    {
        if (speechBubble == null) yield break;
        if (speechText != null) speechText.text = text;
        speechBubble.SetActive(true);

        // Fade in
        CanvasGroup cg = speechBubble.GetComponent<CanvasGroup>();
        if (cg != null) yield return StartCoroutine(FadeGroup(cg, 0f, 1f, 0.2f));

        yield return new WaitForSeconds(speechDuration);

        // Fade out
        if (cg != null) yield return StartCoroutine(FadeGroup(cg, 1f, 0f, 0.3f));
        speechBubble.SetActive(false);
    }

    // ── DISPLAY DE FRAGMENTOS ─────────────────────────────────────

    private void RefreshFragmentDisplay()
    {
        for (int i = 0; i < fragmentDisplayObjects.Length; i++)
        {
            if (fragmentDisplayObjects[i] == null) continue;
            bool collected = GameManager.Instance.HasFragment(i);
            SpriteRenderer sr = fragmentDisplayObjects[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = collected ? fragmentActiveColor : fragmentInactiveColor;

            // Pequena animação de aparecimento quando coletado
            if (collected)
                StartCoroutine(FragmentAppear(fragmentDisplayObjects[i]));
        }

        // Atualiza também os slots do mapa
        if (phaseFragmentSlots == null) return;
        for (int i = 0; i < phaseFragmentSlots.Length; i++)
        {
            if (phaseFragmentSlots[i] == null) continue;
            phaseFragmentSlots[i].color = GameManager.Instance.HasFragment(i)
                ? fragmentActiveColor : fragmentInactiveColor;
        }
    }

    private IEnumerator FragmentAppear(GameObject obj)
    {
        float t = 0f;
        Vector3 orig = obj.transform.localScale;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * Mathf.PI / 0.4f) * 0.15f;
            obj.transform.localScale = orig * s;
            yield return null;
        }
        obj.transform.localScale = orig;
    }

    // ── HUD ───────────────────────────────────────────────────────

    private void RefreshHUD()
    {
        if (vinylCountText    != null)
            vinylCountText.text = GameManager.Instance.GetVinyls().ToString();
        if (fragmentCountText != null)
            fragmentCountText.text =
                $"{GameManager.Instance.FragmentsCollected}/4";
    }

    // ── UTILITÁRIOS ───────────────────────────────────────────────

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        float elapsed = 0f;
        group.alpha = from;
        group.interactable = group.blocksRaycasts = false;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
        group.interactable = group.blocksRaycasts = (to >= 1f);
    }

    private IEnumerator RotateTo(Transform t, float targetZ, float duration)
    {
        Quaternion startRot = t.localRotation;
        Quaternion endRot   = Quaternion.Euler(0, 0, targetZ);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localRotation = Quaternion.Lerp(startRot, endRot,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        t.localRotation = endRot;
    }

    void OnDrawGizmosSelected()
    {
        if (turntableDisc != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(turntableDisc.position, interactRadius);
        }
        if (maestroObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(maestroObject.transform.position, interactRadiusMaestro);
        }
    }
}