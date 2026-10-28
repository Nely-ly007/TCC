using UnityEngine;
using System.Collections;

/// <summary>
/// POP ADVENTURE - EnemySpeakerSpike v3
/// - Dano via Trigger OU Collision (cobre os dois casos)
/// - Flash vermelho via material.color (compatível com URP)
/// - Pulsa escala no beat
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

    [Header("Damage Flash")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Drop")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private float dropChance = 0.5f;

    private int currentHP;
    private Vector3 baseScale;
    private Vector3 startPosition;
    private float moveDirection = 1f;
    private bool pulsing = false;
    private bool flashing = false;

    // Guarda referências dos renderers e suas cores/materiais originais
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    // Cooldown para não aplicar dano todo frame no Stay
    private float damageCooldown = 0.5f;
    private float lastDamageTime = -999f;

    void Awake()
    {
        currentHP     = maxHP;
        baseScale     = transform.localScale;
        startPosition = transform.position;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors  = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    void OnEnable()  => RhythmManager.OnBeatNumberStatic += OnBeat;
    void OnDisable() => RhythmManager.OnBeatNumberStatic -= OnBeat;

    void Update()
    {
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

        if (pulsing)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale, baseScale,
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
        if (beatNumber == 1)
            transform.localScale = new Vector3(
                baseScale.x * (1f - pulseScaleAmount),
                baseScale.y * (1f + pulseScaleAmount), baseScale.z);
        else if (beatNumber == 3)
            transform.localScale = new Vector3(
                baseScale.x * (1f + pulseScaleAmount),
                baseScale.y * (1f - pulseScaleAmount), baseScale.z);
        pulsing = true;
    }

    // ── DANO: Trigger (Is Trigger = ON) ──────────────────────────
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player")) ApplyDamageToPlayer(col.gameObject);
    }

    void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Player") && Time.time >= lastDamageTime + damageCooldown)
            ApplyDamageToPlayer(col.gameObject);
    }

    // ── DANO: Collision (Is Trigger = OFF) ───────────────────────
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player")) ApplyDamageToPlayer(col.gameObject);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player") && Time.time >= lastDamageTime + damageCooldown)
            ApplyDamageToPlayer(col.gameObject);
    }

    private void ApplyDamageToPlayer(GameObject player)
    {
        lastDamageTime = Time.time;
        player.GetComponent<PlayerController>()?.TakeDamage(contactDamage);
    }

    // ── RECEBER DANO ─────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (!flashing) StartCoroutine(DamageFlash());
        if (currentHP <= 0) Die();
    }

    private IEnumerator DamageFlash()
    {
        flashing = true;

        // Aplica vermelho
        foreach (var sr in spriteRenderers)
            if (sr != null) sr.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        // Restaura cor original
        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i] != null) spriteRenderers[i].color = originalColors[i];

        flashing = false;
    }

    private void Die()
    {
        if (dropPrefab != null && Random.value <= dropChance)
            Instantiate(dropPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}