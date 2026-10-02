using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - BossDonna v2
/// - Métodos públicos para Animation Events (SpawnSkateEvent, SpawnWaveEvent)
/// - Triggers de animação integrados
/// - Hurt animation ao receber dano
/// - DamageFlash via SpriteRenderers dos filhos (partes separadas)
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
    [SerializeField] private Transform  skateSpawnPoint; // mão direita
    [SerializeField] private Transform  waveSpawnPoint;  // centro do corpo

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

    // ── ESTADO ───────────────────────────────────────────────────
    private int         currentHP;
    private bool        isDead;
    private bool        isPhase2;
    private bool        isBossActive;
    private int         healDropCount = 0;
    private int         attackPattern = 0;

    // ── COMPONENTES ───────────────────────────────────────────────
    private Animator          anim;
    private AudioSource       audioSource;
    private SpriteRenderer[]  bodyParts;   // todas as partes do body
    private Transform         player;

    // ── EVENTS ───────────────────────────────────────────────────
    public System.Action<int, int> OnHealthChanged;
    public System.Action           OnBossDefeated;

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        anim        = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        bodyParts   = GetComponentsInChildren<SpriteRenderer>();
        currentHP   = maxHP;
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
        // O projétil é instanciado pelo Animation Event no frame 6
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator WaveAttackCoroutine()
    {
        anim?.SetTrigger("AttackWave");
        PlaySFX(attackSFX);
        // O projétil é instanciado pelo Animation Event
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator DoubleSkateCoroutine()
    {
        yield return StartCoroutine(SkateAttackCoroutine());
        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(SkateAttackCoroutine());
    }

    // ── ANIMATION EVENTS (públicos — chamados pelo Animator) ──────

    /// <summary>
    /// Coloque um Animation Event no frame 6 de Donna_Attack_Skate
    /// apontando para este método.
    /// </summary>
    /*public void SpawnSkateEvent()
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
        SkateProjectile sp = obj.GetComponent<SkateProjectile>();
        sp?.Init(dir, skateSpeed, skateDamage);
    }

    /// <summary>
    /// Coloque um Animation Event no frame 5 de Donna_Attack_Wave
    /// apontando para este método.
    /// </summary>
    public void SpawnWaveEvent()
    {
        if (shockwavePrefab == null) return;

        Vector3 spawnPos = waveSpawnPoint != null
            ? waveSpawnPoint.position
            : transform.position;

        // Onda para esquerda e direita
        var wL = Instantiate(shockwavePrefab, spawnPos + Vector3.left  * 0.5f, Quaternion.identity);
        var wR = Instantiate(shockwavePrefab, spawnPos + Vector3.right * 0.5f, Quaternion.identity);
        wL.GetComponent<Shockwave>()?.Init(Vector2.left,  shockwaveDamage);
        wR.GetComponent<Shockwave>()?.Init(Vector2.right, shockwaveDamage);
    }*/

    // ── DANO ─────────────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        bossHPUI?.UpdateHP(currentHP, maxHP);

        PlaySFX(hurtSFX);
        anim?.SetTrigger("Hurt");
        StartCoroutine(DamageFlash());

        // Drop de cura na metade do HP
        if (!isPhase2 && currentHP <= maxHP * 0.5f && healDropCount < maxHealDrops)
            TryDropHeal();

        // Transição para fase 2
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

        anim?.SetTrigger("Death");
        PlaySFX(deathSFX);
        CameraShake.Instance?.Shake(0.8f, 0.4f);

        bossHPUI?.Hide();

        yield return new WaitForSeconds(2f);

        // Dropa o fragmento
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
        // Salva cores originais
        Color[] originalColors = new Color[bodyParts.Length];
        for (int i = 0; i < bodyParts.Length; i++)
            if (bodyParts[i] != null)
                originalColors[i] = bodyParts[i].color;

        // Flash branco
        foreach (var sr in bodyParts)
            if (sr != null) sr.color = Color.white;

        yield return new WaitForSeconds(0.08f);

        // Restaura cores
        for (int i = 0; i < bodyParts.Length; i++)
            if (bodyParts[i] != null)
                bodyParts[i].color = originalColors[i];
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
        if (skateSpawnPoint != null)
            Gizmos.DrawWireSphere(skateSpawnPoint.position, 0.2f);
        if (waveSpawnPoint != null)
            Gizmos.DrawWireSphere(waveSpawnPoint.position, 0.3f);
    }
}
