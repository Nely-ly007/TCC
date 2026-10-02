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
    [SerializeField] private Transform spikesTransform;

    [SerializeField] private float spikeExtendScale = 1.4f;
    [SerializeField] private float spikeRetractScale = 1f;

    [SerializeField] private float extendDuration = 0.08f;
    [SerializeField] private float retractDuration = 0.15f;

    [SerializeField] private bool moveHorizontally = false;

    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float moveSpeed = 1.5f;

    // Estado dos espinhos
    private bool spikesExtended = false;
    private bool isAnimatingSpike = false;

    private Vector3 patrolStart;
    private bool movingRight = true;

    // ============================================================
    // AWAKE
    // ============================================================

    protected override void Awake()
    {
        // ========================================================
        // IMPORTANTE:
        // Define os valores ANTES de chamar base.Awake().
        //
        // O EnemyBase.Awake() faz:
        //
        // currentHP = maxHP;
        //
        // Portanto, se colocássemos maxHP depois do base.Awake(),
        // currentHP poderia começar com o valor errado.
        // ========================================================

        maxHP = 30;
        contactDamage = 15;

        attackOnBeat = true;
        attackOnBeatNumber = 0;

        detectionRange = 0f;

        base.Awake();

        Debug.Log(
            $"[SPIKE INIT] {gameObject.name} configurado com " +
            $"HP {currentHP}/{maxHP}"
        );
    }

    // ============================================================
    // START
    // ============================================================

    protected override void Start()
    {
        base.Start();

        patrolStart = transform.position;

        if (spikesTransform != null)
        {
            spikesTransform.localScale =
                Vector3.one * spikeRetractScale;
        }
    }

    // ============================================================
    // CHASE
    // ============================================================

    protected override void ChasePlayer()
    {
        if (moveHorizontally)
            PatrolHorizontal();
    }

    // ============================================================
    // PATROL
    // ============================================================

    protected override void Patrol()
    {
        if (moveHorizontally)
            PatrolHorizontal();
    }

    private void PatrolHorizontal()
    {
        float targetX = movingRight
            ? patrolStart.x + moveDistance
            : patrolStart.x - moveDistance;

        Vector2 dir =
            new Vector2(
                targetX - transform.position.x,
                0
            ).normalized;

        rb.linearVelocity = new Vector2(
            dir.x * moveSpeed,
            rb.linearVelocity.y
        );

        if (Mathf.Abs(
                transform.position.x - targetX
            ) < 0.1f)
        {
            movingRight = !movingRight;
        }
    }

    // ============================================================
    // BEAT
    // ============================================================

    protected override void OnRhythmBeat(int beatNumber)
    {
        if (isDead || isAnimatingSpike)
            return;

        // Beat 1
        if (beatNumber == 0)
        {
            StartCoroutine(ExtendSpikes());
        }

        // Beat 3
        else if (beatNumber == 2)
        {
            StartCoroutine(RetractSpikes());
        }
    }

    // ============================================================
    // EXTENDER ESPINHOS
    // ============================================================

    private IEnumerator ExtendSpikes()
    {
        if (spikesTransform == null)
            yield break;

        isAnimatingSpike = true;
        spikesExtended = true;

        float t = 0f;

        while (t < extendDuration)
        {
            t += Time.deltaTime;

            float progress =
                t / extendDuration;

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            float scale =
                Mathf.Lerp(
                    spikeRetractScale,
                    spikeExtendScale,
                    smoothProgress
                );

            spikesTransform.localScale =
                Vector3.one * scale;

            yield return null;
        }

        spikesTransform.localScale =
            Vector3.one * spikeExtendScale;

        isAnimatingSpike = false;
    }

    // ============================================================
    // RECOLHER ESPINHOS
    // ============================================================

    private IEnumerator RetractSpikes()
    {
        if (spikesTransform == null)
            yield break;

        isAnimatingSpike = true;
        spikesExtended = false;

        float t = 0f;

        while (t < retractDuration)
        {
            t += Time.deltaTime;

            float progress =
                t / retractDuration;

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            float scale =
                Mathf.Lerp(
                    spikeExtendScale,
                    spikeRetractScale,
                    smoothProgress
                );

            spikesTransform.localScale =
                Vector3.one * scale;

            yield return null;
        }

        spikesTransform.localScale =
            Vector3.one * spikeRetractScale;

        isAnimatingSpike = false;
    }

    // ============================================================
    // DANO AO PLAYER
    // ============================================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        if (spikesExtended)
        {
            PlayerController.Instance?.TakeDamage(
                contactDamage
            );

            Debug.Log(
                $"[SPIKE] Dano de contato no Player: " +
                $"{contactDamage}"
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (spikesExtended)
        {
            PlayerController.Instance?.TakeDamage(
                contactDamage
            );

            Debug.Log(
                $"[SPIKE] Dano de contato no Player: " +
                $"{contactDamage}"
            );
        }
    }

    // ============================================================
    // MORTE
    // ============================================================

    protected override void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log(
            $"[SPIKE] {gameObject.name} morreu."
        );

        StartCoroutine(DestroyCoroutine());
    }

    private IEnumerator DestroyCoroutine()
    {
        DropLoot();

        SpriteRenderer sr =
            GetComponent<SpriteRenderer>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            float t = 0f;

            Color c = sr.color;

            while (t < 0.4f)
            {
                t += Time.deltaTime;

                c.a =
                    1f - (t / 0.4f);

                sr.color = c;

                yield return null;
            }
        }

        Destroy(gameObject);
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (!moveHorizontally)
            return;

        Gizmos.color = Color.yellow;

        Vector3 start =
            Application.isPlaying
                ? patrolStart
                : transform.position;

        Gizmos.DrawLine(
            start + Vector3.left * moveDistance,
            start + Vector3.right * moveDistance
        );
    }
}
