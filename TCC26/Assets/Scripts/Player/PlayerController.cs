using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float riseTime   = 0.4f;
    [SerializeField] private float fallTime   = 0.5f;

    [Header("Combate")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private int   attackDamage   = 10;
    [SerializeField] private float attackRange    = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject attackProjectilePrefab;

    [Header("Vida")]
    [SerializeField] private int maxHP = 100;
    private int currentHP;
    private int bonusMaxHP          = 0;
    private int bonusDamage         = 0;
    private float bonusJumpMultiplier = 1f;

    [Header("Efeitos")]
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip deathSFX;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    // Componentes
    private Rigidbody2D  rb;
    private Animator     anim;
    private AudioSource  audioSource;
    private SpriteRenderer[] bodyParts;

    // Estado
    private bool  isGrounded;
    private bool  isAttacking;
    private bool  isDead;
    private bool  isInvincible;
    private float lastAttackTime = -999f;
    private float horizontalInput;
    private Vector3 checkpointPosition;
    private bool hasMicrophoneRevive = false;

    public System.Action<int, int> OnHealthChanged;
    public System.Action OnPlayerDied;
    public System.Action OnPlayerRevived;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        rb          = GetComponent<Rigidbody2D>();
        anim        = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        bodyParts   = GetComponentsInChildren<SpriteRenderer>();

        ApplyGravityScaleFromJumpParams();
    }

    void Start()
    {
        currentHP          = maxHP + bonusMaxHP;
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
        // Não bloqueia movimento durante ataque — pode atacar parado ou andando
        horizontalInput   = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        if (horizontalInput != 0)
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
    }

    // ── PULO ─────────────────────────────────────────────────────
    private void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, groundCheckRadius, groundLayer);

        bool jumpPressed = Input.GetKeyDown(KeyCode.Space)   ||
                           Input.GetKeyDown(KeyCode.W)       ||
                           Input.GetKeyDown(KeyCode.UpArrow);

        if (jumpPressed && isGrounded)
        {
            float gravity = Physics2D.gravity.y * rb.gravityScale;
            float force   = Mathf.Sqrt(2f * Mathf.Abs(gravity) *
                            (jumpHeight * bonusJumpMultiplier));

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            PlaySFX(jumpSFX);
            anim.SetTrigger("Jump");
        }

        // Gravidade assimétrica
        if (rb.linearVelocity.y < 0)
            rb.gravityScale = GetFallGravityScale();
        else if (rb.linearVelocity.y > 0 &&
                 !Input.GetKey(KeyCode.Space) &&
                 !Input.GetKey(KeyCode.W)     &&
                 !Input.GetKey(KeyCode.UpArrow))
            rb.gravityScale = GetRiseGravityScale() * 1.5f;
        else
            rb.gravityScale = GetRiseGravityScale();
    }

    // ── ATAQUE ───────────────────────────────────────────────────
    private void HandleAttack()
    {
        bool attackPressed = Input.GetKeyDown(KeyCode.Z)        ||
                             Input.GetMouseButtonDown(0)         ||
                             Input.GetKeyDown(KeyCode.JoystickButton2);

        // Permite atacar parado OU andando OU no ar
        // Só bloqueia se já estiver atacando ou morto
        if (attackPressed && !isAttacking &&
            Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking    = true;
        lastAttackTime = Time.time;

        anim.SetTrigger("Attack");
        PlaySFX(attackSFX);

        // Projétil instanciado pelo Animation Event no frame 5
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    /// <summary>
    /// Chamado pelo Animation Event no frame 5 do clipe Enzo_Attack.
    /// DEVE ser public.
    /// </summary>
    public void SpawnProjectileEvent()
    {
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        if (attackProjectilePrefab == null)
        {
            Debug.LogWarning("[PlayerController] Attack Projectile Prefab não atribuído!");
            return;
        }

        if (attackPoint == null)
        {
            Debug.LogWarning("[PlayerController] Attack Point não atribuído!");
            return;
        }

        GameObject proj = Instantiate(
            attackProjectilePrefab,
            attackPoint.position,
            Quaternion.identity);

        MusicProjectile mp = proj.GetComponent<MusicProjectile>();
        if (mp != null)
            mp.Init(direction, attackDamage + bonusDamage);
        else
            Debug.LogWarning("[PlayerController] Prefab do projétil não tem MusicProjectile.cs!");
    }

    private void PerformHitbox()
    {
        if (attackPoint == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position, attackRange, enemyLayer);

        foreach (var hit in hits)
            hit.GetComponent<IDamageable>()?.TakeDamage(attackDamage + bonusDamage);
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
        if (currentHP <= 0) HandleDeath();
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
            hasMicrophoneRevive = false;
            currentHP           = TotalMaxHP / 2;
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
        currentHP          = TotalMaxHP;
        isDead             = false;
        isInvincible       = false;
        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }

    public void SetCheckpoint(Vector3 pos) => checkpointPosition = pos;

    // ── UPGRADES ─────────────────────────────────────────────────
    public void ApplyDamageUpgrade(int bonus)   => bonusDamage += bonus;
    public void ApplyJumpUpgrade(float bonus)   => bonusJumpMultiplier += bonus;
    public void ApplyVitalityUpgrade(int bonus)
    {
        bonusMaxHP += bonus;
        currentHP   = Mathf.Min(currentHP + bonus, TotalMaxHP);
        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }
    public void PickupMicrophone() => hasMicrophoneRevive = true;
    public int TotalMaxHP => maxHP + bonusMaxHP;

    // ── DAMAGE FLASH ─────────────────────────────────────────────
    private IEnumerator DamageFlash()
    {
        isInvincible = true;
        float timer  = 0f;
        while (timer < 0.8f)
        {
            if (bodyParts != null && bodyParts.Length > 0)
            {
                bool show = !bodyParts[0].enabled;
                foreach (var sr in bodyParts)
                    if (sr != null) sr.enabled = show;
            }
            yield return new WaitForSeconds(0.08f);
            timer += 0.08f;
        }
        if (bodyParts != null)
            foreach (var sr in bodyParts)
                if (sr != null) sr.enabled = true;
        isInvincible = false;
    }

    // ── ANIMAÇÕES ────────────────────────────────────────────────
    private void UpdateAnimations()
    {
        anim.SetFloat("Speed",            Mathf.Abs(horizontalInput));
        anim.SetBool("IsGrounded",        isGrounded);
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    // ── FÍSICA ───────────────────────────────────────────────────
    private void ApplyGravityScaleFromJumpParams()
    {
        float g_eff     = 2f * jumpHeight / (riseTime * riseTime);
        rb.gravityScale = g_eff / Mathf.Abs(Physics2D.gravity.y);
    }
    private float GetRiseGravityScale()
    {
        return (2f * jumpHeight / (riseTime * riseTime)) / Mathf.Abs(Physics2D.gravity.y);
    }
    private float GetFallGravityScale()
    {
        return (2f * jumpHeight / (fallTime * fallTime)) / Mathf.Abs(Physics2D.gravity.y);
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