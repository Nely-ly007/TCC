
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

            // Configura o estado inicial
            if (sr != null)
            {
                if (spriteInactive != null)
                {
                    sr.sprite = spriteInactive;
                }
                else
                {
                    sr.color = Color.black;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated) return;

            if (!other.CompareTag("Player")) return;

            // Verifica se existe um Player válido
            PlayerController player = other.GetComponent<PlayerController>();

            if (player == null)
                player = other.GetComponentInParent<PlayerController>();

            if (player == null)
            {
                Debug.LogWarning(
                    "[Checkpoint] PlayerController não encontrado!"
                );
                return;
            }

            isActivated = true;

            // ATUALIZA A POSIÇÃO DE RESPAWN
            player.SetCheckpoint(transform.position);

            // Ativa visualmente o checkpoint
            if (sr != null)
            {
                if (spriteActive != null)
                    sr.sprite = spriteActive;

                sr.color = Color.white;
            }

            // Efeito visual
            if (activateEffect != null)
                activateEffect.Play();

            // Som
            if (activateSFX != null && audioSource != null)
                audioSource.PlayOneShot(activateSFX);

            // Salva o progresso
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame();

                Debug.Log(
                    $"[Checkpoint] Ativado! Respawn definido em: {transform.position}"
                );
            }
            else
            {
                PlayerPrefs.SetFloat(
                    "Checkpoint_X",
                    transform.position.x
                );

                PlayerPrefs.SetFloat(
                    "Checkpoint_Y",
                    transform.position.y
                );

                PlayerPrefs.Save();

                Debug.Log(
                    $"[Checkpoint] Posição salva: {transform.position}"
                );
            }
        }
    }
}
