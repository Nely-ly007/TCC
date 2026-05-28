using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - Checkpoint
/// Ponto de salvamento. Ao tocar, define novo respawn e salva o estado.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool isActive = false;
    [SerializeField] private Animator checkpointAnim;
    [SerializeField] private AudioClip activateSFX;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.gray;

    private SpriteRenderer sr;
    private AudioSource audioSource;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        UpdateVisual();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive || !other.CompareTag("Player")) return;
        Activate();
    }

    private void Activate()
    {
        isActive = true;
        PlayerController.Instance?.SetCheckpoint(transform.position);
        GameManager.Instance?.SaveGame();

        if (activateSFX != null) audioSource?.PlayOneShot(activateSFX);
        checkpointAnim?.SetTrigger("Activate");
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (sr != null) sr.color = isActive ? activeColor : inactiveColor;
    }
}

/// <summary>
/// POP ADVENTURE - BossTrigger
/// Área que ativa o boss ao ser entrado pelo jogador.
/// </summary>
public class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossDonna bossRef;
    [SerializeField] private GameObject bossHealthBarUI;
    [SerializeField] private AudioClip bossIntroSFX;

    private bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(BossIntroSequence());
    }

    private IEnumerator BossIntroSequence()
    {
        // Bloqueia a saída (fechar portas, etc - pode ser expandido)
        if (bossIntroSFX != null)
            AudioSource.PlayClipAtPoint(bossIntroSFX, transform.position);

        yield return new WaitForSeconds(1f);

        if (bossRef != null)
        {
            bossRef.ActivateBoss();
            bossRef.OnBossDefeated += OnBossDefeated;
        }

        if (bossHealthBarUI != null)
            bossHealthBarUI.SetActive(true);
    }

    private void OnBossDefeated()
    {
        if (bossHealthBarUI != null)
            bossHealthBarUI.SetActive(false);

        // Pode abrir porta para próxima fase
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        yield return new WaitForSeconds(2f);
        // Aqui você pode carregar a próxima cena ou retornar ao Hub
        GameManager.Instance?.LoadHub();
    }
}

/// <summary>
/// POP ADVENTURE - WorldColorTransition
/// Transição do mundo cinza para colorido ao entrar numa fase.
/// </summary>
public class WorldColorTransition : MonoBehaviour
{
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private Color targetColor = Color.white;

    private SpriteRenderer[] allSprites;

    void Start()
    {
        allSprites = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        StartCoroutine(ColorTransition());
    }

    private IEnumerator ColorTransition()
    {
        float t = 0;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float lerp = t / transitionDuration;
            foreach (var sr in allSprites)
            {
                if (sr != null)
                    sr.color = Color.Lerp(Color.gray, targetColor, lerp);
            }
            yield return null;
        }
    }
}
