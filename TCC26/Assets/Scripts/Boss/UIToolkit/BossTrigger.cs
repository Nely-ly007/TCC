using UnityEngine;

/// <summary>
/// Coloque este script no GameObject que fica na entrada da arena do Boss.
/// Requer BoxCollider2D com Is Trigger MARCADO.
/// </summary>
public class BossTrigger : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private BossDonna boss;

    private bool hasTriggered = false;

    private void OnEnable()
    {
        hasTriggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (boss != null)
            {
                boss.ActivateBoss();
                Debug.Log("[BossTrigger] JOGADOR ENTROU NA ARENA! Boss ativado com sucesso.");
            }
            else
            {
                Debug.LogError("[BossTrigger] ERRO: Arraste o GameObject do BossDonna para a variável 'Boss' no Inspector deste Trigger!");
            }
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    // Desenha uma caixa visível na aba Scene para identificar a posição da arena
    private void OnDrawGizmos()
    {
        Gizmos.color = hasTriggered ? Color.green : Color.red;
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        }
    }
}