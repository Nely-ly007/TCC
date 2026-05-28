using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - PlayerController
/// Controla Enzo: movimento, pulo e ataque musical.
/// Parâmetros exatos do GDD implementados.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    // ── STATS (GDD) ──────────────────────────────────────────────
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;          // GDD: 5 unidades/s
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpHeight = 3f;         // GDD: 3 unidades
    [SerializeField] private float riseTime = 0.4f;         // GDD: 0.4s
    [SerializeField] private float fallTime = 0.5f;         // GDD: 0.5s

    [Header("Combate")]
    [SerializeField] private float attackCooldown = 0.4f;   // GDD: 0.4s
    [SerializeField] private float hitboxDuration = 0.2f;   // GDD: 0.2s
    [SerializeField] private int attackDamage = 10;         // GDD: 10 dano base
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform attackPoint;

    [Header("Vida")]
    [SerializeField] private int maxHP = 100;               // GDD: 100 HP
    private int currentHP;
    private int bonusMaxHP = 0;                             // upgrades
    private int bonusDamage = 0;
    private float bonusJumpMultiplier = 1f;

    [Header("Efeitos")]
    [SerializeField] private GameObject attackProjectilePrefab;
    [SerializeField] private GameObject damageFlashEffect;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip deathSFX;

    // ── COMPONENTES ───────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;

    // ── ESTADO ───────────────────────────────────────────────────
    private bool isGrounded;
    private bool isAttacking;
    private bool isDead;
    private bool isInvincible;
    private float lastAttackTime = -999f;
    private float horizontalInput;
    private Vector3 checkpointPosition;

    // ── EXTRA LIFE ───────────────────────────────────────────────
    private bool hasMicrophoneRevive = false;

    // ── EVENTS ───────────────────────────────────────────────────
    public System.Action<int, int> OnHealthChanged;   // (current, max)
    public System.Action OnPlayerDied;
    public System.Action OnPlayerRevived;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ajusta gravidade para corresponder aos tempos de subida/queda do GDD
        ApplyGravityScaleFromJumpParams();
    }

    void Start()
    {
        currentHP = maxHP + bonusMaxHP;
        checkpointPosition = transform.position;
        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }

    void Update()
    {
        if (isDead) return;

        HandleMovement();
        HandleJump();
        HandleAttack();
        UpdateAnimations();
    }

    // ── MOVIMENTO ────────────────────────────────────────────────
    private void HandleMovement()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        // Vira o sprite na direção correta
        if (horizontalInput != 0)
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
    }

    private void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Força calculada para atingir a altura desejada em riseTime
            float gravity = Physics2D.gravity.y * rb.gravityScale;
            float calculatedJumpForce = Mathf.Sqrt(2f * Mathf.Abs(gravity) * (jumpHeight * bonusJumpMultiplier));
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, calculatedJumpForce);

            PlaySFX(jumpSFX);
            anim.SetTrigger("Jump");
        }

        // Gravidade assimétrica (subida mais suave, queda mais rápida)
        if (rb.linearVelocity.y < 0)
            rb.gravityScale = GetFallGravityScale();
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
            rb.gravityScale = GetRiseGravityScale() * 1.5f; // pulo curto ao soltar
        else
            rb.gravityScale = GetRiseGravityScale();
    }

    // ── ATAQUE ───────────────────────────────────────────────────
    private void HandleAttack()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        anim.SetTrigger("Attack");
        PlaySFX(attackSFX);

        // Lança projétil OU verifica hitbox
        if (attackProjectilePrefab != null)
        {
            SpawnProjectile();
        }
        else
        {
            // Hitbox direto por hitboxDuration
            yield return new WaitForSeconds(0.05f); // pequeno delay para animação
            PerformHitbox();
        }

        yield return new WaitForSeconds(hitboxDuration);
        isAttacking = false;
    }

    private void SpawnProjectile()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        GameObject proj = Instantiate(attackProjectilePrefab,
            attackPoint.position, Quaternion.identity);
        MusicProjectile projectile = proj.GetComponent<MusicProjectile>();
        if (projectile != null)
            projectile.Init(direction, attackDamage + bonusDamage);
    }

    private void PerformHitbox()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position, attackRange, enemyLayer);

        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            damageable?.TakeDamage(attackDamage + bonusDamage);
        }
    }

    // ── DANO & VIDA ──────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);

        PlaySFX(damageSFX);
        CameraShake.Instance?.Shake(0.2f, 0.15f);
        StartCoroutine(DamageFlash());

        if (currentHP <= 0)
            HandleDeath();
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(TotalMaxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }

    private void HandleDeath()
    {
        if (hasMicrophoneRevive)
        {
            // Microfone: revive automaticamente com HP parcial
            hasMicrophoneRevive = false;
            currentHP = TotalMaxHP / 2;
            OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
            OnPlayerRevived?.Invoke();
            return;
        }

        isDead = true;
        PlaySFX(deathSFX);
        anim.SetTrigger("Death");
        OnPlayerDied?.Invoke();
        StartCoroutine(RespawnAtCheckpoint());
    }

    private IEnumerator RespawnAtCheckpoint()
    {
        yield return new WaitForSeconds(1.5f);
        transform.position = checkpointPosition;
        currentHP = TotalMaxHP;
        isDead = false;
        isInvincible = false;
        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
    }

    // ── UPGRADES (Hub) ───────────────────────────────────────────
    public void ApplyDamageUpgrade(int bonus) { bonusDamage += bonus; }
    public void ApplyJumpUpgrade(float bonus) { bonusJumpMultiplier += bonus; }
    public void ApplyVitalityUpgrade(int bonusHP)
    {
        bonusMaxHP += bonusHP;
        currentHP = Mathf.Min(currentHP + bonusHP, TotalMaxHP);
        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }

    // ── POWER-UPS ────────────────────────────────────────────────
    public void PickupMicrophone() { hasMicrophoneRevive = true; }

    // ── HELPERS ──────────────────────────────────────────────────
    public int TotalMaxHP => maxHP + bonusMaxHP;

    private IEnumerator DamageFlash()
    {
        isInvincible = true;
        float timer = 0f;
        float invincibilityDuration = 0.8f;

        while (timer < invincibilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.08f);
            timer += 0.08f;
        }
        spriteRenderer.enabled = true;
        isInvincible = false;
    }

    private void UpdateAnimations()
    {
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    private void ApplyGravityScaleFromJumpParams()
    {
        // Deriva a escala de gravidade a partir dos tempos do GDD
        // g_eff = 2 * h / t_rise^2
        // gravityScale = g_eff / Physics2D.gravity.y
        float h = jumpHeight;
        float g_eff = 2f * h / (riseTime * riseTime);
        rb.gravityScale = g_eff / Mathf.Abs(Physics2D.gravity.y);
    }

    private float GetRiseGravityScale()
    {
        float h = jumpHeight;
        float g_eff = 2f * h / (riseTime * riseTime);
        return g_eff / Mathf.Abs(Physics2D.gravity.y);
    }

    private float GetFallGravityScale()
    {
        float h = jumpHeight;
        float g_eff = 2f * h / (fallTime * fallTime);
        return g_eff / Mathf.Abs(Physics2D.gravity.y);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
