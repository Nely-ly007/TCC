using UnityEngine;

/// <summary>
/// O "marcador" no cenário: um Collider2D marcado como "Is Trigger",
/// posicionado num ponto da fase (ex: entrada da zona de combate,
/// início da arena do boss). Quando o player passa por ele, pede a
/// troca de seção musical.
///
/// COMO USAR:
/// 1. Crie um GameObject vazio no ponto do cenário onde quer a troca.
/// 2. Adicione um Collider2D (BoxCollider2D funciona bem) e marque "Is Trigger".
/// 3. Adicione este script ao mesmo GameObject.
/// 4. No Inspector, escolha qual seção deve tocar (ex: Combate ou Boss).
/// 5. Confira se o seu player tem a tag "Player" configurada.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MusicSectionTrigger : MonoBehaviour
{
    [Tooltip("Qual seção deve começar a tocar quando o player passar por aqui.")]
    public MusicSection sectionToPlay;

    [Tooltip("Se marcado, o marcador só funciona uma vez (bom para Combate/Boss).")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && hasTriggered) return;

        if (MusicSectionManager.Instance != null)
        {
            MusicSectionManager.Instance.RequestSection(sectionToPlay);
        }
        else
        {
            Debug.LogWarning("MusicSectionTrigger: nenhum MusicSectionManager encontrado na cena.");
        }

        hasTriggered = true;
    }
}