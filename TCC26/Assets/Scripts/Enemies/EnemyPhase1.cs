using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - EnemyDiscoDancer
/// Inimigo básico da Fase 1 (Disco Fever).
/// Anda lentamente e dá dano de contato sincronizado.
/// </summary>
public class EnemyDiscoDancer : EnemyBase
{
    [Header("Disco Dancer")]
    [SerializeField] private float patrolDistance = 4f;
    [SerializeField] private float pauseBetweenPatrol = 0.5f;

    private Vector3 patrolStart;
    private bool patrolRight = true;
    private float patrolTimer;

    protected override void Awake()
    {
        base.Awake();
        moveSpeed = 1.5f;           // Anda devagar
        contactDamage = 15;         // GDD: básico 15 HP
        attackOnBeatNumber = 0;     // Ataca no beat 1
    }

    protected override void Start()
    {
        base.Start();
        patrolStart = transform.position;
    }

    protected override void Patrol()
    {
        patrolTimer += Time.deltaTime;

        if (patrolTimer < pauseBetweenPatrol) return;

        float targetX = patrolRight ?
            patrolStart.x + patrolDistance :
            patrolStart.x - patrolDistance;

        Vector2 dir = new Vector2(targetX - transform.position.x, 0).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        transform.localScale = new Vector3(Mathf.Sign(dir.x), 1, 1);

        if (Mathf.Abs(transform.position.x - targetX) < 0.2f)
        {
            patrolRight = !patrolRight;
            patrolTimer = 0;
        }

        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }
}

/// <summary>
/// POP ADVENTURE - EnemyDiscoBall
/// Bola de disco que cai do teto sincronizada com o beat.
/// </summary>
public class EnemyDiscoBall : MonoBehaviour
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float warningTime = 0.5f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool hasFallen;
    private bool hasDealtDamage;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
    }

    void OnEnable()
    {
        RhythmManager.OnBeatNumberStatic += OnBeat;
    }

    void OnDisable()
    {
        RhythmManager.OnBeatNumberStatic -= OnBeat;
    }

    private void OnBeat(int beatNumber)
    {
        if (hasFallen) return;
        if (beatNumber == 0) // Beat 1: mostra aviso
            StartCoroutine(WarnAndFall());
    }

    private IEnumerator WarnAndFall()
    {
        // Pisca vermelho como aviso
        float t = 0;
        while (t < warningTime)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            t += 0.2f;
        }

        // Cai
        hasFallen = true;
        rb.gravityScale = 3f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDealtDamage) return;

        if (other.CompareTag("Player"))
        {
            hasDealtDamage = true;
            PlayerController.Instance?.TakeDamage(damage);
        }

        if (other.CompareTag("Ground"))
        {
            // Para no chão, some após 1s
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
            Destroy(gameObject, 1f);
        }
    }
}
