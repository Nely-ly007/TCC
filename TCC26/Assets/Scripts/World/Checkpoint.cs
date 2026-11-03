using UnityEngine;

namespace PopAdventure.World
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Sprites")]
        [Tooltip("Sprite quando o Checkpoint ainda NÃO foi ativado (Preto/Desativado)")]
        [SerializeField] private Sprite spriteInactive;
        [Tooltip("Sprite quando o Checkpoint FOI ativado (Original/Colorido)")]
        [SerializeField] private Sprite spriteActive;

        [Header("Efeitos")]
        [SerializeField] private ParticleSystem activateEffect;
        [SerializeField] private AudioClip activateSFX;

        private bool isActivated = false;
        private SpriteRenderer sr;
        private AudioSource audioSource;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();

            // Configura o estado inicial (Desativado / Preto)
            if (sr != null)
            {
                if (spriteInactive != null)
                {
                    sr.sprite = spriteInactive;
                }
                else
                {
                    // Caso não tenha colocado um sprite desativado, escurece o sprite atual (ficando preto)
                    sr.color = Color.black; 
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated) return;

            if (other.CompareTag("Player"))
            {
                isActivated = true;

                // Troca para o sprite ativado / devolve a cor normal
                if (sr != null)
                {
                    if (spriteActive != null) sr.sprite = spriteActive;
                    sr.color = Color.white; // Restaura a cor original
                }

                if (activateEffect != null)
                    activateEffect.Play();

                if (activateSFX != null && audioSource != null)
                    audioSource.PlayOneShot(activateSFX);

                // Salva o progresso
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SaveGame();
                    Debug.Log($"[Checkpoint] Progresso salvo pelo GameManager na posição: {transform.position}");
                }
                else
                {
                    PlayerPrefs.SetFloat("Checkpoint_X", transform.position.x);
                    PlayerPrefs.SetFloat("Checkpoint_Y", transform.position.y);
                    PlayerPrefs.Save();
                }
            }
        }
    }
}