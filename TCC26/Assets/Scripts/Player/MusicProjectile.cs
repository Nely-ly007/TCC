using UnityEngine;

/// <summary>
/// POP ADVENTURE - MusicProjectile
/// A nota musical disparada pelo jogador.
/// </summary>
public class MusicProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem hitEffect;

    private Vector2 direction;
    private int damage;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 dir, int dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        rb.linearVelocity = direction * speed;

        // Gira o sprite na direção correta
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            SpawnHitEffect();
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            ParticleSystem fx = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(fx.gameObject, 1f);
        }
    }
}

/// <summary>
/// Interface implementada por tudo que pode receber dano.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
}
