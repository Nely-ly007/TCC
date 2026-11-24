
using UnityEngine;

namespace PopAdventure.World
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Sprites")]
        [Tooltip("Sprite quando o Checkpoint ainda NÃO foi ativado.")]
        [SerializeField] private Sprite spriteInactive;

        [Tooltip("Sprite quando o Checkpoint FOI ativado.")]
        [SerializeField] private Sprite spriteActive;

        [Header("Efeitos")]
        [SerializeField] private ParticleSystem activateEffect;
        [SerializeField] private AudioClip activateSFX;

        [Header("Cores")]
        [SerializeField] private Color burstColor = new Color(1.00f, 0.22f, 0.60f);
        [SerializeField] private Color ringColor = new Color(1.00f, 0.85f, 0.20f);

        private bool isActivated = false;

        private SpriteRenderer sr;
        private AudioSource audioSource;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();

            if (sr != null)
            {
                if (spriteInactive != null)
                    sr.sprite = spriteInactive;
                else
                    sr.color = Color.black;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated) return;
            if (!other.CompareTag("Player")) return;

            PlayerController player = other.GetComponent<PlayerController>();

            if (player == null)
                player = other.GetComponentInParent<PlayerController>();

            if (player == null)
            {
                Debug.LogWarning("[Checkpoint] PlayerController não encontrado!");
                return;
            }

            isActivated = true;

            // Respawn
            player.SetCheckpoint(transform.position);

            // Visual
            if (sr != null)
            {
                if (spriteActive != null)
                    sr.sprite = spriteActive;

                sr.color = Color.white;
            }

            // Partículas
            if (activateEffect != null)
                activateEffect.Play();

            // Som
            if (activateSFX != null && audioSource != null)
                audioSource.PlayOneShot(activateSFX);

            // Salvamento
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame();

                Debug.Log($"[Checkpoint] Ativado! Respawn definido em: {transform.position}");
            }
            else
            {
                PlayerPrefs.SetFloat("Checkpoint_X", transform.position.x);
                PlayerPrefs.SetFloat("Checkpoint_Y", transform.position.y);
                PlayerPrefs.Save();

                Debug.Log($"[Checkpoint] Posição salva: {transform.position}");
            }
        }
    }
}
