using UnityEngine;

/// <summary>
/// POP ADVENTURE - EnemySpeakerSpike
/// Mob da Fase 1: caixa de som com espinhos.
/// Pulsa no beat. Dano ao contato sempre (espinhos sempre ativos).
/// </summary>
public class EnemySpeakerSpike : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 30;
    [SerializeField] private int contactDamage = 15;

    [Header("Movimento")]
    [SerializeField] private bool moveHorizontally = false;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float moveRange = 3f;

    [Header("Beat Pulse")]
    [SerializeField] private float pulseScaleAmount = 0.15f;
    [SerializeField] private float pulseReturnSpeed = 8f;

    [Header("Drop")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private float dropChance = 0.5f;

    private int currentHP;
    private Vector3 baseScale;
    private Vector3 startPosition;
    private float moveDirection = 1f;
    private bool pulsing = false;

    void Awake()
    {
        currentHP   = maxHP;
        baseScale   = transform.localScale;
        startPosition = transform.position;
    }

    void OnEnable()  => RhythmManager.OnBeatNumberStatic += OnBeat;
    void OnDisable() => RhythmManager.OnBeatNumberStatic -= OnBeat;

    void Update()
    {
        // Movimento horizontal patrol
        if (moveHorizontally)
        {
            transform.position += Vector3.right * moveDirection * moveSpeed * Time.deltaTime;

            float dist = transform.position.x - startPosition.x;
            if (Mathf.Abs(dist) >= moveRange)
            {
                moveDirection *= -1f;
                transform.localScale = new Vector3(
                    -Mathf.Sign(moveDirection) * Mathf.Abs(baseScale.x),
                    baseScale.y, baseScale.z);
            }
        }

        // Retorna escala ao normal suavemente
        if (pulsing)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                baseScale,
                pulseReturnSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.localScale, baseScale) < 0.01f)
            {
                transform.localScale = baseScale;
                pulsing = false;
            }
        }
    }

    private void OnBeat(int beatNumber)
    {
        // Beat 1: estica Y (espinhos saem)
        // Beat 3: estica X (recolhe)
        if (beatNumber == 1)
        {
            transform.localScale = new Vector3(
                baseScale.x * (1f - pulseScaleAmount),
                baseScale.y * (1f + pulseScaleAmount),
                baseScale.z);
        }
        else if (beatNumber == 3)
        {
            transform.localScale = new Vector3(
                baseScale.x * (1f + pulseScaleAmount),
                baseScale.y * (1f - pulseScaleAmount),
                baseScale.z);
        }
        pulsing = true;
    }

    // Dano ao contato — sempre ativo (sem verificação de spikesExtended)
    void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
            col.gameObject.GetComponent<PlayerController>()?.TakeDamage(contactDamage);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
            col.gameObject.GetComponent<PlayerController>()?.TakeDamage(contactDamage);
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP <= 0) Die();
    }

    private void Die()
    {
        if (dropPrefab != null && Random.value <= dropChance)
            Instantiate(dropPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}