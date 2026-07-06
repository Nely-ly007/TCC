using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// POP ADVENTURE - MaestroDialogue
/// Sistema de diálogo do NPC Maestro.
/// - Typewriter (letras aparecem uma a uma)
/// - Retrato do NPC ao lado do texto
/// - Tecla E para interagir / avançar / fechar
/// - Falas diferentes dependendo dos fragmentos coletados
/// </summary>
public class MaestroDialogue : MonoBehaviour
{
    // ── REFERÊNCIAS UI ────────────────────────────────────────────
    [Header("Painel de Diálogo (Canvas World Space)")]
    [SerializeField] private GameObject dialoguePanel;     // painel inteiro
    [SerializeField] private CanvasGroup dialogueGroup;    // para fade in/out
    [SerializeField] private TextMeshProUGUI nameText;     // "Maestro"
    [SerializeField] private TextMeshProUGUI bodyText;     // texto principal
    [SerializeField] private Image portraitImage;          // sprite do Maestro
    [SerializeField] private GameObject continueIndicator; // "▼" piscando

    // ── RETRATOS ──────────────────────────────────────────────────
    [Header("Sprites do Retrato")]
    [SerializeField] private Sprite portraitNormal;        // expressão padrão
    [SerializeField] private Sprite portraitHappy;         // expressão feliz
    [SerializeField] private Sprite portraitSurprised;     // expressão surpresa

    // ── CONFIGURAÇÕES ─────────────────────────────────────────────
    [Header("Configurações")]
    [SerializeField] private float typewriterSpeed = 0.04f; // segundos por letra
    [SerializeField] private float interactRadius  = 2f;    // distância para interagir
    [SerializeField] private AudioClip typingSFX;           // som de cada letra
    [SerializeField] private AudioClip openSFX;             // som ao abrir diálogo

    // ── FALAS (por quantidade de fragmentos) ──────────────────────
    [Header("Falas do Maestro")]
    [SerializeField] private DialogueLine[] linesFragment0; // sem fragmentos
    [SerializeField] private DialogueLine[] linesFragment1; // 1 fragmento
    [SerializeField] private DialogueLine[] linesFragment2; // 2 fragmentos
    [SerializeField] private DialogueLine[] linesFragment3; // 3 fragmentos
    [SerializeField] private DialogueLine[] linesFragment4; // todos coletados

    // ── ESTADO INTERNO ────────────────────────────────────────────
    private bool isOpen         = false;
    private bool isTyping       = false;
    private bool playerIsNear   = false;
    private int  currentLine    = 0;
    private DialogueLine[] currentLines;
    private Coroutine typewriterCoroutine;
    private Transform player;
    private AudioSource audioSource;
    private GameObject interactPrompt; // "E - Falar" que aparece acima do Maestro

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        player      = PlayerController.Instance?.transform;
        audioSource = GetComponent<AudioSource>();

        // Fecha tudo no início
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);

        // Cria o prompt "E - Falar" dinamicamente
        CreateInteractPrompt();
    }

    void Update()
    {
        if (player == null) return;

        CheckProximity();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isOpen && playerIsNear)
                StartDialogue();
            else if (isOpen)
                HandleEPressed();
        }
    }

    // ── PROXIMIDADE ───────────────────────────────────────────────
    private void CheckProximity()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        bool near  = dist <= interactRadius;

        if (near != playerIsNear)
        {
            playerIsNear = near;
            if (interactPrompt != null)
                interactPrompt.SetActive(near && !isOpen);
        }
    }

    // ── ABRIR DIÁLOGO ─────────────────────────────────────────────
    private void StartDialogue()
    {
        isOpen      = true;
        currentLine = 0;
        currentLines = GetLinesForCurrentProgress();

        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (openSFX != null) audioSource?.PlayOneShot(openSFX);

        dialoguePanel.SetActive(true);
        StartCoroutine(FadePanel(0f, 1f, 0.2f));

        ShowLine(currentLine);
    }

    // ── TECLA E PRESSIONADA DURANTE DIÁLOGO ──────────────────────
    private void HandleEPressed()
    {
        if (isTyping)
        {
            // Se ainda está digitando: pula para o fim da linha
            SkipTypewriter();
        }
        else
        {
            // Linha completa: avança para a próxima
            currentLine++;
            if (currentLine < currentLines.Length)
                ShowLine(currentLine);
            else
                CloseDialogue();
        }
    }

    // ── EXIBE UMA LINHA ───────────────────────────────────────────
    private void ShowLine(int index)
    {
        if (index >= currentLines.Length) return;
        DialogueLine line = currentLines[index];

        // Atualiza nome e retrato
        if (nameText != null)
            nameText.text = line.speakerName == "" ? "Maestro" : line.speakerName;

        if (portraitImage != null && line.portrait != null)
            portraitImage.sprite = line.portrait;
        else if (portraitImage != null)
            portraitImage.sprite = portraitNormal; // fallback

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        // Inicia typewriter
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
    }

    // ── TYPEWRITER ────────────────────────────────────────────────
    private IEnumerator TypewriterEffect(string fullText)
    {
        isTyping = true;
        if (bodyText != null) bodyText.text = "";

        foreach (char letter in fullText)
        {
            if (bodyText != null) bodyText.text += letter;

            // Som a cada letra (não toca em espaços para não poluir)
            if (typingSFX != null && letter != ' ')
                audioSource?.PlayOneShot(typingSFX, 0.3f);

            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;

        // Mostra indicador de continuar
        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }

    // ── PULA O TYPEWRITER ─────────────────────────────────────────
    private void SkipTypewriter()
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        if (bodyText != null && currentLines != null)
            bodyText.text = currentLines[currentLine].text;

        isTyping = false;
        if (continueIndicator != null)
            continueIndicator.SetActive(true);
    }

    // ── FECHAR DIÁLOGO ────────────────────────────────────────────
    private void CloseDialogue()
    {
        StartCoroutine(CloseSequence());
    }

    private IEnumerator CloseSequence()
    {
        yield return StartCoroutine(FadePanel(1f, 0f, 0.2f));
        dialoguePanel.SetActive(false);
        isOpen = false;

        if (playerIsNear && interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    // ── ESCOLHE FALAS PELO PROGRESSO ─────────────────────────────
    private DialogueLine[] GetLinesForCurrentProgress()
    {
        int fragments = GameManager.Instance?.FragmentsCollected ?? 0;
        return fragments switch
        {
            0 => linesFragment0,
            1 => linesFragment1,
            2 => linesFragment2,
            3 => linesFragment3,
            _ => linesFragment4
        };
    }

    // ── CRIA PROMPT "E - FALAR" ───────────────────────────────────
    private void CreateInteractPrompt()
    {
        interactPrompt = new GameObject("InteractPrompt");
        interactPrompt.transform.SetParent(transform);
        interactPrompt.transform.localPosition = new Vector3(0, 1.4f, 0);

        // Canvas em world space para aparecer acima do NPC
        Canvas c = interactPrompt.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        interactPrompt.AddComponent<CanvasScaler>();

        RectTransform rt = interactPrompt.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 0.4f);
        rt.localScale = Vector3.one * 0.01f;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(interactPrompt.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = "E  —  Falar";
        tmp.fontSize  = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        interactPrompt.SetActive(false);
    }

    // ── FADE DO PAINEL ────────────────────────────────────────────
    private IEnumerator FadePanel(float from, float to, float dur)
    {
        if (dialogueGroup == null) yield break;
        float elapsed = 0f;
        dialogueGroup.alpha = from;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            dialogueGroup.alpha = Mathf.Lerp(from, to, elapsed / dur);
            yield return null;
        }
        dialogueGroup.alpha = to;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

// ── ESTRUTURA DE UMA LINHA DE DIÁLOGO ────────────────────────────
[System.Serializable]
public class DialogueLine
{
    public string speakerName = "Maestro"; // deixe vazio para usar "Maestro"
    [TextArea(2, 4)]
    public string text;                    // o texto da fala
    public Sprite portrait;                // retrato para esta fala (opcional)
}