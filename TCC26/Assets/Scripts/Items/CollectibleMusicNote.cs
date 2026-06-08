using UnityEngine;
using System.Collections;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}/// <summary>
/// POP ADVENTURE - CollectibleMusicNote
/// Nota musical: restaura +15 HP ao jogador.
/// </summary>
public class CollectibleMusicNote : MonoBehaviour
{
    [SerializeField] private int healAmount = 15; // GDD: +15 HP
    [SerializeField] private AudioClip collectSFX;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController.Instance?.Heal(healAmount);
        if (collectSFX != null)
            AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        // Efeito de partícula poderia ser adicionado aqui
        Destroy(gameObject);
    }

    void Update()
    {
        // Flutua para cima suavemente
        transform.position += Vector3.up * 0.5f * Time.deltaTime;
    }
}

