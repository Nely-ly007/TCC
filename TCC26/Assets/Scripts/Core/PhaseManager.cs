using UnityEngine;

/// <summary>
/// POP ADVENTURE - PhaseManager
/// Inicializa cada cena de fase:
/// - Inicia a música com o BPM correto
/// - Define o índice da fase para o fragmento
/// - Faz fade in ao entrar
/// Adicione em um GameObject vazio "PhaseManager" em cada cena de fase.
/// </summary>
public class PhaseManager : MonoBehaviour
{
    [Header("Fase")]
    [SerializeField] private int phaseIndex = 0;       // 0=Fase1, 1=Fase2, 2=Fase3, 3=Fase4

    [Header("Música")]
    [SerializeField] private AudioClip phaseMusic;
    [SerializeField] private float phaseBPM = 120f;

    [Header("Spawn do Player")]
    [SerializeField] private Transform spawnPoint;     // arraste o ponto de spawn

    void Start()
    {
        // Fade in ao entrar na fase
        SceneController.Instance?.FadeIn(0.5f);

        // Inicia a música sincronizada com o RhythmManager
        if (phaseMusic != null)
            RhythmManager.Instance?.StartMusic(phaseMusic, phaseBPM);
        else
            Debug.LogWarning($"[PhaseManager] Fase {phaseIndex + 1}: nenhuma música atribuída!");

        // Posiciona o player no spawn
        if (spawnPoint != null && PlayerController.Instance != null)
            PlayerController.Instance.transform.position = spawnPoint.position;
    }
}