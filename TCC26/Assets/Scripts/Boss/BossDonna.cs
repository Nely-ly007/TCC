using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - BossDonna
/// Boss da Fase 1 (Disco Fever).
/// Ataques: lança patins pelo chão + ondas de choque sincronizadas.
/// Padrões no beat 1 (principal) e beat 3 (variação).
/// </summary>
public class BossDonna : MonoBehaviour, IDamageable
{
    [Header("Stats do Boss")]
    [SerializeField] private int maxHP = 300;
    [SerializeField] private int phase2HPThreshold = 150; // Entra na fase 2

    [Header("Ataques")]
    [SerializeField] private GameObject skateProjectilePrefab;  // Patim
    [SerializeField] private GameObject shockwavePrefab;        // Onda de choque no chão
    [SerializeField] private int skateDamage = 15;
    [SerializeField] private int shockwaveDamage = 15;
    [SerializeField] private float skateSpeed = 6f;
    [SerializeField] private float bossArenaWidth = 12f;

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Transform leftBound;
    [SerializeField] private Transform rightBound;

    [Header("Drops de Cura (GDD: max 2 durante luta)")]
    [SerializeField] private GameObject musicNoteHealPrefab;
    [SerializeField] private int maxHealDrops = 2;

    [Header("Efeitos")]
    [SerializeField] private AudioClip phase1Music;
    [SerializeField] private AudioClip phase2Music;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private ParticleSystem discoParticles;

    [Header("Fragmento de Recompensa")]
    [SerializeField] private GameObject goldenDiscFragmentPrefab;

    private int currentHP;
    private bool isDead;
    private bool isPhase2;
    private bool isBossActive = false;
    private int healDropCount = 0;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Transform player;

    // Padrão atual do compasso
    private int attackPattern = 0; // 0 = patins, 1 = onda de choque

    public System.Action<int, int> OnHealthChanged;
    public System.Action OnBossDefeated;

    void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        currentHP = maxHP;
    }

    void Start()
    {
        player = PlayerController.Instance?.transform;
    }

    void OnEnable()
    {
        RhythmManager.OnBeatNumberStatic += OnBeat;
    }

    void OnDisable()
    {
        RhythmManager.OnBeatNumberStatic -= OnBeat;
    }

    /// <summary>
    /// Ativa o boss (chamado por trigger de área na fase).
    /// </summary>
    public void ActivateBoss()
    {
        isBossActive = true;
        anim.SetTrigger("BossEntrance");

        if (phase1Music != null)
        {
            RhythmManager.Instance?.StartMusic(phase1Music, 120f);
        }

        if (discoParticles != null) discoParticles.Play();
    }

    private void OnBeat(int beatNumber)
    {
        if (!isBossActive || isDead) return;

        // GDD: ataques no beat 1 (index 0) e beat 3 (index 2)
        if (beatNumber == 0)
        {
            ExecuteAttackPattern();
        }
        else if (beatNumber == 2 && isPhase2)
        {
            // Fase 2: ataque extra no beat 3
            StartCoroutine(ShockwaveAttack());
        }
    }

    private void ExecuteAttackPattern()
    {
        if (isDead) return;

        switch (attackPattern)
        {
            case 0: StartCoroutine(SkateAttack()); break;
            case 1: StartCoroutine(ShockwaveAttack()); break;
            case 2: StartCoroutine(DoubleSkateAttack()); break; // apenas fase 2
        }

        // Alterna padrão
        int maxPattern = isPhase2 ? 3 : 2;
        attackPattern = (attackPattern + 1) % maxPattern;
    }

    // ── ATAQUE 1: PATINS ────────────────────────────────────────
    private IEnumerator SkateAttack()
    {
        anim.SetTrigger("ThrowSkate");
        PlaySFX(attackSFX);

        yield return new WaitForSeconds(0.2f);

        // Lança patim na direção do player
        if (skateProjectilePrefab != null && player != null)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            dir.y = 0; // patim rola pelo chão
            dir.Normalize();

            GameObject skate = Instantiate(skateProjectilePrefab,
                transform.position, Quaternion.identity);
            SkateProjectile sp = skate.GetComponent<SkateProjectile>();
            sp?.Init(dir, skateSpeed, skateDamage);
        }
    }

    // ── ATAQUE 2: ONDA DE CHOQUE ─────────────────────────────────
    private IEnumerator ShockwaveAttack()
    {
        anim.SetTrigger("Shockwave");
        PlaySFX(attackSFX);

        yield return new WaitForSeconds(0.15f);

        if (shockwavePrefab != null)
        {
            // Onda para a esquerda e direita do boss
            Instantiate(shockwavePrefab,
                transform.position + Vector3.right * 0.5f,
                Quaternion.identity).GetComponent<Shockwave>()?.Init(Vector2.right, shockwaveDamage);
            Instantiate(shockwavePrefab,
                transform.position + Vector3.left * 0.5f,
                Quaternion.identity).GetComponent<Shockwave>()?.Init(Vector2.left, shockwaveDamage);
        }
    }

    // ── ATAQUE 3 (FASE 2): DUPLO PATIM ──────────────────────────
    private IEnumerator DoubleSkateAttack()
    {
        yield return StartCoroutine(SkateAttack());
        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(SkateAttack());
    }

    // ── DANO ─────────────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        PlaySFX(damageSFX);
        StartCoroutine(DamageFlash());

        // Drop de cura (máx 2 durante a luta - GDD)
        if (currentHP < maxHP * 0.5f && healDropCount < maxHealDrops)
        {
            TryDropHeal();
        }

        // Transição para fase 2
        if (!isPhase2 && currentHP <= phase2HPThreshold)
            EnterPhase2();

        if (currentHP <= 0)
            StartCoroutine(Die());
    }

    private void EnterPhase2()
    {
        isPhase2 = true;
        anim.SetTrigger("Phase2");

        if (phase2Music != null)
            RhythmManager.Instance?.StartMusic(phase2Music, 140f); // BPM mais alto na fase 2

        // Efeito visual de transição
        CameraShake.Instance?.Shake(0.5f, 0.3f);
        moveSpeed *= 1.4f;
    }

    private void TryDropHeal()
    {
        if (musicNoteHealPrefab != null && healDropCount < maxHealDrops)
        {
            healDropCount++;
            Vector3 dropPos = transform.position + Vector3.up;
            Instantiate(musicNoteHealPrefab, dropPos, Quaternion.identity);
        }
    }

    private IEnumerator Die()
    {
        isDead = true;
        isBossActive = false;
        anim.SetTrigger("Death");
        PlaySFX(deathSFX);
        CameraShake.Instance?.Shake(0.8f, 0.4f);

        yield return new WaitForSeconds(2f);

        // Dropa o fragmento do Disco Dourado
        if (goldenDiscFragmentPrefab != null)
            Instantiate(goldenDiscFragmentPrefab, transform.position, Quaternion.identity);

        OnBossDefeated?.Invoke();

        // Fade e destroy
        float t = 0;
        Color c = spriteRenderer.color;
        while (t < 1f)
        {
            t += Time.deltaTime;
            c.a = 1f - t;
            spriteRenderer.color = c;
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        spriteRenderer.color = Color.magenta; // Cor temática disco
        yield return new WaitForSeconds(0.06f);
        spriteRenderer.color = Color.white;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}

// ── PROJÉTIL PATIM ───────────────────────────────────────────────
public class SkateProjectile : MonoBehaviour
{
    private int damage;
    private Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    public void Init(Vector2 dir, float speed, int dmg)
    {
        damage = dmg;
        rb.linearVelocity = dir * speed;
        // Gira o skate visualmente
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Destroy(gameObject, 4f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController.Instance?.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}

// ── ONDA DE CHOQUE ───────────────────────────────────────────────
public class Shockwave : MonoBehaviour
{
    private int damage;
    private Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    public void Init(Vector2 dir, int dmg)
    {
        damage = dmg;
        rb.linearVelocity = dir * 4f;
        Destroy(gameObject, 3f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController.Instance?.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
