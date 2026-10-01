using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - EnemySpeakerSpike
/// Mob da Fase 1: caixa de som com espinhos.
/// Fica parado pulsando no beat e estica os espinhos
/// sincronizado com a música — causa dano ao contato.
/// </summary>
public class EnemySpeakerSpike : EnemyBase
{
    [Header("Speaker Spike")]
    [SerializeField] private Transform spikesTransform;  // filho com os espinhos
    [SerializeField] private float     spikeExtendScale = 1.4f;  // escala ao esticar
    [SerializeField] private float     spikeRetractScale = 1f;
    [SerializeField] private float     extendDuration = 0.08f;
    [SerializeField] private float     retractDuration = 0.15f;
    [SerializeField] private bool      moveHorizontally = false; // variante que se move
    [SerializeField] private float     moveDistance = 2f;
    [SerializeField] private float     moveSpeed = 1.5f;

    // Estado dos espinhos
    private bool spikesExtended = false;
    private bool isAnimatingSpike = false;
    private Vector3 patrolStart;
    private bool movingRight = true;

    protected override void Awake()
    {
        base.Awake();
        // Sobrescreve stats base para este mob
        contactDamage    = 15;
        maxHP            = 20;    // menos HP — é um obstáculo
        attackOnBeat     = true;
        attackOnBeatNumber = 0;   // estica espinhos no beat 1
        detectionRange   = 0f;    // não persegue o player
        moveSpeed        = this.moveSpeed;
    }

    protected override void Start()
    {
        base.Start();
        patrolStart = transform.position;

        // Garante escala inicial dos espinhos
        if (spikesTransform != null)
            spikesTransform.localScale = Vector3.one * spikeRetractScale;
    }

    // ── SUBSTITUI O CHASE — não persegue ────────────────────────
    protected override void ChasePlayer()
    {
        // Não persegue — só patrulha se configurado
        if (moveHorizontally) PatrolHorizontal();
    }

    protected override void Patrol()
    {
        if (moveHorizontally) PatrolHorizontal();
        // Se não move, fica parado
    }

    private void PatrolHorizontal()
    {
        float targetX = movingRight
            ? patrolStart.x + moveDistance
            : patrolStart.x - moveDistance;

        Vector2 dir = new Vector2(targetX - transform.position.x, 0).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

        if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
            movingRight = !movingRight;
    }

    // ── BEAT: estica espinhos ─────────────────────────────────────
    protected override void OnRhythmBeat(int beatNumber)
    {
        if (isDead || isAnimatingSpike) return;

        // Beat 1: estica espinhos (ataque)
        // Beat 3: recolhe espinhos
        if (beatNumber == 0)
            StartCoroutine(ExtendSpikes());
        else if (beatNumber == 2)
            StartCoroutine(RetractSpikes());
    }

    private IEnumerator ExtendSpikes()
    {
        if (spikesTransform == null) yield break;
        isAnimatingSpike = true;
        spikesExtended   = true;

        float t = 0f;
        while (t < extendDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(spikeRetractScale, spikeExtendScale,
                                  Mathf.SmoothStep(0f, 1f, t / extendDuration));
            spikesTransform.localScale = Vector3.one * s;
            yield return null;
        }
        spikesTransform.localScale = Vector3.one * spikeExtendScale;
        isAnimatingSpike = false;
    }

    private IEnumerator RetractSpikes()
    {
        if (spikesTransform == null) yield break;
        isAnimatingSpike = true;
        spikesExtended   = false;

        float t = 0f;
        while (t < retractDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(spikeExtendScale, spikeRetractScale,
                                  Mathf.SmoothStep(0f, 1f, t / retractDuration));
            spikesTransform.localScale = Vector3.one * s;
            yield return null;
        }
        spikesTransform.localScale = Vector3.one * spikeRetractScale;
        isAnimatingSpike = false;
    }

    // ── DANO AO CONTATO ───────────────────────────────────────────
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        // Só causa dano se espinhos estiverem estendidos
        if (spikesExtended)
            PlayerController.Instance?.TakeDamage(contactDamage);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (spikesExtended)
            PlayerController.Instance?.TakeDamage(contactDamage);
    }

    // ── OVERRIDE: não morre caindo — é um obstáculo fixo ─────────
    protected override void Die()
    {
        isDead = true;
        // Animação simples: some
        StartCoroutine(DestroyCoroutine());
    }

    private IEnumerator DestroyCoroutine()
    {
        DropLoot();
        // Pisca e some
        SpriteRenderer sr = GetComponent<SpriteRenderer>()
                         ?? GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            float t = 0f;
            Color c = sr.color;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                c.a = 1f - (t / 0.4f);
                sr.color = c;
                yield return null;
            }
        }
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (!moveHorizontally) return;
        Gizmos.color = Color.yellow;
        Vector3 start = Application.isPlaying ? patrolStart : transform.position;
        Gizmos.DrawLine(start + Vector3.left  * moveDistance, 
                        start + Vector3.right * moveDistance);
    }
}
