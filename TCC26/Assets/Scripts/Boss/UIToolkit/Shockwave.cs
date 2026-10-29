using UnityEngine;

/// <summary>
/// POP ADVENTURE - Shockwave
/// Onda de choque rasteira da Donna: expande para um lado e some.
///
/// Setup do Prefab:
/// 1. GameObject "Shockwave"
/// 2. SpriteRenderer → sprite de onda/energia (largo e baixo)
/// 3. BoxCollider2D → Is Trigger: TRUE → ajusta fino na altura (rasteiro)
/// 4. Rigidbody2D → Gravity Scale: 0, Collision Detection: Continuous
/// 5. Este script
/// </summary>
public class Shockwave : MonoBehaviour
{
    [SerializeField] private float speed    = 5f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float scaleGrowSpeed = 1.5f; // cresce levemente enquanto avança

    private Vector2 direction;
    private int     damage;
    private bool    initialized = false;
    private bool    hit         = false;

    private Vector3 baseScale;

    /// <summary>
    /// Chamado por BossDonna.SpawnWaveEvent() logo após Instantiate.
    /// </summary>
    public void Init(Vector2 dir, int dmg)
    {
        direction   = dir.normalized;
        damage      = dmg;
        initialized = true;
        baseScale   = transform.localScale;

        // Vira o sprite conforme direção
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x),
                                               baseScale.y,
                                               baseScale.z);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!initialized) return;

        // Move na direção
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Cresce levemente (efeito de expansão de onda)
        float sign = Mathf.Sign(transform.localScale.x);
        float newX = Mathf.Abs(transform.localScale.x) + scaleGrowSpeed * Time.deltaTime;
        transform.localScale = new Vector3(sign * newX,
                                           transform.localScale.y,
                                           transform.localScale.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hit) return; // acerta apenas uma vez

        if (other.CompareTag("Player"))
        {
            hit = true;
            other.GetComponent<PlayerController>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}