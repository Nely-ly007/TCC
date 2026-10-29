using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - BossDonna v3
/// Fixes:
/// - Hurt não volta ao idle: ResetTrigger antes de SetTrigger + flag isHurt
/// - DamageFlash com cor vermelha visível (não branco) + guard contra chamadas simultâneas
/// - bossHPUI null-safe com busca automática se não vinculado no Inspector
/// </summary>
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
    [SerializeField] private Color flashColor    = new Color(1f, 0.2f, 0.2f); // vermelho
    [SerializeField] private float flashDuration = 0.12f;

    // ── ESTADO ───────────────────────────────────────────────────
    private int  currentHP;
    private bool isDead;
    private bool isPhase2;
    private bool isBossActive;
    private int  healDropCount = 0;
    private int  attackPattern = 0;
    private bool isHurt        = false; // guard: evita Hurt trigger enquanto já está em Hurt
    private bool isFlashing    = false; // guard: evita flash simultâneo

    // ── COMPONENTES ───────────────────────────────────────────────
    private Animator         anim;
    private AudioSource      audioSource;
    private SpriteRenderer[] bodyParts;
    private Color[]          originalColors;
    private Transform        player;

    // ── EVENTS ───────────────────────────────────────────────────
    public System.Action<int, int> OnHealthChanged;
    public System.Action           OnBossDefeated;

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        anim        = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        currentHP   = maxHP;

        // Busca todos SpriteRenderers (incluindo inativos) e salva cores originais
        bodyParts      = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[bodyParts.Length];
        for (int i = 0; i < bodyParts.Length; i++)
            if (bodyParts[i] != null)
                originalColors[i] = bodyParts[i].color;

        // Se não vinculou no Inspector, tenta encontrar na cena
        if (bossHPUI == null)
            bossHPUI = FindObjectOfType<BossHPUIToolkit>();
    }

    void Start()
    {
        player = PlayerController.Instance?.transform;
        bossHPUI?.Hide();
        bossHPUI?.UpdateHP(currentHP, maxHP);
    }

    void OnEnable()  => RhythmManager.OnBeatNumberStatic += OnBeat;
    void OnDisable() => RhythmManager.OnBeatNumberStatic -= OnBeat;

    // ── ATIVAÇÃO ─────────────────────────────────────────────────
    public void ActivateBoss()
    {
        isBossActive = true;
        bossHPUI?.Show(currentHP, maxHP);

        if (phase1Music != null)
            RhythmManager.Instance?.StartMusic(phase1Music, 120f);

        anim?.SetTrigger("Entrance");
    }

    // ── RITMO ────────────────────────────────────────────────────
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

    // ── COROUTINES DE ATAQUE ─────────────────────────────────────
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

    // ── ANIMATION EVENTS ─────────────────────────────────────────
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

        var wL = Instantiate(shockwavePrefab, spawnPos + Vector3.left  * 0.5f, Quaternion.identity);
        var wR = Instantiate(shockwavePrefab, spawnPos + Vector3.right * 0.5f, Quaternion.identity);
        wL.GetComponent<Shockwave>()?.Init(Vector2.left,  shockwaveDamage);
        wR.GetComponent<Shockwave>()?.Init(Vector2.right, shockwaveDamage);
    }

    /// <summary>
    /// Coloque um Animation Event NO ÚLTIMO FRAME de Donna_Hurt
    /// apontando para este método. Ele libera o Animator de volta ao Idle.
    /// </summary>
    public void OnHurtAnimationEnd()
    {
        isHurt = false;
        anim?.ResetTrigger("Hurt");
    }

    // ── DANO ─────────────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        bossHPUI?.UpdateHP(currentHP, maxHP);

        PlaySFX(hurtSFX);

        // Só dispara Hurt se não estiver já em Hurt
        if (!isHurt)
        {
            isHurt = true;
            anim?.ResetTrigger("Hurt"); // limpa acumulação de triggers
            anim?.SetTrigger("Hurt");
        }

        if (!isFlashing)
            StartCoroutine(DamageFlash());

        if (!isPhase2 && currentHP <= maxHP * 0.5f && healDropCount < maxHealDrops)
            TryDropHeal();

        if (!isPhase2 && currentHP <= phase2HPThreshold)
            StartCoroutine(EnterPhase2());

        if (currentHP <= 0)
            StartCoroutine(Die());
    }

    private IEnumerator EnterPhase2()
    {
        isPhase2 = true;
        anim?.SetTrigger("Phase2");
        PlaySFX(phase2SFX);
        CameraShake.Instance?.Shake(0.5f, 0.3f);

        if (phase2Effect != null) phase2Effect.Play();
        bossHPUI?.ActivatePhase2();

        yield return new WaitForSeconds(1f);

        if (phase2Music != null)
            RhythmManager.Instance?.StartMusic(phase2Music, 140f);
    }

    private void TryDropHeal()
    {
        if (musicNoteHealPrefab == null) return;
        healDropCount++;
        Instantiate(musicNoteHealPrefab,
            transform.position + Vector3.up * 1f,
            Quaternion.identity);
    }

    private IEnumerator Die()
    {
        isDead       = true;
        isBossActive = false;

        StopCoroutine(nameof(DamageFlash));
        isFlashing = false;
        isHurt     = false;

        // Restaura cores antes do fade
        for (int i = 0; i < bodyParts.Length; i++)
            if (bodyParts[i] != null)
                bodyParts[i].color = originalColors[i];

        anim?.SetTrigger("Death");
        PlaySFX(deathSFX);
        CameraShake.Instance?.Shake(0.8f, 0.4f);

        bossHPUI?.Hide();

        yield return new WaitForSeconds(2f);

        if (goldenDiscFragmentPrefab != null)
            Instantiate(goldenDiscFragmentPrefab,
                transform.position + Vector3.up,
                Quaternion.identity);

        OnBossDefeated?.Invoke();

        // Fade out
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            foreach (var sr in bodyParts)
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 1f - t;
                    sr.color = c;
                }
            yield return null;
        }

        Destroy(gameObject);
    }

    // ── DAMAGE FLASH ─────────────────────────────────────────────
    private IEnumerator DamageFlash()
    {
        isFlashing = true;

        foreach (var sr in bodyParts)
            if (sr != null) sr.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < bodyParts.Length; i++)
            if (bodyParts[i] != null)
                bodyParts[i].color = originalColors[i];

        isFlashing = false;
    }

    // ── HELPERS ───────────────────────────────────────────────────
    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (skateSpawnPoint != null) Gizmos.DrawWireSphere(skateSpawnPoint.position, 0.2f);
        if (waveSpawnPoint  != null) Gizmos.DrawWireSphere(waveSpawnPoint.position,  0.3f);
    }
}