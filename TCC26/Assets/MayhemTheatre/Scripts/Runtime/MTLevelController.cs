using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Raiz da Fase 4: spawn, checkpoint, respawn, queda no vazio e eventos de fim de fase.</summary>
    public class MTLevelController : MonoBehaviour
    {
        public static MTLevelController Instance { get; private set; }

        [Header("Spawn")]
        public Transform playerSpawn;
        public bool teleportPlayerToSpawn = true;
        public MTMoment startMoment = MTMoment.Fase1;

        [Header("Morte / respawn")]
        public float respawnDelay = 1.5f;
        [Tooltip("Abaixo desta altura o Player morre (queda no vazio).")]
        public float killY = -14f;

        [Header("Referências (o builder preenche)")]
        public MTBossController boss;
        public MTBossArena arena;

        [Header("Eventos (ligue ao seu GameManager / SceneController)")]
        public UnityEvent onLevelStart = new UnityEvent();
        public UnityEvent onPlayerRespawned = new UnityEvent();
        public UnityEvent onBossDefeated = new UnityEvent();
        [Tooltip("Disparado quando o fragmento do Disco Dourado é coletado. Ligue ao seu GameManager (fragmento) e SceneController (voltar ao Hub).")]
        public UnityEvent onLevelComplete = new UnityEvent();

        Vector3 checkpointPos;
        MTMoment checkpointMoment;
        MTPlayerHook player;
        bool respawning;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            player = MTPlayerHook.Instance != null ? MTPlayerHook.Instance : MTUtil.FindFirst<MTPlayerHook>();
            Vector3 spawn = playerSpawn != null ? playerSpawn.position : transform.position;

            if (player == null)
            {
                Debug.LogWarning("[MT] Nenhum MTPlayerHook na cena. Adicione o componente MT Player Hook ao Player.");
            }
            else
            {
                if (teleportPlayerToSpawn && playerSpawn != null) player.transform.position = spawn;
                player.onDied.AddListener(OnPlayerDied);
            }

            checkpointPos = spawn;
            checkpointMoment = startMoment;

            if (MTMusicDirector.Instance != null) MTMusicDirector.Instance.PlayMoment(startMoment, true);
            onLevelStart.Invoke();
        }

        void Update()
        {
            if (player != null && !player.IsDead && player.transform.position.y < killY) player.Kill();
        }

        public void SetCheckpoint(Vector3 position, MTMoment moment)
        {
            checkpointPos = position;
            checkpointMoment = moment;
        }

        void OnPlayerDied()
        {
            if (!respawning) StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            respawning = true;
            yield return new WaitForSeconds(respawnDelay);

            if (arena != null) arena.ResetArena();
            if (player != null) player.Respawn(checkpointPos);
            if (MTMusicDirector.Instance != null) MTMusicDirector.Instance.PlayMoment(checkpointMoment, true);
            onPlayerRespawned.Invoke();
            respawning = false;
        }

        public void BossDefeated()
        {
            onBossDefeated.Invoke();
        }

        public void CompleteLevel()
        {
            onLevelComplete.Invoke();
        }
    }
}
