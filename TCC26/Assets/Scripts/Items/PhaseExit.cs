using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// POP ADVENTURE - PhaseExit v3
/// Fix: fallback robusto para testes no Editor (sem GameManager na cena)
/// Fix: player fica "dentro" da porta — sorting order + animação de entrada
///
/// Setup:
/// - BoxCollider2D → Is Trigger: TRUE
/// - Arraste BossDonna no Inspector
/// - Hub Scene Name: nome exato da cena do Hub no Build Settings
/// </summary>
public class PhaseExit : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private BossDonna bossRef;

    [Header("Cena do Hub")]
    [Tooltip("Nome exato da cena do Hub no Build Settings (ex: Hub)")]
    [SerializeField] private string hubSceneName = "Hub";

    [Header("Sprites")]
    [SerializeField] private Sprite spriteClosed;
    [SerializeField] private Sprite spriteOpen;

    [Header("Sorting")]
    [Tooltip("Sorting Order quando fechada (atrás do player)")]
    [SerializeField] private int closedSortingOrder = 0;
    [Tooltip("Sorting Order quando player entra (frente do player)")]
    [SerializeField] private int openSortingOrder   = 5;

    [Header("Efeitos")]
    [SerializeField] private AudioClip openSFX;
    [SerializeField] private AudioClip enterSFX;
    [SerializeField] private ParticleSystem openEffect;
    [SerializeField] private GameObject interactPrompt;

    [Header("Delay após boss morrer")]
    [SerializeField] private float openDelay = 2f;

    private bool          isOpen     = false;
    private bool          isEntering = false;
    private SpriteRenderer sr;
    private AudioSource    audioSource;

    void Awake()
    {
        sr          = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (sr != null)
        {
            if (spriteClosed != null) sr.sprite = spriteClosed;
            sr.sortingOrder = closedSortingOrder;
        }

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void Start()
    {
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

    private void OnBossDefeated() => StartCoroutine(OpenSequence());

    private IEnumerator OpenSequence()
    {
        yield return new WaitForSeconds(openDelay);
        Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        if (sr != null && spriteOpen != null)
            sr.sprite = spriteOpen;

        audioSource?.PlayOneShot(openSFX);
        if (openEffect != null) openEffect.Play();
        CameraShake.Instance?.Pulse(0.08f);

        StartCoroutine(OpenAnimation());
    }

    private IEnumerator OpenAnimation()
    {
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

        if (interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen || isEntering) return;
        if (!other.CompareTag("Player")) return;

        isEntering = true;

        if (interactPrompt != null) interactPrompt.SetActive(false);
        audioSource?.PlayOneShot(enterSFX);

        // Porta na frente do player
        if (sr != null) sr.sortingOrder = openSortingOrder;

        // Trava collider do player
        var col = other.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(ExitSequence(other.gameObject));
    }

    private IEnumerator ExitSequence(GameObject player)
    {
        GameManager.Instance?.SaveGame();

        yield return StartCoroutine(PlayerEnterAnimation(player));

        GoToHub();
    }

    private IEnumerator PlayerEnterAnimation(GameObject player)
    {
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Vector3 startPos   = player.transform.position;
        Vector3 targetPos  = transform.position;
        Vector3 startScale = player.transform.localScale;

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float pct = t / 0.4f;
            player.transform.position   = Vector3.Lerp(startPos, targetPos, pct);
            player.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, pct);
            yield return null;
        }
    }

    private void GoToHub()
    {
        // Tenta SceneController
        if (SceneController.Instance != null)
        {
            SceneController.Instance.GoToHub();
            return;
        }

        // Tenta GameManager direto
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadHub();
            return;
        }

        // Fallback para testes no Editor (sem managers na cena)
        if (!string.IsNullOrEmpty(hubSceneName))
        {
            Debug.LogWarning("[PhaseExit] Nenhum Manager encontrado. Carregando Hub direto via SceneManager.");
            SceneManager.LoadScene(hubSceneName);
        }
        else
        {
            Debug.LogError("[PhaseExit] hubSceneName está vazio e nenhum Manager existe. " +
                           "Preencha o campo Hub Scene Name no Inspector.");
        }
    }
}