using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Gerente da Fase 3: checkpoints, respawn, vinis coletados, fragmento do Disco Dourado.
    /// Ligue os eventos ao seu GameManager/HUD pelo Inspector (nada aqui depende dos seus scripts).
    /// </summary>
    public class GGLevelManager : MonoBehaviour
    {
        public static GGLevelManager Instance { get; private set; }

        [SerializeField] Transform initialSpawn;
        [SerializeField] float respawnDelay = 1.5f;
        [Tooltip("Opcional: cena carregada ao concluir a fase. Deixe vazio se você usa o seu SceneController nos eventos.")]
        [SerializeField] string nextSceneName = "";
        [SerializeField] float completeDelay = 3f;

        [Header("Eventos")]
        [Tooltip("Total de vinis coletados nesta fase.")]
        public GGIntEvent onVinylsChanged;
        [Tooltip("Quantidade que acabou de ser coletada (repasse ao GameManager).")]
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

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        }

        void Start()
        {
            GGPickup[] pickups = FindObjectsByType<GGPickup>(FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
                if (pickups[i].PickupKind == GGPickup.Kind.Vinyl) TotalVinylsInLevel++;
            GGBossArena[] arenas = FindObjectsByType<GGBossArena>(FindObjectsSortMode.None);
            if (arenas.Length > 0) arena = arenas[0];
        }

        void Update()
        {
            if (playerHealth == null)
            {
                Transform p = GGUtil.Player;
                if (p == null) return;
                playerHealth = p.GetComponent<GGPlayerHealth>();
                if (playerHealth != null) playerHealth.Died += OnPlayerDied;
            }
        }

        public void SetCheckpoint(GGCheckpoint cp)
        {
            if (currentCheckpoint != null && currentCheckpoint != cp) { /* o anterior continua visualmente ativo */ }
            currentCheckpoint = cp;
        }

        public void AddVinyls(int amount)
        {
            Vinyls += amount;
            if (onVinylCollected != null) onVinylCollected.Invoke(amount);
            if (onVinylsChanged != null) onVinylsChanged.Invoke(Vinyls);
        }

        public void CollectFragment()
        {
            if (FragmentCollected) return;
            FragmentCollected = true;
            if (onFragmentCollected != null) onFragmentCollected.Invoke();
            StartCoroutine(CompleteRoutine());
        }

        IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(completeDelay);
            if (onLevelComplete != null) onLevelComplete.Invoke();
            if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
        }

        Vector3 SpawnPosition()
        {
            if (currentCheckpoint != null) return currentCheckpoint.RespawnPosition;
            if (initialSpawn != null) return initialSpawn.position;
            return Vector3.zero;
        }

        /// <summary>Leva o Player ao checkpoint sem restaurar a vida (usado ao cair no abismo).</summary>
        public void TeleportPlayerToCheckpoint()
        {
            Transform p = GGUtil.Player;
            if (p == null) return;
            Vector3 pos = SpawnPosition();
            p.position = new Vector3(pos.x, pos.y, p.position.z);
            Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.SetVelocity(Vector2.zero); rb.position = new Vector2(pos.x, pos.y); }
        }

        void OnPlayerDied()
        {
            if (respawning) return;
            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            respawning = true;
            yield return new WaitForSeconds(respawnDelay);
            TeleportPlayerToCheckpoint();
            if (playerHealth != null) playerHealth.RestoreFull();
            if (arena != null) arena.ResetFight();
            if (onPlayerRespawned != null) onPlayerRespawned.Invoke();
            respawning = false;
        }
    }
}
