using System;
using UnityEditor;
using UnityEngine;

namespace PopAdventure.Fase3.EditorTools
{
    /// <summary>Janela do construtor: Pop Adventure > Fase 3 > Construtor.</summary>
    public class GGFase3Window : EditorWindow
    {
        const string KeyPlayer = "PA_GG3_Player";
        const string KeyProjectile = "PA_GG3_Projectile";
        const string KeyLevelMusic = "PA_GG3_LevelMusic";
        const string KeyBossMusic = "PA_GG3_BossMusic";

        GameObject playerPrefab;
        GameObject projectilePrefab;
        AudioClip levelMusic;
        AudioClip bossMusic;
        float bpm = 114f;
        bool generateLayout = true;
        Vector2 scroll;

        [MenuItem("Pop Adventure/Fase 3/Construtor da Fase 3")]
        static void Open()
        {
            GGFase3Window w = GetWindow<GGFase3Window>("Fase 3 - Graveyard Groove");
            w.minSize = new Vector2(430f, 520f);
        }

        void OnEnable()
        {
            playerPrefab = LoadAsset<GameObject>(KeyPlayer);
            projectilePrefab = LoadAsset<GameObject>(KeyProjectile);
            levelMusic = LoadAsset<AudioClip>(KeyLevelMusic);
            bossMusic = LoadAsset<AudioClip>(KeyBossMusic);
        }

        static T LoadAsset<T>(string key) where T : UnityEngine.Object
        {
            string path = EditorPrefs.GetString(key, "");
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        static void SaveAsset(string key, UnityEngine.Object o)
        {
            EditorPrefs.SetString(key, o != null ? AssetDatabase.GetAssetPath(o) : "");
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("POP ADVENTURE — Fase 3: Graveyard Groove", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Passo 1 cria Tiles, as duas Tile Palettes, os prefabs e o Animator do boss.\n"
                + "Passo 2 monta a cena (Grid + layout jogável + boss + sistemas).", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seus assets", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), false);
            projectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projétil (nota) Prefab", projectilePrefab, typeof(GameObject), false);
            levelMusic = (AudioClip)EditorGUILayout.ObjectField("Música da fase", levelMusic, typeof(AudioClip), false);
            bossMusic = (AudioClip)EditorGUILayout.ObjectField("Música do boss", bossMusic, typeof(AudioClip), false);
            bpm = EditorGUILayout.FloatField("BPM (Fase 3 = 114)", bpm);
            generateLayout = EditorGUILayout.Toggle("Gerar layout base da fase", generateLayout);
            if (EditorGUI.EndChangeCheck())
            {
                SaveAsset(KeyPlayer, playerPrefab);
                SaveAsset(KeyProjectile, projectilePrefab);
                SaveAsset(KeyLevelMusic, levelMusic);
                SaveAsset(KeyBossMusic, bossMusic);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("1) Configurar sprites + Tiles + Paletas + Prefabs", GUILayout.Height(32f)))
                Run(RunStep1);

            if (GUILayout.Button("2) Construir cena da Fase 3", GUILayout.Height(32f)))
                Run(RunStep2);

            if (GUILayout.Button("Tudo (1 + 2)", GUILayout.Height(28f)))
                Run(delegate { RunStep1(); RunStep2(); });

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Opcional", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Adiciona GGPlayerHealth (e tag Player) ao prefab do Player e GGProjectileRelay ao prefab do projétil, "
                + "para espinhos, inimigos e boss causarem/receberem dano. Se o seu Player já tem vida própria, "
                + "desmarque 'Use Internal Health' no GGPlayerHealth e ligue o evento onDamaged ao seu método.", MessageType.None);
            using (new EditorGUI.DisabledScope(playerPrefab == null && projectilePrefab == null))
            {
                if (GUILayout.Button("Preparar Player / Projétil"))
                    Run(RunPrepare);
            }

            EditorGUILayout.EndScrollView();
        }

        static void Run(Action a)
        {
            try { a(); }
            catch (Exception e) { Debug.LogError("[Fase3] Erro: " + e); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        // ------------------------------------------------------------------ passos
        static void RunStep1()
        {
            if (!AssetDatabase.IsValidFolder(GGPaths.Art))
            {
                EditorUtility.DisplayDialog("Fase 3",
                    "Pasta " + GGPaths.Art + " não encontrada. Copie a pasta Fase3_Graveyard inteira para dentro de Assets.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Fase 3", "Importando sprites...", 0.10f);
            int changed = GGTileFactory.ConfigureSprites();

            EditorUtility.DisplayProgressBar("Fase 3", "Criando Tiles...", 0.30f);
            System.Collections.Generic.Dictionary<string, UnityEngine.Tilemaps.Tile> tiles = GGTileFactory.CreateTiles();

            EditorUtility.DisplayProgressBar("Fase 3", "Criando prefabs...", 0.50f);
            GGPrefabFactory.CreateAll();

            EditorUtility.DisplayProgressBar("Fase 3", "Criando paleta de Tiles...", 0.80f);
            GGTileFactory.CreateTilePalette(tiles);

            EditorUtility.DisplayProgressBar("Fase 3", "Criando paleta de Objetos...", 0.90f);
            GGTileFactory.CreateObjectsPalette(GGPrefabFactory.PaletteEntries());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Fase3] Passo 1 concluído (" + changed + " sprites reconfigurados). Paletas em " + GGPaths.Palettes
                      + " | Prefabs em " + GGPaths.Prefabs);
        }

        void RunStep2()
        {
            GGSceneOptions o = new GGSceneOptions();
            o.playerPrefab = playerPrefab;
            o.levelMusic = levelMusic;
            o.bossMusic = bossMusic;
            o.bpm = bpm;
            o.generateLayout = generateLayout;
            GGSceneBuilder.Build(o);
        }

        void RunPrepare()
        {
            bool ok = EditorUtility.DisplayDialog("Fase 3",
                "Isto vai MODIFICAR os prefabs selecionados:\n"
                + "• Player: adiciona GGPlayerHealth e define a tag Player\n"
                + "• Projétil: adiciona GGProjectileRelay (dano 10)\n\nContinuar?", "Continuar", "Cancelar");
            if (!ok) return;

            if (playerPrefab != null)
                ModifyPrefab(playerPrefab, delegate (GameObject go)
                {
                    if (go.GetComponentInChildren<GGPlayerHealth>() == null) go.AddComponent<GGPlayerHealth>();
                    go.tag = "Player";
                });
            if (projectilePrefab != null)
                ModifyPrefab(projectilePrefab, delegate (GameObject go)
                {
                    if (go.GetComponent<GGProjectileRelay>() == null) go.AddComponent<GGProjectileRelay>();
                });
            AssetDatabase.SaveAssets();
            Debug.Log("[Fase3] Player/Projétil preparados.");
        }

        static void ModifyPrefab(GameObject prefabAsset, Action<GameObject> edit)
        {
            string path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[Fase3] " + prefabAsset.name + " precisa ser um prefab da pasta Project.");
                return;
            }
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
