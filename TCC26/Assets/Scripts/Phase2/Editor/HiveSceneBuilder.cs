using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace PopAdventure.Hive.EditorTools
{
    /// <summary>
    /// Menus: Pop Adventure ▸ Fase 2
    ///   1 - Construir Hierarquia na Cena Atual   (idempotente: só cria o que falta)
    ///   2 - Criar Cena Nova (Phase2_TheHive)     (cena vazia + hierarquia + Build Settings)
    ///   3 - Gerar Layout de Teste (Blockout)     (fase jogável de teste com todos os elementos)
    ///   4 - Limpar Tilemaps da Cena              (para cena duplicada da Fase 1)
    /// </summary>
    public static class HiveSceneBuilder
    {
        private const string Menu = "Pop Adventure/Fase 2/";
        private const string ScenePath = "Assets/Scenes/Phase2_TheHive.unity";

        public static readonly string[] MarkerGroups =
        {
            "00_Spawn_Checkpoints", "01_Platforms", "02_Hazards", "03_Pickups",
            "04_Enemies", "05_Boss_Exit", "06_Music", "99_Blockout"
        };

        // ============================================================ Helpers

        private static GameObject FindRoot(string name)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var g in scene.GetRootGameObjects())
                if (g.name == name) return g;
            return null;
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            var g = FindRoot(name);
            if (g == null)
            {
                g = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(g, "Hive: criar " + name);
            }
            return g;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var g = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(g, "Hive: criar " + name);
            g.transform.SetParent(parent, false);
            return g;
        }

        private static T Ensure<T>(GameObject g) where T : Component
        {
            var c = g.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(g);
            return c;
        }

        public static Transform GetMarkerGroup(string group)
        {
            var markers = GetOrCreateRoot("[Markers]");
            return GetOrCreateChild(markers.transform, group).transform;
        }

        // ============================================================ 1 - Hierarquia

        [MenuItem(Menu + "1 - Construir Hierarquia na Cena Atual", false, 10)]
        public static void BuildHierarchyMenu()
        {
            BuildHierarchy(true);
        }

        public static void BuildHierarchy(bool report)
        {
            Undo.IncrementCurrentGroup();

            // ---- [Systems]
            var systems = GetOrCreateRoot("[Systems]");
            var beat = GetOrCreateChild(systems.transform, "HiveBeatDriver");
            Ensure<HiveBeatDriver>(beat);
            var music = GetOrCreateChild(systems.transform, "HiveMusic");
            Ensure<HiveMusic>(music);
            var boot = GetOrCreateChild(systems.transform, "HiveLevelBootstrap");
            Ensure<HiveLevelBootstrap>(boot);

            // ---- Grid + Tilemaps
            var existingGrid = Object.FindFirstObjectByType<Grid>();
            GameObject gridGo = existingGrid != null ? existingGrid.gameObject : GetOrCreateRoot("Grid");
            Ensure<Grid>(gridGo);

            Layer(gridGo.transform, "Background", -2, "pulse");
            Layer(gridGo.transform, "Ground", 0, "ground");
            Layer(gridGo.transform, "Platforms", 1, "platform");
            Layer(gridGo.transform, "Hazards", 2, "hazard");
            Layer(gridGo.transform, "Foreground", 5, "pulse");

            // ---- [Markers] + [Dynamic]
            var markers = GetOrCreateRoot("[Markers]");
            foreach (var g in MarkerGroups) GetOrCreateChild(markers.transform, g);
            GetOrCreateRoot("[Dynamic]");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            if (!report) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Hive] Hierarquia da Fase 2 pronta ([Systems], Grid com 5 Tilemaps, [Markers], [Dynamic]).");
            bool hasPlayer = false;
            try { hasPlayer = GameObject.FindGameObjectWithTag("Player") != null; } catch (UnityException) { }
            sb.AppendLine(hasPlayer ? " ✔ Player encontrado (tag Player)." :
                          " ✘ FALTA o Player: arraste o prefab do Player da Fase 1 para a cena (tag 'Player').");
            sb.AppendLine(Camera.main != null ? " ✔ Main Camera encontrada." : " ✘ FALTA a Main Camera (copie a da Fase 1 com o Cinemachine).");
            sb.AppendLine(Object.FindFirstObjectByType<UnityEngine.UIElements.UIDocument>() != null
                ? " ✔ HUD (UIDocument) encontrado."
                : " ✘ FALTA o HUD: copie o objeto do HUD (UIDocument) da Fase 1.");
            int found = 0;
            foreach (var n in new[] { "hive_intro", "hive_loop", "hive_combate", "hive_boss" })
                if (Resources.Load<AudioClip>("Hive/" + n) != null) found++;
            sb.AppendLine(found == 4
                ? " ✔ Música: 4 seções em Assets/Resources/Hive (intro, loop, combate, boss)."
                : " ✘ Música: só " + found + "/4 seções em Assets/Resources/Hive — copie os .wav do pacote para essa pasta " +
                  "(ou arraste os clipes em [Systems] ▸ HiveMusic).");
            Debug.Log(sb.ToString());
        }

        private static void Layer(Transform grid, string name, int order, string kind)
        {
            var go = GetOrCreateChild(grid, name);
            bool fresh = go.GetComponent<Tilemap>() == null;
            if (fresh)
            {
                Ensure<Tilemap>(go);
                var tr = Ensure<TilemapRenderer>(go);
                tr.sortingOrder = order;
            }

            switch (kind)
            {
                case "ground":
                    if (fresh) SetupSolid(go, false);
                    break;
                case "platform":
                    if (fresh) SetupSolid(go, true);
                    break;
                case "hazard":
                    Ensure<HiveTilemapHazard>(go);   // adiciona TilemapCollider2D e o torna trigger em Play
                    break;
                case "pulse":
                    Ensure<HiveTilemapPulse>(go);
                    break;
            }
        }

        private static void SetupSolid(GameObject go, bool oneWay)
        {
            var tc = Ensure<TilemapCollider2D>(go);
            var cc = Ensure<CompositeCollider2D>(go);
            tc.usedByComposite = true;
            cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;

            if (oneWay)
            {
                cc.usedByEffector = true;
                var eff = Ensure<PlatformEffector2D>(go);
                eff.useOneWay = true;
                eff.surfaceArc = 170f;
                eff.useSideFriction = false;
                eff.useSideBounce = false;
            }
        }

        // ============================================================ 2 - Cena nova

        [MenuItem(Menu + "2 - Criar Cena Nova (Phase2_TheHive)", false, 11)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var light = GameObject.Find("Directional Light");
            if (light != null) UnityEngine.Object.DestroyImmediate(light);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6.5f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = HivePalette.Black;
                cam.transform.position = new Vector3(0f, 3f, -10f);
            }

            BuildHierarchy(false);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);

            Debug.Log("[Hive] Cena criada: " + ScenePath + " (já nas Build Settings).\n" +
                      "Próximo: traga Player, Main Camera/Cinemachine e HUD da Fase 1 (copiar/colar entre cenas) " +
                      "ou use a Paleta de Marcadores e o menu 3 para um layout de teste.");
        }

        private static void AddToBuildSettings(string path)
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == path)) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ============================================================ 4 - Limpar tilemaps

        [MenuItem(Menu + "4 - Limpar Tilemaps da Cena (cena duplicada da Fase 1)", false, 13)]
        public static void ClearTilemaps()
        {
            if (!EditorUtility.DisplayDialog("Limpar Tilemaps",
                    "Apaga TODOS os tiles pintados em TODOS os Tilemaps da cena atual (dá para desfazer com Ctrl+Z).\n\n" +
                    "Use depois de duplicar a cena da Fase 1.", "Limpar", "Cancelar"))
                return;

            int n = 0;
            foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                Undo.RecordObject(tm, "Limpar Tilemap");
                tm.ClearAllTiles();
                n++;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Hive] " + n + " Tilemaps limpos. Apague manualmente inimigos/boss/objetos específicos da Fase 1.");
        }

        // ============================================================ 3 - Layout de teste

        [MenuItem(Menu + "3 - Gerar Layout de Teste (Blockout)", false, 12)]
        public static void GenerateTestLayout()
        {
            if (!EditorUtility.DisplayDialog("Gerar layout de teste",
                    "Cria ~60 marcadores formando uma fase jogável de teste (A: plataformas no ritmo, B: mel e " +
                    "plataformas móveis, C: esfarelantes e vespa, D: arena do boss) com chão de blocos temporários.\n\n" +
                    "Os blocos ficam em [Markers]/99_Blockout — apague esse grupo quando pintar os tiles.",
                    "Gerar", "Cancelar"))
                return;

            BuildHierarchy(false);

            // ---- chão e paredes (BlockoutBlock: centro, tamanho)
            Block(8f, -2f, 24f, 4f);
            Block(48f, -2f, 16f, 4f);
            Block(82f, -2f, 16f, 4f);
            Block(122f, -2f, 24f, 4f);
            Block(162f, -2f, 56f, 4f);
            Block(-5f, 5f, 2f, 14f);
            Block(191f, 5f, 2f, 14f);

            // ---- spawn / checkpoints
            Mk(HiveMarkerType.PlayerSpawn, 2f, 1f);
            Mk(HiveMarkerType.Checkpoint, 42f, 0f);
            Mk(HiveMarkerType.Checkpoint, 76f, 0f);
            Mk(HiveMarkerType.Checkpoint, 112f, 0f);
            Mk(HiveMarkerType.Checkpoint, 141f, 0f);

            // ---- A: plataformas no ritmo (onda rolando para a direita)
            Mk(HiveMarkerType.KillZone, 30f, -8f, m => m.size = new Vector2(24f, 2f));
            Mk(HiveMarkerType.BeatPlatform, 23f, 1.5f, m => { m.pattern = "1100"; m.offset = 0; });
            Mk(HiveMarkerType.BeatPlatform, 27f, 2.5f, m => { m.pattern = "1100"; m.offset = 3; m.tier = 1; });
            Mk(HiveMarkerType.BeatPlatform, 31f, 1.5f, m => { m.pattern = "1100"; m.offset = 2; });
            Mk(HiveMarkerType.BeatPlatform, 34.5f, 2.5f, m => { m.pattern = "1100"; m.offset = 1; m.tier = 1; });
            Mk(HiveMarkerType.VinylRow, 8f, 1.3f, m => { m.count = 5; m.spacing = 1.2f; m.arc = 1.2f; });
            Mk(HiveMarkerType.VinylRow, 27f, 5f, m => { m.count = 8; m.spacing = 1.2f; m.arc = 1.5f; });
            Mk(HiveMarkerType.WorkerBee, 46f, 3.5f, m => m.travel = new Vector2(8f, 0f));

            // ---- B: plataforma móvel + mel
            Mk(HiveMarkerType.KillZone, 65f, -8f, m => m.size = new Vector2(24f, 2f));
            Mk(HiveMarkerType.MovingPlatform, 58f, 1f, m => { m.travel = new Vector2(14f, 0f); m.beats = 4; });
            Mk(HiveMarkerType.VinylRow, 65f, 4f, m => { m.count = 6; m.spacing = 2.2f; m.arc = 2f; });
            Mk(HiveMarkerType.HoneyPool, 80f, 0.4f, m => m.size = new Vector2(6f, 0.8f));
            Mk(HiveMarkerType.HoneyDrip, 84f, 7f, m => { m.beats = 4; m.offset = 0; });
            Mk(HiveMarkerType.HoneyDrip, 86f, 7f, m => { m.beats = 4; m.offset = 2; });
            Mk(HiveMarkerType.WaxThorns, 88.5f, 0.3125f, m => m.size = new Vector2(2f, 0.625f));
            Mk(HiveMarkerType.VinylRow, 82f, 1.3f, m => { m.count = 4; m.spacing = 1.2f; m.arc = 0f; });
            Mk(HiveMarkerType.WorkerBee, 84f, 4.2f, m => m.travel = new Vector2(6f, 0f));

            // ---- C: plataformas que esfarelam + vespa-guarda
            Mk(HiveMarkerType.KillZone, 100f, -8f, m => m.size = new Vector2(26f, 2f));
            Mk(HiveMarkerType.CrumblePlatform, 92.5f, 1f);
            Mk(HiveMarkerType.CrumblePlatform, 96.5f, 2f);
            Mk(HiveMarkerType.CrumblePlatform, 100.5f, 3f);
            Mk(HiveMarkerType.CrumblePlatform, 104.5f, 2f);
            Mk(HiveMarkerType.CrumblePlatform, 108f, 1f);
            Mk(HiveMarkerType.VinylRow, 100.5f, 5.2f, m => { m.count = 5; m.spacing = 2f; m.arc = 2f; });
            Mk(HiveMarkerType.HornetGuard, 120f, 0.6f, m => m.travel = new Vector2(10f, 0f));
            Mk(HiveMarkerType.VinylRow, 122f, 1.3f, m => { m.count = 5; m.spacing = 1.2f; m.arc = 0f; });
            Mk(HiveMarkerType.HealNote, 126f, 1f);
            Mk(HiveMarkerType.WorkerBee, 130f, 3.5f, m => m.travel = new Vector2(6f, 0f));

            // ---- música: COMBATE na zona C, volta ao LOOP antes do checkpoint do boss (o boss troca sozinho)
            Mk(HiveMarkerType.MusicCue, 91f, 3f, m => { m.size = new Vector2(2f, 18f); m.tier = 2; });
            Mk(HiveMarkerType.MusicCue, 136f, 3f, m => { m.size = new Vector2(2f, 18f); m.tier = 1; });

            // ---- D: arena da Rainha (chão = base do retângulo: y 0) + saída
            Mk(HiveMarkerType.BossArena, 160f, 5f, m => m.size = new Vector2(22f, 10f));
            Mk(HiveMarkerType.LevelExit, 186f, 1.5f);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Hive] Layout de teste gerado. Coloque o Player na cena (tag 'Player'), confirme os 4 .wav em " +
                      "Assets/Resources/Hive e dê Play. Câmera: Cinemachine Orthographic Size ≥ 6.5 para enxergar a arena inteira.");
        }

        private static void Mk(HiveMarkerType t, float x, float y, System.Action<HiveMarker> cfg = null)
        {
            var m = HiveMarkerFactory.Create(t, new Vector3(x, y, 0f), false);
            if (cfg != null) cfg(m);
        }

        private static void Block(float x, float y, float w, float h)
        {
            Mk(HiveMarkerType.BlockoutBlock, x, y, m => m.size = new Vector2(w, h));
        }
    }
}
