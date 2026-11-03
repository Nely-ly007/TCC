using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
    [SerializeField] private float openDelay = 0.5f;

    private bool isOpen     = false;
    private bool isEntering = false;
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

        // Se esquecer de arrastar no Inspector, busca na cena
        if (bossRef == null)
            bossRef = FindObjectOfType<BossDonna>();

        if (bossRef != null)
        {
            bossRef.OnBossDefeated += OnBossDefeated;
            Debug.Log("[PhaseExit] Inscrito no OnBossDefeated com sucesso!");
        }
        else
        {
            Debug.LogError("[PhaseExit] NENHUM BossDonna encontrado na cena!");
        }
    }

    void OnDestroy()
    {
        if (bossRef != null)
            bossRef.OnBossDefeated -= OnBossDefeated;
    }

    private void OnBossDefeated()
    {
        Debug.Log("[PhaseExit] Evento de vitória recebido! Abrindo porta...");
        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        yield return new WaitForSeconds(openDelay);
        Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        Debug.Log("[PhaseExit] Porta ABERTA!");

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
        {
            interactPrompt.SetActive(true);
            Debug.Log("[PhaseExit] interactPrompt ATIVADO!");
        }
        else
        {
            Debug.LogWarning("[PhaseExit] interactPrompt é NULL! Verifique o Inspector.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen || isEntering) return;
        if (!other.CompareTag("Player")) return;

        isEntering = true;

        if (interactPrompt != null) interactPrompt.SetActive(false);
        audioSource?.PlayOneShot(enterSFX);

        if (sr != null) sr.sortingOrder = openSortingOrder;

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
        if (SceneController.Instance != null)
        {
            SceneController.Instance.GoToHub();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadHub();
            return;
        }

        if (!string.IsNullOrEmpty(hubSceneName))
        {
            SceneManager.LoadScene(hubSceneName);
        }
    }
}