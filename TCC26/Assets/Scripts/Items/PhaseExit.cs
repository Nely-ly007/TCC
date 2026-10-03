using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - PhaseExit
/// Porta/portal que abre após o boss ser derrotado.
/// 
/// Fluxo completo:
/// 1. Boss morre → OnBossDefeated dispara
/// 2. Fragmento dourado aparece no chão
/// 3. Player coleta o fragmento (CollectibleDiscFragment)
/// 4. PhaseExit.Open() é chamado → porta abre com animação
/// 5. Player entra na porta → volta ao Hub com fade
///
/// Setup:
/// - Crie um GameObject "PhaseExit" com este script
/// - Adicione SpriteRenderer (sprite da porta fechada)
/// - Adicione BoxCollider2D (Is Trigger: true) — área de entrada
/// - Arraste a referência do BossDonna no Inspector
/// </summary>
public class PhaseExit : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private BossDonna bossRef;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteClosed;   // porta fechada
    [SerializeField] private Sprite spriteOpen;     // porta aberta

    [Header("Efeitos")]
    [SerializeField] private AudioClip openSFX;
    [SerializeField] private AudioClip enterSFX;
    [SerializeField] private ParticleSystem openEffect;
    [SerializeField] private GameObject interactPrompt; // "ENTER — Voltar ao Hub"

    [Header("Delay após boss morrer")]
    [SerializeField] private float openDelay = 2f; // aguarda animação do boss

    private bool isOpen    = false;
    private bool isEntering = false;
    private SpriteRenderer sr;
    private AudioSource    audioSource;

    void Awake()
    {
        sr          = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Porta começa fechada
        if (sr != null && spriteClosed != null)
            sr.sprite = spriteClosed;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void Start()
    {
        // Subscreve ao evento de boss derrotado
        if (bossRef != null)
            bossRef.OnBossDefeated += OnBossDefeated;
        else
            Debug.LogWarning("[PhaseExit] BossRef não atribuído no Inspector!");
    }

    void OnDestroy()
    {
        if (bossRef != null)
            bossRef.OnBossDefeated -= OnBossDefeated;
    }

    // ── BOSS DERROTADO ────────────────────────────────────────────
    private void OnBossDefeated()
    {
        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        // Aguarda animação de morte do boss + coleta do fragmento
        yield return new WaitForSeconds(openDelay);

        Open();
    }

    // ── ABRE A PORTA ─────────────────────────────────────────────
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        // Troca sprite
        if (sr != null && spriteOpen != null)
            sr.sprite = spriteOpen;

        // SFX
        if (openSFX != null)
            audioSource?.PlayOneShot(openSFX);

        // Partículas
        if (openEffect != null)
            openEffect.Play();

        // Câmera aponta para a porta brevemente
        CameraShake.Instance?.Pulse(0.08f);

        // Anima a abertura
        StartCoroutine(OpenAnimation());
    }

    private IEnumerator OpenAnimation()
    {
        // Scale bounce ao abrir
        Vector3 orig = transform.localScale;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t / 0.3f * Mathf.PI) * 0.15f;
            transform.localScale = orig * s;
            yield return null;
        }
        transform.localScale = orig;

        // Mostra prompt de interação
        if (interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    // ── PLAYER ENTRA NA PORTA ────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen || isEntering) return;
        if (!other.CompareTag("Player")) return;

        isEntering = true;
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (enterSFX != null) audioSource?.PlayOneShot(enterSFX);

        StartCoroutine(ExitSequence());
    }

    private IEnumerator ExitSequence()
    {
        // Salva o jogo antes de sair
        GameManager.Instance?.SaveGame();

        // Pequena pausa dramática
        yield return new WaitForSeconds(0.3f);

        // Volta ao Hub com fade
        SceneController.Instance?.GoToHub();
    }
}
