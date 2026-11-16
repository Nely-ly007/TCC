
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
    [SerializeField] private float riseTime = 0.4f;
    [SerializeField] private float fallTime = 0.5f;

    [Header("Combate")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject attackProjectilePrefab;

    [Header("Vida")]
    [SerializeField] private int maxHP = 100;
    private int currentHP;
    private int bonusMaxHP = 0;
    private int bonusDamage = 0;
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

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private string idleAnimationName = "Idle";

    // Componentes
    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;
    private SpriteRenderer[] bodyParts;

    // Estado
    private bool isGrounded;
    private bool isAttacking;
    private bool isDead;
    private bool isInvincible;

    private float lastAttackTime = -999f;
    private float horizontalInput;

    private Vector3 checkpointPosition;
    private bool hasMicrophoneRevive = false;

    private Coroutine attackCoroutine;
    private Coroutine damageFlashCoroutine;

    public System.Action<int, int> OnHealthChanged;
    public System.Action OnPlayerDied;
    public System.Action OnPlayerRevived;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        bodyParts = GetComponentsInChildren<SpriteRenderer>();

        ApplyGravityScaleFromJumpParams();
    }

    void Start()
    {
        currentHP = TotalMaxHP;
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

    // MOVIMENTO
    private void HandleMovement()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed,
            rb.linearVelocity.y
        );

        if (horizontalInput != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(horizontalInput), 1, 1
            );
        }
    }

    // PULO
    private void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        bool jumpPressed =
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow);

        if (jumpPressed && isGrounded)
        {
            float gravity = Physics2D.gravity.y * rb.gravityScale;

            float force = Mathf.Sqrt(
                2f * Mathf.Abs(gravity) *
                (jumpHeight * bonusJumpMultiplier)
            );

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x, force
            );

            PlaySFX(jumpSFX);
            anim.SetTrigger("Jump");
        }

        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = GetFallGravityScale();
        }
        else if (rb.linearVelocity.y > 0 &&
                 !Input.GetKey(KeyCode.Space) &&
                 !Input.GetKey(KeyCode.W) &&
                 !Input.GetKey(KeyCode.UpArrow))
        {
            rb.gravityScale = GetRiseGravityScale() * 1.5f;
        }
        else
        {
            rb.gravityScale = GetRiseGravityScale();
        }
    }

    // ATAQUE
    private void HandleAttack()
    {
        bool attackPressed =
            Input.GetKeyDown(KeyCode.Z) ||
            Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.JoystickButton2);

        if (attackPressed && !isAttacking &&
            Time.time >= lastAttackTime + attackCooldown)
        {
            attackCoroutine = StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        anim.SetTrigger("Attack");
        PlaySFX(attackSFX);

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
        attackCoroutine = null;
    }

    /// <summary>
    /// Chamado pelo Animation Event no frame 5.
    /// </summary>
    public void SpawnProjectileEvent()
    {
        if (isDead) return;

        Vector2 direction = transform.localScale.x > 0
            ? Vector2.right
            : Vector2.left;

        if (attackProjectilePrefab == null)
        {
            Debug.LogWarning(
                "[PlayerController] Attack Projectile Prefab não atribuído!"
            );
            return;
        }

        if (attackPoint == null)
        {
            Debug.LogWarning(
                "[PlayerController] Attack Point não atribuído!"
            );
            return;
        }

        GameObject proj = Instantiate(
            attackProjectilePrefab,
            attackPoint.position,
            Quaternion.identity
        );

        MusicProjectile mp = proj.GetComponent<MusicProjectile>();

        if (mp != null)
            mp.Init(direction, attackDamage + bonusDamage);
        else
            Debug.LogWarning(
                "[PlayerController] Prefab sem MusicProjectile.cs!"
            );
    }

    private void PerformHitbox()
    {
        if (attackPoint == null || isDead) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (var hit in hits)
            hit.GetComponent<IDamageable>()?.TakeDamage(
                attackDamage + bonusDamage
            );
    }

    // DANO E VIDA
    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible) return;

        currentHP = Mathf.Max(0, currentHP - amount);

        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);

        PlaySFX(damageSFX);

        damageFlashCoroutine = StartCoroutine(DamageFlash());

        if (currentHP <= 0)
            HandleDeath();
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHP = Mathf.Min(
            TotalMaxHP,
            currentHP + amount
        );

        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }

    private void HandleDeath()
    {
        if (isDead) return;

        // Revive pelo microfone
        if (hasMicrophoneRevive)
        {
            hasMicrophoneRevive = false;

            currentHP = TotalMaxHP / 2;

            OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
            OnPlayerRevived?.Invoke();

            return;
        }

        isDead = true;

        horizontalInput = 0f;
        rb.linearVelocity = Vector2.zero;

        PlaySFX(deathSFX);

        anim.ResetTrigger("Jump");
        anim.ResetTrigger("Attack");
        anim.SetTrigger("Death");

        OnPlayerDied?.Invoke();

        StartCoroutine(RespawnAtCheckpoint());
    }

    // RESPAWN
    private IEnumerator RespawnAtCheckpoint()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Restaura posição
        transform.position = checkpointPosition;

        // Zera física
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Restaura gravidade
        ApplyGravityScaleFromJumpParams();

        // Restaura vida
        currentHP = TotalMaxHP;

        // Restaura estados
        isDead = false;
        isInvincible = false;
        isAttacking = false;
        isGrounded = false;
        horizontalInput = 0f;

        lastAttackTime = -999f;

        // Interrompe possíveis rotinas anteriores
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = null;
        }

        // Garante que todas as partes do personagem estejam visíveis
        if (bodyParts != null)
        {
            foreach (var sr in bodyParts)
            {
                if (sr != null)
                    sr.enabled = true;
            }
        }

        // LIMPEZA COMPLETA DO ANIMATOR
        anim.enabled = true;

        anim.ResetTrigger("Death");
        anim.ResetTrigger("Attack");
        anim.ResetTrigger("Jump");

        anim.SetFloat("Speed", 0f);
        anim.SetFloat("VerticalVelocity", 0f);
        anim.SetBool("IsGrounded", false);

        // Reinicia o Animator e remove o estado anterior
        anim.Rebind();
        anim.Update(0f);

        // Força a animação Idle inicial
        anim.Play(idleAnimationName, 0, 0f);
        anim.Update(0f);

        // Atualiza interface
        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }

    public void SetCheckpoint(Vector3 pos)
    {
        checkpointPosition = pos;
    }

    // UPGRADES
    public void ApplyDamageUpgrade(int bonus)
    {
        bonusDamage += bonus;
    }

    public void ApplyJumpUpgrade(float bonus)
    {
        bonusJumpMultiplier += bonus;
    }

    public void ApplyVitalityUpgrade(int bonus)
    {
        bonusMaxHP += bonus;

        currentHP = Mathf.Min(
            currentHP + bonus,
            TotalMaxHP
        );

        OnHealthChanged?.Invoke(currentHP, TotalMaxHP);
    }

    public void PickupMicrophone()
    {
        hasMicrophoneRevive = true;
    }

    public int TotalMaxHP => maxHP + bonusMaxHP;

    // DAMAGE FLASH
    private IEnumerator DamageFlash()
    {
        isInvincible = true;

        float timer = 0f;

        while (timer < 0.8f)
        {
            if (bodyParts != null && bodyParts.Length > 0)
            {
                bool show = !bodyParts[0].enabled;

                foreach (var sr in bodyParts)
                {
                    if (sr != null)
                        sr.enabled = show;
                }
            }

            yield return new WaitForSeconds(0.08f);

            timer += 0.08f;
        }

        if (bodyParts != null)
        {
            foreach (var sr in bodyParts)
            {
                if (sr != null)
                    sr.enabled = true;
            }
        }

        isInvincible = false;
        damageFlashCoroutine = null;
    }

    // ANIMAÇÕES
    private void UpdateAnimations()
    {
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));

        anim.SetBool("IsGrounded", isGrounded);

        anim.SetFloat(
            "VerticalVelocity",
            rb.linearVelocity.y
        );
    }

    // FÍSICA
    private void ApplyGravityScaleFromJumpParams()
    {
        float g_eff = 2f * jumpHeight /
                      (riseTime * riseTime);

        rb.gravityScale = g_eff /
                          Mathf.Abs(Physics2D.gravity.y);
    }

    private float GetRiseGravityScale()
    {
        return (2f * jumpHeight / (riseTime * riseTime)) /
               Mathf.Abs(Physics2D.gravity.y);
    }

    private float GetFallGravityScale()
    {
        return (2f * jumpHeight / (fallTime * fallTime)) /
               Mathf.Abs(Physics2D.gravity.y);
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

            Gizmos.DrawWireSphere(
                attackPoint.position,
                attackRange
            );
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundCheckRadius
            );
        }
    }
}
