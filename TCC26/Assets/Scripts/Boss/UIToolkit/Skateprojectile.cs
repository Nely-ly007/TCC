using UnityEngine;

/// <summary>
/// POP ADVENTURE - SkateProjectile
/// Projétil do ataque da Donna: patim que desliza pelo chão.
/// </summary>
public class SkateProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;

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
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z);
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!initialized) return;
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignora colisões com o próprio Boss ou com outros projéteis
        if (other.CompareTag("Boss") || other.GetComponent<SkateProjectile>() != null) return;

        // Procura o PlayerController no objeto que colidiu ou nos parentes/filhos dele
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerController>();
        }

        if (player != null || other.CompareTag("Player"))
        {
            Debug.Log($"[SkateProjectile] ACERTOU O PLAYER! Aplicando {damage} de dano.");

            if (player != null)
            {
                player.TakeDamage(damage);
            }
            else
            {
                // Fallback de segurança se o script no Player tiver outro nome de método
                other.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }

            Destroy(gameObject);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}