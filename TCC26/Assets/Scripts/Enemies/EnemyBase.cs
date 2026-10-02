using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - EnemyBase
/// Classe base de todos os inimigos.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected int maxHP = 30;
    [SerializeField] protected int contactDamage = 15;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float detectionRange = 8f;
    [SerializeField] protected float attackRange = 1.5f;

    [Header("Drops")]
    [SerializeField] protected GameObject vinylDropPrefab;
    [SerializeField] protected GameObject musicNotePrefab;
    [SerializeField] [Range(0f, 1f)]
    protected float healDropChance = 0.3f;
    [SerializeField] protected int vinylDropAmount = 1;

    [Header("Ritmo")]
    [SerializeField] protected bool attackOnBeat = true;
    [SerializeField] protected int attackOnBeatNumber = 2;

    [Header("Feedback")]
    [SerializeField] protected AudioClip attackSFX;
    [SerializeField] protected AudioClip damageSFX;
    [SerializeField] protected AudioClip deathSFX;

    // ============================================================
    // ESTADO
    // ============================================================

    protected int currentHP;
    protected bool isDead;
    protected bool isAttacking;

    protected Transform player;
    protected Rigidbody2D rb;
    protected Animator anim;
    protected AudioSource audioSource;
    protected SpriteRenderer spriteRenderer;

    protected enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }

    protected EnemyState currentState = EnemyState.Idle;

    // ============================================================
    // AWAKE
    // ============================================================

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentHP = maxHP;

        isDead = false;
        isAttacking = false;
    }

    // ============================================================
    // START
    // ============================================================

    protected virtual void Start()
    {
        player = PlayerController.Instance?.transform;

        if (attackOnBeat)
            RhythmManager.OnBeatNumberStatic += OnRhythmBeat;
    }

    // ============================================================
    // UPDATE
    // ============================================================

    protected virtual void Update()
    {
        if (isDead || player == null)
            return;

        UpdateState();
        ExecuteState();
    }

    // ============================================================
    // DESTROY
    // ============================================================

    protected virtual void OnDestroy()
    {
        RhythmManager.OnBeatNumberStatic -= OnRhythmBeat;
    }

    // ============================================================
    // IA
    // ============================================================

    protected virtual void UpdateState()
    {
        if (player == null)
            return;

        float distToPlayer =
            Vector2.Distance(
                transform.position,
                player.position
            );

        if (distToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        else if (distToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
        }
        else
        {
            currentState = EnemyState.Patrol;
        }
    }

    protected virtual void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Chase:
                ChasePlayer();
                break;

            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Attack:
                // Ataque acontece pelo beat.
                break;
        }
    }

    // ============================================================
    // CHASE
    // ============================================================

    protected virtual void ChasePlayer()
    {
        if (player == null)
            return;

        Vector2 direction =
            (player.position - transform.position).normalized;

        rb.linearVelocity =
            new Vector2(
                direction.x * moveSpeed,
                rb.linearVelocity.y
            );

        if (direction.x != 0)
        {
            transform.localScale =
                new Vector3(
                    Mathf.Sign(direction.x),
                    1f,
                    1f
                );
        }

        anim.SetFloat(
            "Speed",
            Mathf.Abs(rb.linearVelocity.x)
        );
    }

    // ============================================================
    // PATROL
    // ============================================================

    protected virtual void Patrol()
    {
        anim.SetFloat("Speed", 0f);
    }

    // ============================================================
    // RITMO
    // ============================================================

    protected virtual void OnRhythmBeat(int beatNumber)
    {
        if (isDead || isAttacking)
            return;

        if (beatNumber == attackOnBeatNumber &&
            currentState == EnemyState.Attack)
        {
            StartCoroutine(
                PerformRhythmAttack()
            );
        }
    }

    protected virtual IEnumerator PerformRhythmAttack()
    {
        isAttacking = true;

        anim.SetTrigger("Attack");

        PlaySFX(attackSFX);

        yield return new WaitForSeconds(0.1f);

        if (player != null)
        {
            float dist =
                Vector2.Distance(
                    transform.position,
                    player.position
                );

            if (dist <= attackRange)
            {
                PlayerController.Instance?.TakeDamage(
                    contactDamage
                );
            }
        }

        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
    }

    // ============================================================
    // RECEBER DANO
    // ============================================================

    public virtual void TakeDamage(int amount)
    {
        if (isDead)
            return;

        if (amount <= 0)
            return;

        // Aplica dano
        currentHP -= amount;

        // Garante que nunca fique abaixo de zero
        currentHP = Mathf.Max(
            currentHP,
            0
        );

        // Som de dano
        PlaySFX(damageSFX);

        // Feedback visual
        StartCoroutine(
            DamageFlash()
        );

        // Knockback desativado
        // Mantido assim para evitar que o inimigo seja
        // arremessado para fora da posição.

        // Morte somente quando chegar a 0 HP
        if (currentHP <= 0)
        {
            Die();
        }
    }

    // ============================================================
    // MORTE
    // ============================================================

    protected virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;

        currentState =
            EnemyState.Dead;

        rb.linearVelocity =
            Vector2.zero;

        rb.gravityScale = 0f;

        Collider2D col =
            GetComponent<Collider2D>();

        if (col != null)
            col.enabled = false;

        if (anim != null)
            anim.SetTrigger("Death");

        PlaySFX(deathSFX);

        DropLoot();

        StartCoroutine(
            DestroyAfterDeath()
        );
    }

    // ============================================================
    // LOOT
    // ============================================================

    protected virtual void DropLoot()
    {
        if (vinylDropPrefab != null)
        {
            for (int i = 0;
                 i < vinylDropAmount;
                 i++)
            {
                Vector2 dropPos =
                    (Vector2)transform.position +
                    Random.insideUnitCircle * 0.5f;

                Instantiate(
                    vinylDropPrefab,
                    dropPos,
                    Quaternion.identity
                );
            }
        }

        if (musicNotePrefab != null &&
            Random.value <= healDropChance)
        {
            Instantiate(
                musicNotePrefab,
                transform.position,
                Quaternion.identity
            );
        }
    }

    // ============================================================
    // DESTRUIÇÃO APÓS MORTE
    // ============================================================

    protected IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(1f);

        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        float t = 0f;

        Color c =
            spriteRenderer.color;

        while (t < 0.5f)
        {
            t += Time.deltaTime;

            c.a =
                1f -
                (t / 0.5f);

            spriteRenderer.color =
                c;

            yield return null;
        }

        Destroy(gameObject);
    }

    // ============================================================
    // FEEDBACK DE DANO
    // ============================================================

    protected IEnumerator DamageFlash()
    {
        if (spriteRenderer == null)
            yield break;

        Color originalColor =
            spriteRenderer.color;

        spriteRenderer.color =
            Color.red;

        yield return new WaitForSeconds(0.12f);

        if (!isDead &&
            spriteRenderer != null)
        {
            spriteRenderer.color =
                originalColor;
        }
    }

    // ============================================================
    // SOM
    // ============================================================

    protected void PlaySFX(AudioClip clip)
    {
        if (clip != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
