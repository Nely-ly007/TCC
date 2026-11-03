using UnityEngine;
using System.Collections;

public class BossDonna : MonoBehaviour, IDamageable
{
    // ── STATS ─────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] private int maxHP             = 300;
    [SerializeField] private int phase2HPThreshold = 150;

    // ── PROJÉTEIS ─────────────────────────────────────────────────
    [Header("Projéteis")]
    [SerializeField] private GameObject skateProjectilePrefab;
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private int        skateDamage     = 15;
    [SerializeField] private int        shockwaveDamage = 15;
    [SerializeField] private float      skateSpeed      = 6f;
    [SerializeField] private Transform  skateSpawnPoint;
    [SerializeField] private Transform  waveSpawnPoint;

    // ── DROPS DE CURA ─────────────────────────────────────────────
    [Header("Drops de Cura")]
    [SerializeField] private GameObject musicNoteHealPrefab;
    [SerializeField] private int        maxHealDrops = 2;

    // ── RECOMPENSA ────────────────────────────────────────────────
    [Header("Fragmento")]
    [SerializeField] private GameObject goldenDiscFragmentPrefab;

    // ── EFEITOS ───────────────────────────────────────────────────
    [Header("Efeitos")]
    [SerializeField] private AudioClip phase1Music;
    [SerializeField] private AudioClip phase2Music;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip hurtSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip phase2SFX;
    [SerializeField] private ParticleSystem phase2Effect;

    // ── BOSS HP BAR ───────────────────────────────────────────────
    [Header("HP Bar (UI Toolkit)")]
    [Tooltip("Arraste o GameObject BossHPUI aqui")]
    [SerializeField] private BossHPUIToolkit bossHPUI;

    // ── DAMAGE FLASH ──────────────────────────────────────────────
    [Header("Damage Flash")]
    [SerializeField] private Color flashColor    = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float flashDuration = 0.12f;

    // ── ESTADO ───────────────────────────────────────────────────
    private int  currentHP;
    private bool isDead;
    private bool isPhase2;
    private bool isBossActive = false;
    private int  healDropCount = 0;
    private int  attackPattern = 0;
    private bool isHurt        = false;
    private bool isFlashing    = false;

    // ── COMPONENTES ───────────────────────────────────────────────
    private Animator         anim;
    private AudioSource      audioSource;
    private SpriteRenderer[] bodyParts;
    private Color[]          originalColors;
    private Transform        player;

    // ── EVENTS ───────────────────────────────────────────────────
    public System.Action<int, int> OnHealthChanged;
    public System.Action           OnBossDefeated;

    void Awake()
    {
        anim        = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        currentHP   = maxHP;

        bodyParts      = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[bodyParts.Length];
        for (int i = 0; i < bodyParts.Length; i++)
        {
            if (bodyParts[i] != null)
                originalColors[i] = bodyParts[i].color;
        }

        if (bossHPUI == null)
            bossHPUI = FindObjectOfType<BossHPUIToolkit>();
    }

    void Start()
    {
        player = PlayerController.Instance?.transform;

        // Oculta a UI de HP ao carregar a cena (só exibe no ActivateBoss)
        bossHPUI?.Hide();
    }

    void OnEnable()  => RhythmManager.OnBeatNumberStatic += OnBeat;
    void OnDisable() => RhythmManager.OnBeatNumberStatic -= OnBeat;

    // ── ATIVAÇÃO ─────────────────────────────────────────────────
    public void ActivateBoss()
    {
        if (isBossActive) return;

        isBossActive = true;
        bossHPUI?.Show(currentHP, maxHP);

        if (phase1Music != null)
            RhythmManager.Instance?.StartMusic(phase1Music, 120f);

        anim?.SetTrigger("Entrance");
    }

    private void OnBeat(int beatNumber)
    {
        if (!isBossActive || isDead) return;
        if (beatNumber == 0) ExecuteAttackPattern();
        else if (beatNumber == 2 && isPhase2)
            StartCoroutine(WaveAttackCoroutine());
    }

    private void ExecuteAttackPattern()
    {
        if (isDead) return;
        int maxPattern = isPhase2 ? 3 : 2;
        switch (attackPattern % maxPattern)
        {
            case 0: StartCoroutine(SkateAttackCoroutine()); break;
            case 1: StartCoroutine(WaveAttackCoroutine());  break;
            case 2: StartCoroutine(DoubleSkateCoroutine()); break;
        }
        attackPattern++;
    }

    private IEnumerator SkateAttackCoroutine()
    {
        anim?.SetTrigger("AttackSkate");
        PlaySFX(attackSFX);
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator WaveAttackCoroutine()
    {
        anim?.SetTrigger("AttackWave");
        PlaySFX(attackSFX);
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator DoubleSkateCoroutine()
    {
        yield return StartCoroutine(SkateAttackCoroutine());
        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(SkateAttackCoroutine());
    }

    public void SpawnSkateEvent()
    {
        if (player == null || skateProjectilePrefab == null) return;

        Vector2 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir == Vector2.zero) dir = Vector2.right;
        dir.Normalize();

        Vector3 spawnPos = skateSpawnPoint != null
            ? skateSpawnPoint.position
            : transform.position + Vector3.right;

        GameObject obj = Instantiate(skateProjectilePrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<SkateProjectile>()?.Init(dir, skateSpeed, skateDamage);
    }

    public void SpawnWaveEvent()
    {
        if (shockwavePrefab == null) return;

        Vector3 spawnPos = waveSpawnPoint != null
            ? waveSpawnPoint.position
            : transform.position;

        Instantiate(shockwavePrefab, spawnPos, Quaternion.identity);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || !isBossActive) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        OnHealthChanged?.Invoke(currentHP, maxHP);
        bossHPUI?.UpdateHP(currentHP, maxHP);

        if (!isFlashing)
            StartCoroutine(DamageFlashCoroutine());

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        if (!isPhase2 && currentHP <= phase2HPThreshold)
        {
            EnterPhase2();
        }
        else if (!isHurt)
        {
            StartCoroutine(HurtCoroutine());
        }
    }

    private void EnterPhase2()
    {
        isPhase2 = true;
        anim?.SetTrigger("Phase2");
        PlaySFX(phase2SFX);
        if (phase2Effect != null) phase2Effect.Play();

        if (phase2Music != null)
            RhythmManager.Instance?.StartMusic(phase2Music, 140f);
    }

    private IEnumerator HurtCoroutine()
    {
        isHurt = true;
        anim?.ResetTrigger("Hurt");
        anim?.SetTrigger("Hurt");
        PlaySFX(hurtSFX);

        yield return new WaitForSeconds(0.3f);
        isHurt = false;
    }

    public void OnHurtAnimationEnd()
    {
        isHurt = false;
        anim?.ResetTrigger("Hurt");
    }

    private IEnumerator DamageFlashCoroutine()
    {
        isFlashing = true;

        for (int i = 0; i < bodyParts.Length; i++)
        {
            if (bodyParts[i] != null)
                bodyParts[i].color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < bodyParts.Length; i++)
        {
            if (bodyParts[i] != null)
                bodyParts[i].color = originalColors[i];
        }

        isFlashing = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[BossDonna] Boss derrotado! Notificando a PhaseExit...");

        OnBossDefeated?.Invoke();

        bossHPUI?.Hide();

        if (anim != null && HasParameter(anim, "Die"))
        {
            anim.SetTrigger("Die");
        }

        PlaySFX(deathSFX);

        if (goldenDiscFragmentPrefab != null)
        {
            Instantiate(goldenDiscFragmentPrefab, transform.position, Quaternion.identity);
        }

        StartCoroutine(DieSequence());
    }

    private bool HasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    private IEnumerator DieSequence()
    {
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}