using UnityEngine;

/// <summary>
/// POP ADVENTURE - MusicProjectile
/// Nota musical disparada pelo Enzo.
/// O prefab PRECISA ter Rigidbody2D com Gravity Scale = 0.
/// </summary>
public class MusicProjectile : MonoBehaviour
{
    [SerializeField] private float speed    = 10f;
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private ParticleSystem hitEffect;

    private Vector2      direction;
    private int          damage;
    private Rigidbody2D  rb;
    private bool         initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Se não tiver Rigidbody2D no prefab, cria um automaticamente
        if (rb == null)
        {
            rb               = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale  = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Debug.LogWarning("[MusicProjectile] Rigidbody2D não encontrado no prefab — " +
                             "foi criado automaticamente. Adicione um manualmente no prefab " +
                             "com Gravity Scale = 0 para evitar este aviso.");
        }
    }

    public void Init(Vector2 dir, int dmg)
    {
        direction   = dir.normalized;
        damage      = dmg;
        initialized = true;

        rb.gravityScale  = 0f;
        rb.linearVelocity = direction * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!initialized) return;

        // Ignora colisão com o próprio player
        if (other.CompareTag("Player")) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            SpawnHitEffect();
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            ParticleSystem fx = Instantiate(
                hitEffect, transform.position, Quaternion.identity);
            Destroy(fx.gameObject, 1f);
        }
    }
}

public interface IDamageable
{
    void TakeDamage(int amount);
}