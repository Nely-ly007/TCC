using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Gerente da Fase 3: checkpoints, respawn, vinis coletados,
    /// fragmento do Disco Dourado e conclusão da fase.
    /// </summary>
    public class GGLevelManager : MonoBehaviour
    {
        public static GGLevelManager Instance { get; private set; }

        [SerializeField] Transform initialSpawn;
        [SerializeField] float respawnDelay = 1.5f;

        [Header("Conclusão da Fase")]
        [Tooltip("Cena carregada automaticamente ao concluir a fase.")]
        [SerializeField] string nextSceneName = "Ending";

        [SerializeField] float completeDelay = 3f;

        [Header("Eventos")]
        [Tooltip("Total de vinis coletados nesta fase.")]
        public GGIntEvent onVinylsChanged;

        [Tooltip("Quantidade de vinis recém-coletados.")]
        public GGIntEvent onVinylCollected;

        public UnityEvent onFragmentCollected;
        public UnityEvent onPlayerRespawned;
        public UnityEvent onLevelComplete;

        public int Vinyls { get; private set; }
        public int TotalVinylsInLevel { get; private set; }
        public bool FragmentCollected { get; private set; }

        GGCheckpoint currentCheckpoint;
        GGPlayerHealth playerHealth;
        GGBossArena arena;

        bool respawning;
        bool levelCompleted;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (playerHealth != null)
                playerHealth.Died -= OnPlayerDied;
        }

        void Start()
        {
            GGPickup[] pickups = FindObjectsByType<GGPickup>(FindObjectsSortMode.None);

            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i].PickupKind == GGPickup.Kind.Vinyl)
                    TotalVinylsInLevel++;
            }

            GGBossArena[] arenas = FindObjectsByType<GGBossArena>(FindObjectsSortMode.None);

            if (arenas.Length > 0)
                arena = arenas[0];
        }

        void Update()
        {
            if (playerHealth == null)
            {
                Transform p = GGUtil.Player;

                if (p == null)
                    return;

                playerHealth = p.GetComponent<GGPlayerHealth>();

                if (playerHealth != null)
                    playerHealth.Died += OnPlayerDied;
            }
        }

        public void SetCheckpoint(GGCheckpoint cp)
        {
            if (currentCheckpoint != null && currentCheckpoint != cp)
            {
                // O checkpoint anterior continua visualmente ativo.
            }

            currentCheckpoint = cp;
        }

        public void AddVinyls(int amount)
        {
            Vinyls += amount;

            if (onVinylCollected != null)
                onVinylCollected.Invoke(amount);

            if (onVinylsChanged != null)
                onVinylsChanged.Invoke(Vinyls);
        }

        public void CollectFragment()
        {
            if (FragmentCollected || levelCompleted)
                return;

            FragmentCollected = true;

            if (onFragmentCollected != null)
                onFragmentCollected.Invoke();

            StartCoroutine(CompleteRoutine());
        }

        IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(completeDelay);

            if (levelCompleted)
                yield break;

            levelCompleted = true;

            if (onLevelComplete != null)
                onLevelComplete.Invoke();

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("[GGLevelManager] Nome da próxima cena não configurado!");
            }
        }

        Vector3 SpawnPosition()
        {
            if (currentCheckpoint != null)
                return currentCheckpoint.RespawnPosition;

            if (initialSpawn != null)
                return initialSpawn.position;

            return Vector3.zero;
        }

        /// <summary>
        /// Leva o Player ao checkpoint sem restaurar a vida.
        /// </summary>
        public void TeleportPlayerToCheckpoint()
        {
            Transform p = GGUtil.Player;

            if (p == null)
                return;

            Vector3 pos = SpawnPosition();

            p.position = new Vector3(pos.x, pos.y, p.position.z);

            Rigidbody2D rb = p.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.SetVelocity(Vector2.zero);
                rb.position = new Vector2(pos.x, pos.y);
            }
        }

        void OnPlayerDied()
        {
            if (respawning || levelCompleted || FragmentCollected)
                return;

            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            respawning = true;

            yield return new WaitForSeconds(respawnDelay);

            if (levelCompleted || FragmentCollected)
            {
                respawning = false;
                yield break;
            }

            TeleportPlayerToCheckpoint();

            if (playerHealth != null)
                playerHealth.RestoreFull();

            if (arena != null)
                arena.ResetFight();

            if (onPlayerRespawned != null)
                onPlayerRespawned.Invoke();

            respawning = false;
        }
    }
}