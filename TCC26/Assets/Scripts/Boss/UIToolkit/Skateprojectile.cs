using UnityEngine;

/// <summary>
/// POP ADVENTURE - SkateProjectile
/// Projétil do ataque da Donna: patim que desliza pelo chão.
///
/// Setup do Prefab:
/// 1. GameObject "SkateProjectile"
/// 2. SpriteRenderer → sprite do patim
/// 3. BoxCollider2D → Is Trigger: TRUE → ajusta ao sprite
/// 4. Rigidbody2D → Gravity Scale: 0, Collision Detection: Continuous
/// 5. Este script
/// </summary>
public class SkateProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f; // tempo até sumir se não atingir nada

    private Vector2 direction;
    private float   speed;
    private int     damage;
    private bool    initialized = false;

    /// <summary>
    /// Chamado por BossDonna.SpawnSkateEvent() logo após Instantiate.
    /// </summary>
    public void Init(Vector2 dir, float spd, int dmg)
    {
        direction   = dir.normalized;
        speed       = spd;
        damage      = dmg;
        initialized = true;

        // Vira o sprite na direção do movimento
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!initialized) return;
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
        // Destrói ao bater em chão/parede (Layer "Ground")
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}