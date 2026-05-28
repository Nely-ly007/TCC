using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - EnemyBase
/// Classe base para todos os inimigos do jogo.
/// IA simples com ataques sincronizados ao ritmo.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected int maxHP = 30;
    [SerializeField] protected int contactDamage = 15;     // GDD: básico 15 HP
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float detectionRange = 8f;
    [SerializeField] protected float attackRange = 1.5f;

    [Header("Drops")]
    [SerializeField] protected GameObject vinylDropPrefab;
    [SerializeField] protected GameObject musicNotePrefab;
    [SerializeField] [Range(0f, 1f)] protected float healDropChance = 0.3f;
    [SerializeField] protected int vinylDropAmount = 1;

    [Header("Ritmo")]
    [SerializeField] protected bool attackOnBeat = true;
    [SerializeField] protected int attackOnBeatNumber = 2; // 0-3 (beat 3 = index 2)

    [Header("Feedback")]
    [SerializeField] protected AudioClip attackSFX;
    [SerializeField] protected AudioClip damageSFX;
    [SerializeField] protected AudioClip deathSFX;

    protected int currentHP;
    protected bool isDead;
    protected bool isAttacking;
    protected Transform player;
    protected Rigidbody2D rb;
    protected Animator anim;
    protected AudioSource audioSource;
    protected SpriteRenderer spriteRenderer;

    // Estado de IA
    protected enum EnemyState { Idle, Patrol, Chase, Attack, Dead }
    protected EnemyState currentState = EnemyState.Idle;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHP = maxHP;
    }

    protected virtual void Start()
    {
        player = PlayerController.Instance?.transform;
        if (attackOnBeat)
            RhythmManager.OnBeatNumberStatic += OnRhythmBeat;
    }

    protected virtual void OnDestroy()
    {
        RhythmManager.OnBeatNumberStatic -= OnRhythmBeat;
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;
        UpdateState();
        ExecuteState();
    }

    protected virtual void UpdateState()
    {
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (distToPlayer <= attackRange)
            currentState = EnemyState.Attack;
        else if (distToPlayer <= detectionRange)
            currentState = EnemyState.Chase;
        else
            currentState = EnemyState.Patrol;
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
                // Ataque acontece no beat via OnRhythmBeat
                break;
        }
    }

    protected virtual void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        // Vira sprite
        if (direction.x != 0)
            transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);

        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    protected virtual void Patrol()
    {
        // Implementação básica - subclasses podem sobrescrever
        anim.SetFloat("Speed", 0);
    }

    // ── RITMO ────────────────────────────────────────────────────
    protected virtual void OnRhythmBeat(int beatNumber)
    {
        if (isDead || isAttacking) return;
        if (beatNumber == attackOnBeatNumber && currentState == EnemyState.Attack)
        {
            StartCoroutine(PerformRhythmAttack());
        }
    }

    protected virtual IEnumerator PerformRhythmAttack()
    {
        isAttacking = true;
        anim.SetTrigger("Attack");
        PlaySFX(attackSFX);

        // Dano de contato (pode ser sobrescrito)
        yield return new WaitForSeconds(0.1f);

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange)
            PlayerController.Instance?.TakeDamage(contactDamage);

        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    // ── DANO ─────────────────────────────────────────────────────
    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP -= amount;
        PlaySFX(damageSFX);
        StartCoroutine(DamageFlash());

        // Knockback leve (GDD: "recuam levemente")
        Vector2 knockDir = (transform.position - player.position).normalized;
        rb.AddForce(knockDir * 3f, ForceMode2D.Impulse);

        if (currentHP <= 0)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        GetComponent<Collider2D>().enabled = false;

        anim.SetTrigger("Death");
        PlaySFX(deathSFX);

        DropLoot();
        StartCoroutine(DestroyAfterDeath());
    }

    protected virtual void DropLoot()
    {
        // Drop de vinis
        if (vinylDropPrefab != null)
        {
            for (int i = 0; i < vinylDropAmount; i++)
            {
                Vector2 dropPos = (Vector2)transform.position +
                    Random.insideUnitCircle * 0.5f;
                Instantiate(vinylDropPrefab, dropPos, Quaternion.identity);
            }
        }

        // Chance de drop de nota musical (cura)
        if (musicNotePrefab != null && Random.value <= healDropChance)
            Instantiate(musicNotePrefab, transform.position, Quaternion.identity);
    }

    protected IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(1f);

        // Fade out
        float t = 0;
        Color c = spriteRenderer.color;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            c.a = 1f - (t / 0.5f);
            spriteRenderer.color = c;
            yield return null;
        }

        Destroy(gameObject);
    }

    protected IEnumerator DamageFlash()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original;
    }

    protected void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Dano de contato com o player
        if (collision.gameObject.CompareTag("Player") && !isDead)
            PlayerController.Instance?.TakeDamage(contactDamage);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
