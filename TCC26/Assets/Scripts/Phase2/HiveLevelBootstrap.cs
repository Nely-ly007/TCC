using System.Collections;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Cérebro da Fase 2. Fica em [Systems]/HiveLevelBootstrap.
    /// Em Play: lê todos os HiveMarker da cena e CRIA por código plataformas, hazards, pickups, inimigos,
    /// checkpoints, arena do boss e saída; cria o cenário de fundo e o overlay de UI; cuida de
    /// checkpoint/queda/morte e da tela de vitória. Tudo que é criado vai para [Dynamic]/...
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class HiveLevelBootstrap : MonoBehaviour
    {
        public static HiveLevelBootstrap Instance { get; private set; }

        /// <summary>True enquanto a luta da Rainha está acontecendo (inimigos comuns não dropam cura).</summary>
        public static bool BossActive;

        [Header("Fase")]
        public string phaseTitle = "THE HIVE";
        public string phaseSubtitle = "Fase 2 · 108 BPM";
        public int phaseNumber = 2;
        [Tooltip("Cena carregada ao concluir a fase (nome exato nas Build Settings).")]
        public string nextSceneName = "Hub";

        [Header("Sistemas")]
        public bool spawnBackdrop = true;
        public bool spawnOverlay = true;
        [Tooltip("Cria o HiveMusic sozinho se existirem os .wav em Assets/Resources/Hive. Desmarque para usar outro sistema de música.")]
        public bool autoMusic = true;
        [Tooltip("Detecta HP = 0 no Player (reflexão) e volta ao último checkpoint com vida cheia.")]
        public bool handlePlayerDeath = true;
        public float deathDelay = 1.0f;

        [Header("Compatibilidade com o seu Player (pulo)")]
        [Tooltip("Layer aplicada ao chão/plataformas da fase (Tilemaps Ground e Platforms, blocos de teste e plataformas criadas). " +
                 "Deve ser a MESMA layer que o Ground Check do seu PlayerController aceita. Vazio = não mexe. A layer precisa existir em Edit ▸ Project Settings ▸ Tags and Layers.")]
        public string groundLayerName = "Ground";
        [Tooltip("Tag aplicada ao chão/plataformas, se o seu Player checa o chão por tag (ex.: Ground). Vazio = não mexe. A tag precisa existir em Tags and Layers.")]
        public string groundTag = "";

        [Header("Boss (visual próprio)")]
        [Tooltip("Arraste aqui o sprite do seu boss (um frame do sprite recortado). Vazio = usa a Rainha desenhada por código. Troca só o visual; ataques, vida e fases continuam iguais.")]
        public Sprite bossSprite;
        [Tooltip("Tamanho do sprite do boss. Ajuste até ele ficar com ~3 unidades de altura.")]
        public float bossSpriteScale = 1f;

        [Header("Balanceamento")]
        public int pitDamage = 15;
        [Tooltip("Distância (unidades) em que o vinil começa a ser puxado até o Player. 0 = sem ímã (só coleta encostando).")]
        public float vinylMagnet = 0.9f;
        [Tooltip("Distância em que a nota de cura é puxada. 0 = sem ímã.")]
        public float noteMagnet = 0.7f;

        private Transform dynamicRoot;
        private Transform gPlatforms, gHazards, gPickups, gEnemies, gCheckpoints, gBoss, gMisc;

        private Vector2 spawnPoint;
        private bool hasSpawn;
        private Vector2 respawnPoint;
        private HiveCheckpoint currentCheckpoint;
        private HiveQueenArena arena;

        private HiveMusicSection checkpointMusic = HiveMusicSection.Loop;   // seção a retomar ao voltar ao checkpoint
        private int vinylsCollected, vinylsTotal, bonusVinyls, deaths;
        private float levelTime;
        private bool levelDone, bossRequired, bossDefeated, dying;
        private float nextPoll;

        public bool CanExit { get { return !bossRequired || bossDefeated; } }
        public bool BossDefeated { get { return bossDefeated; } }
        public Transform DynamicRoot { get { return dynamicRoot; } }

        // ============================================================ Ciclo de vida

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            BossActive = false;

            HivePickup.VinylMagnet = Mathf.Max(0f, vinylMagnet);
            HivePickup.NoteMagnet = Mathf.Max(0f, noteMagnet);

            // música em 4 seções: se a cena não tem HiveMusic mas existem os .wav em Resources/Hive, cria sozinho
            if (autoMusic) HiveMusic.EnsureExists();

            if (HiveBeatDriver.Instance == null)
            {
                var go = new GameObject("HiveBeatDriver");
                go.AddComponent<HiveBeatDriver>();
                bool hasMusic = HiveMusic.Instance != null && HiveMusic.Instance.HasClips;
                Debug.LogWarning("[Hive] Não achei HiveBeatDriver na cena: criei o relógio de 108 BPM" +
                                 (hasMusic ? "." : " SEM música. Coloque os .wav em Assets/Resources/Hive/ " +
                                                   "ou arraste um .wav em [Systems] ▸ HiveBeatDriver ▸ Music Clip."));
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                BossActive = false;
            }
        }

        private void Start()
        {
            if (Instance != this) return;

            PrepareRoots();

            var player = HiveBridge.Player;
            if (player == null)
                Debug.LogWarning("[Hive] Player não encontrado. Coloque o prefab do Player na cena com a tag 'Player'.");
            else
                HivePlayerStatus.For(player);

            BuildFromMarkers();
            ApplyGroundToTilemaps();

            if (spawnBackdrop) HiveBackdrop.Create();
            if (spawnOverlay) HiveOverlay.Create();

            if (player != null)
            {
                if (!hasSpawn) spawnPoint = player.transform.position;
                respawnPoint = spawnPoint;
                if (hasSpawn) TeleportPlayer(spawnPoint);
            }

            HiveOverlay.SetVinyls(0, vinylsTotal);
            HiveOverlay.Banner(phaseTitle, phaseSubtitle, 3f);
        }

        private void Update()
        {
            if (levelDone) return;
            levelTime += Time.deltaTime;

            if (handlePlayerDeath && !dying && Time.timeSinceLevelLoad > 1f && Time.time >= nextPoll)
            {
                nextPoll = Time.time + 0.1f;
                float hp, max;
                if (HiveBridge.TryGetPlayerHealth(out hp, out max) && max > 0f && hp <= 0f)
                    StartCoroutine(DeathRoutine());
            }
        }

        // ============================================================ Hierarquia dinâmica

        private Transform FindOrCreate(string name, Transform parent)
        {
            Transform t = null;
            if (parent != null) t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                t = go.transform;
                t.SetParent(parent, false);
            }
            return t;
        }

        private void PrepareRoots()
        {
            var existing = GameObject.Find("[Dynamic]");
            if (existing != null) dynamicRoot = existing.transform;
            else dynamicRoot = new GameObject("[Dynamic]").transform;
            dynamicRoot.position = Vector3.zero;

            gPlatforms = FindOrCreate("Platforms", dynamicRoot);
            gHazards = FindOrCreate("Hazards", dynamicRoot);
            gPickups = FindOrCreate("Pickups", dynamicRoot);
            gEnemies = FindOrCreate("Enemies", dynamicRoot);
            gCheckpoints = FindOrCreate("Checkpoints", dynamicRoot);
            gBoss = FindOrCreate("Boss", dynamicRoot);
            gMisc = FindOrCreate("Misc", dynamicRoot);
        }

        private GameObject NewObj(string name, Transform parent, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            return go;
        }

        // ============================================================ Layer/tag do chão (para o pulo do seu Player)

        private bool warnedLayer, warnedTag;

        private void ApplyGround(GameObject go)
        {
            if (go == null) return;
            if (!string.IsNullOrEmpty(groundLayerName))
            {
                int layer = LayerMask.NameToLayer(groundLayerName);
                if (layer >= 0) SetLayerDeep(go.transform, layer);
                else if (!warnedLayer)
                {
                    warnedLayer = true;
                    Debug.LogWarning("[Hive] A layer '" + groundLayerName + "' não existe. Crie em Edit ▸ Project Settings ▸ Tags and Layers " +
                                     "(ou escreva em HiveLevelBootstrap ▸ Ground Layer Name a layer que o Ground Check do seu Player usa). " +
                                     "Sem isso o Player pode não detectar o chão e não pular.");
                }
            }
            if (!string.IsNullOrEmpty(groundTag))
            {
                try { go.tag = groundTag; }
                catch (UnityException)
                {
                    if (!warnedTag)
                    {
                        warnedTag = true;
                        Debug.LogWarning("[Hive] A tag '" + groundTag + "' não existe. Crie em Tags and Layers ou deixe Ground Tag vazio.");
                    }
                }
            }
        }

        private static void SetLayerDeep(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerDeep(t.GetChild(i), layer);
        }

        /// <summary>Tilemaps "Ground" e "Platforms" (os que você pinta) também ganham a layer/tag do chão.</summary>
        private void ApplyGroundToTilemaps()
        {
            var grid = FindFirstObjectByType<Grid>();
            if (grid == null) return;
            for (int i = 0; i < grid.transform.childCount; i++)
            {
                var c = grid.transform.GetChild(i);
                if (c.name == "Ground" || c.name == "Platforms") ApplyGround(c.gameObject);
            }
        }

        // ============================================================ Marcadores → objetos

        private void BuildFromMarkers()
        {
            var markers = FindObjectsByType<HiveMarker>(FindObjectsSortMode.None);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] == null || !markers[i].gameObject.activeInHierarchy) continue;
                SpawnMarker(markers[i]);
            }
            Debug.Log("[Hive] " + markers.Length + " marcadores processados · " + vinylsTotal + " vinis na fase" +
                      (bossRequired ? " · boss: sim" : " · boss: não"));
        }

        private void SpawnMarker(HiveMarker m)
        {
            Vector2 pos = m.transform.position;
            GameObject go;

            switch (m.type)
            {
                case HiveMarkerType.PlayerSpawn:
                    spawnPoint = pos;
                    hasSpawn = true;
                    break;

                case HiveMarkerType.Checkpoint:
                    go = NewObj("Checkpoint", gCheckpoints, pos);
                    go.AddComponent<HiveCheckpoint>().Init();
                    break;

                case HiveMarkerType.VinylRow:
                {
                    int n = Mathf.Max(1, m.count);
                    for (int i = 0; i < n; i++)
                    {
                        HivePickup.Create(HivePickupType.Vinyl, m.VinylPoint(i), 1, false, gPickups);
                        vinylsTotal++;
                    }
                    break;
                }

                case HiveMarkerType.HealNote:
                    HivePickup.Create(HivePickupType.Heal, pos, 15, false, gPickups);
                    break;

                case HiveMarkerType.BeatPlatform:
                    go = NewObj("BeatPlatform", gPlatforms, pos);
                    go.AddComponent<HiveBeatPlatform>().Init(m.size, m.pattern, m.offset, m.tier);
                    ApplyGround(go);
                    break;

                case HiveMarkerType.MovingPlatform:
                    go = NewObj("MovingPlatform", gPlatforms, pos);
                    go.AddComponent<HiveMovingPlatform>().Init(m.size, m.travel, m.beats, m.offset);
                    ApplyGround(go);
                    break;

                case HiveMarkerType.CrumblePlatform:
                    go = NewObj("CrumblePlatform", gPlatforms, pos);
                    go.AddComponent<HiveCrumblePlatform>().Init(m.size, m.beats, m.count);
                    ApplyGround(go);
                    break;

                case HiveMarkerType.HoneyPool:
                    go = NewObj("HoneyPool", gHazards, pos);
                    go.AddComponent<HiveHoneyPool>().Init(m.size);
                    break;

                case HiveMarkerType.HoneyDrip:
                    go = NewObj("HoneyDrip", gHazards, pos);
                    go.AddComponent<HiveHoneyDrip>().Init(m.beats, m.offset);
                    break;

                case HiveMarkerType.KillZone:
                    go = NewObj("KillZone", gHazards, pos);
                    go.AddComponent<HiveKillZone>().Init(m.size);
                    break;

                case HiveMarkerType.WaxThorns:
                    SpawnThorns(m, pos);
                    break;

                case HiveMarkerType.WorkerBee:
                    go = NewObj("WorkerBee", gEnemies, pos);
                    go.AddComponent<HiveWorkerBee>().Init(m.travel.x, m.value);
                    break;

                case HiveMarkerType.HornetGuard:
                    go = NewObj("HornetGuard", gEnemies, pos);
                    go.AddComponent<HiveHornetGuard>().Init(m.travel.x, m.value);
                    break;

                case HiveMarkerType.BossArena:
                    go = NewObj("QueenArena", gBoss, pos);
                    arena = go.AddComponent<HiveQueenArena>();
                    arena.Init(m.size, m.value);
                    bossRequired = true;
                    break;

                case HiveMarkerType.LevelExit:
                    go = NewObj("LevelExit", gMisc, pos);
                    go.AddComponent<HiveLevelExit>().Init();
                    break;

                case HiveMarkerType.BlockoutBlock:
                    SpawnBlockout(m, pos);
                    break;

                case HiveMarkerType.MusicCue:
                    go = NewObj("MusicCue", gMisc, pos);
                    go.AddComponent<HiveMusicCue>().Init(m.size, (HiveMusicSection)Mathf.Clamp(m.tier, 1, 3));
                    break;
            }
        }

        private void SpawnThorns(HiveMarker m, Vector2 pos)
        {
            float w = Mathf.Max(0.5f, m.size.x);
            var go = NewObj("WaxThorns", gHazards, pos);
            go.AddComponent<HiveHazard>().Init(new Vector2(w, 0.55f), 15);

            var v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            v.transform.localPosition = new Vector3(0f, -0.3125f, 0f);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Thorns(w);
            sr.sortingOrder = 3;
        }

        private void SpawnBlockout(HiveMarker m, Vector2 pos)
        {
            var go = NewObj("Blockout", gMisc, pos);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = m.size;
            ApplyGround(go);

            HiveBlockStyle style = m.tier == 1 ? HiveBlockStyle.Platform :
                                   (m.tier == 2 ? HiveBlockStyle.Gold : HiveBlockStyle.Ground);

            int tx = Mathf.Max(1, Mathf.CeilToInt(m.size.x / 16f));
            int ty = Mathf.Max(1, Mathf.CeilToInt(m.size.y / 8f));
            float tw = m.size.x / tx;
            float th = m.size.y / ty;
            Sprite spr = HiveSpriteFactory.Block(tw, th, style);
            for (int i = 0; i < tx; i++)
                for (int j = 0; j < ty; j++)
                {
                    var t = new GameObject("Tile");
                    t.transform.SetParent(go.transform, false);
                    t.transform.localPosition = new Vector3(-m.size.x * 0.5f + tw * (i + 0.5f),
                                                            -m.size.y * 0.5f + th * (j + 0.5f), 0f);
                    var sr = t.AddComponent<SpriteRenderer>();
                    sr.sprite = spr;
                    sr.sortingOrder = 0;
                }
        }

        // ============================================================ Player / checkpoint / morte

        public void TeleportPlayer(Vector2 p)
        {
            var go = HiveBridge.Player;
            if (go == null) return;
            var rb = HiveBridge.PlayerRb;
            if (rb != null)
            {
                HiveBridge.SetVelocity(rb, Vector2.zero);
                rb.position = p;
            }
            go.transform.position = new Vector3(p.x, p.y, go.transform.position.z);
        }

        public void SetCheckpoint(HiveCheckpoint cp)
        {
            if (currentCheckpoint != null) currentCheckpoint.SetCurrent(false);
            currentCheckpoint = cp;
            cp.SetCurrent(true);
            respawnPoint = cp.RespawnPoint;

            // lembra a música deste trecho (boss volta como combate; intro volta como loop)
            HiveMusicSection t = HiveMusic.Target;
            checkpointMusic = t == HiveMusicSection.Boss ? HiveMusicSection.Combat :
                              (t == HiveMusicSection.Intro ? HiveMusicSection.Loop : t);
        }

        /// <summary>Caiu no abismo: perde pitDamage de HP e volta ao último checkpoint.</summary>
        public void PitFall()
        {
            if (dying || levelDone) return;
            HiveBridge.DamagePlayer(pitDamage, respawnPoint, 0f, true);
            RespawnPlayer();
        }

        public void RespawnPlayer()
        {
            TeleportPlayer(respawnPoint);
            HiveOverlay.Flash(Color.black, 0.8f, 0.5f);
            HiveFx.Ring(respawnPoint, HivePalette.Gold, 2.5f, 0.5f);
            HiveSfx.Play(HiveSfxId.Appear, 0.5f, 0.7f);
            if (arena != null) arena.ResetFight();
            HiveMusic.Request(checkpointMusic);
        }

        private IEnumerator DeathRoutine()
        {
            dying = true;
            deaths++;
            HiveOverlay.Toast("DE VOLTA AO CHECKPOINT", HivePalette.Honey, 1.6f);
            yield return new WaitForSeconds(deathDelay);
            RespawnPlayer();
            HiveBridge.RevivePlayer();
            yield return new WaitForSeconds(0.3f);
            dying = false;
        }

        // ============================================================ Eventos de jogo

        public void OnVinylCollected(bool countsInTotal)
        {
            if (countsInTotal)
            {
                vinylsCollected++;
                HiveOverlay.SetVinyls(vinylsCollected, vinylsTotal);
            }
            else bonusVinyls++;
        }

        public HivePickup SpawnPickup(HivePickupType type, Vector2 pos, int value, bool falls, bool countsInTotal)
        {
            var p = HivePickup.Create(type, pos, value, falls, gPickups);
            p.countsInTotal = countsInTotal;
            return p;
        }

        /// <summary>Inimigo morreu: chance de nota de cura, senão derruba vinis. Sem drops durante o boss.</summary>
        public void OnEnemyKilled(Vector2 pos, float healChance, int vinyls)
        {
            if (BossActive) return;
            if (Random.value < healChance)
            {
                SpawnPickup(HivePickupType.Heal, pos + Vector2.up * 0.4f, 15, true, false);
                return;
            }
            for (int i = 0; i < vinyls; i++)
            {
                Vector2 off = new Vector2((i - (vinyls - 1) * 0.5f) * 0.6f, 0.4f);
                SpawnPickup(HivePickupType.Vinyl, pos + off, 1, true, false);
            }
        }

        public void OnBossDefeated()
        {
            bossDefeated = true;
            HiveMusic.Request(HiveMusicSection.Loop);   // a Rainha caiu: a música volta ao loop principal
        }

        public void OnFragmentCollected()
        {
            StartCoroutine(CompleteAfter(1.4f));
        }

        private IEnumerator CompleteAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            CompleteLevel();
        }

        public void CompleteLevel()
        {
            if (levelDone) return;
            levelDone = true;

            int mins = Mathf.FloorToInt(levelTime / 60f);
            int secs = Mathf.FloorToInt(levelTime % 60f);
            string vin = "Vinis:  " + vinylsCollected + " / " + vinylsTotal + (bonusVinyls > 0 ? "  (+" + bonusVinyls + ")" : "");
            string time = "Tempo:  " + mins.ToString("00") + ":" + secs.ToString("00");
            string dth = "Voltas ao checkpoint:  " + deaths;

            try
            {
                PlayerPrefs.SetInt("Hive_Cleared", 1);
                float best = PlayerPrefs.GetFloat("Hive_BestTime", 0f);
                if (best <= 0f || levelTime < best) PlayerPrefs.SetFloat("Hive_BestTime", levelTime);
                if (vinylsCollected > PlayerPrefs.GetInt("Hive_BestVinyls", 0)) PlayerPrefs.SetInt("Hive_BestVinyls", vinylsCollected);
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Hive] PlayerPrefs: " + e.Message);
            }

            HiveMusic.FadeOut(1.5f);
            HiveSfx.Play(HiveSfxId.Victory, 0.8f, 1f);
            HiveFx.Burst(HiveBridge.Player != null ? (Vector2)HiveBridge.Player.transform.position : Vector2.zero,
                         HivePalette.Gold, 40, 9f, 1.2f, 0.28f, 2f);

            string next = nextSceneName;
            HiveOverlay.ShowVictory(new[] { vin, time, dth }, () =>
            {
                if (string.IsNullOrEmpty(next)) return;
                HiveBridge.LoadScene(next);
            });
        }
    }
}