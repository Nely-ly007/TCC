using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace PopAdventure.Fase3.EditorTools
{
    public class GGSceneOptions
    {
        public GameObject playerPrefab;
        public AudioClip levelMusic;
        public AudioClip bossMusic;
        public float bpm = 114f;
        public bool generateLayout = true;
    }

    /// <summary>
    /// Monta a cena da Fase 3 (hierarquia no mesmo estilo da fase 2: Grid com Background/Ground/Platforms/Foreground).
    /// O layout jogável fica em GGSceneBuilder.Layout.cs (partial).
    /// </summary>
    public partial class GGSceneBuilder
    {
        const int MinX = -12;
        const int MaxX = 262;
        const int BottomY = -6;
        const int NoGround = int.MinValue;

        readonly GGSceneOptions opt;
        readonly int[] heights = new int[MaxX - MinX + 1];
        readonly Dictionary<string, Tile> tileCache = new Dictionary<string, Tile>();

        Tilemap tmBackground, tmGround, tmPlatforms, tmForeground;
        Transform gridT, decor, hazards, rhythm, pickups, interact, enemies, bossFight;
        GameObject spawnPoint;

        GGSceneBuilder(GGSceneOptions options)
        {
            opt = options;
            for (int i = 0; i < heights.Length; i++) heights[i] = NoGround;
        }

        // ------------------------------------------------------------------ entrada
        public static void Build(GGSceneOptions options)
        {
            if (GGEditorUtil.LoadPrefab("Enemy_Crow") == null || GGTileFactory.LoadTile("Ground_Top_M") == null)
            {
                EditorUtility.DisplayDialog("Fase 3", "Rode primeiro o passo 1 (Configurar sprites + Tiles + Paletas + Prefabs).", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(GGPaths.SceneFile))
            {
                bool ok = EditorUtility.DisplayDialog("Fase 3",
                    "A cena " + GGPaths.SceneFile + " já existe e será SUBSTITUÍDA (o que você editou nela será perdido). Continuar?",
                    "Substituir", "Cancelar");
                if (!ok) return;
            }

            GGEditorUtil.ClearCaches();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GGSceneBuilder b = new GGSceneBuilder(options);
            b.BuildAll();

            GGEditorUtil.EnsureFolder(GGPaths.Scenes);
            EditorSceneManager.SaveScene(scene, GGPaths.SceneFile);
            AddToBuildSettings(GGPaths.SceneFile);
            AssetDatabase.Refresh();
            Debug.Log("[Fase3] Cena criada: " + GGPaths.SceneFile);
        }

        static void AddToBuildSettings(string path)
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            for (int i = 0; i < current.Length; i++)
                if (current[i].path == path) return;
            EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[current.Length + 1];
            for (int i = 0; i < current.Length; i++) next[i] = current[i];
            next[current.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = next;
        }

        void BuildAll()
        {
            spawnPoint = new GameObject("PlayerSpawn");
            spawnPoint.transform.position = new Vector3(0f, 0.3f, 0f);

            CreateCameraAndSystems();
            CreateGrid();
            CreateBackdrop();

            if (opt.generateLayout) BuildLayout();
            else BuildEmptyLayout();

            DrawTerrain();
            FinishLayout();
            CreateKillZone();
            CreatePlayer();
        }

        // ------------------------------------------------------------------ sistemas
        void CreateCameraAndSystems()
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(2.7f, 1.7f, -10f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.02f, 0.07f, 1f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<GGCameraFollow>();

            GameObject systems = new GameObject("Systems");
            GGBeatClock clock = systems.AddComponent<GGBeatClock>();
            GGEditorUtil.SetMany(clock, "bpm", opt.bpm, "levelMusic", opt.levelMusic, "bossMusic", opt.bossMusic);
            GGLevelManager lm = systems.AddComponent<GGLevelManager>();
            GGEditorUtil.Set(lm, "initialSpawn", spawnPoint.transform);
        }

        // ------------------------------------------------------------------ grid
        void CreateGrid()
        {
            GameObject gridGo = new GameObject("Grid");
            Grid grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            gridT = gridGo.transform;

            tmBackground = MakeTilemap("Background", -2, false, false);
            tmBackground.color = new Color(0.42f, 0.42f, 0.52f, 1f);
            tmGround = MakeTilemap("Ground", 0, true, false);
            tmPlatforms = MakeTilemap("Platforms", 1, true, true);
            tmForeground = MakeTilemap("Foreground", 5, false, false);

            // Grupos para objetos (têm um Tilemap vazio só para aparecerem como "Active Target" no Tile Palette).
            decor = MakeGroup("Decor");
            hazards = MakeGroup("Hazards");
            rhythm = MakeGroup("RhythmPlatforms");
            pickups = MakeGroup("Pickups");
            interact = MakeGroup("Interactables");
            enemies = MakeGroup("Enemies");

            GameObject bf = new GameObject("BossFight");
            bossFight = bf.transform;
        }

        Tilemap MakeTilemap(string name, int order, bool collider, bool oneWay)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(gridT, false);
            Tilemap tm = go.AddComponent<Tilemap>();
            TilemapRenderer tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = order;

            if (collider)
            {
                Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                CompositeCollider2D cc = go.AddComponent<CompositeCollider2D>();
                cc.geometryType = oneWay ? CompositeCollider2D.GeometryType.Polygons : CompositeCollider2D.GeometryType.Outlines;
                TilemapCollider2D tc = go.AddComponent<TilemapCollider2D>();
#if UNITY_2023_1_OR_NEWER
                tc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
                tc.usedByComposite = true;
#endif
                if (oneWay)
                {
                    cc.usedByEffector = true;
                    PlatformEffector2D eff = go.AddComponent<PlatformEffector2D>();
                    eff.useOneWay = true;
                    eff.surfaceArc = 160f;
                    eff.useSideFriction = false;
                    eff.useSideBounce = false;
                }
            }
            return tm;
        }

        Transform MakeGroup(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(gridT, false);
            go.AddComponent<Tilemap>();
            return go.transform;
        }

        // ------------------------------------------------------------------ fundo e névoa
        void CreateBackdrop()
        {
            Sprite bg = GGEditorUtil.LoadSprite("background_moonlit_cemetery");
            GameObject root = new GameObject("Backdrop");
            root.transform.position = new Vector3(2.7f, 1.7f, 0f);
            GGParallax par = root.AddComponent<GGParallax>();
            GGEditorUtil.Set(par, "factor", new Vector2(0.85f, 0.9f));
            if (bg != null)
            {
                float w = bg.rect.width / bg.pixelsPerUnit;
                for (int i = -3; i <= 3; i++)
                {
                    GameObject t = new GameObject("BG_" + (i + 3));
                    t.transform.SetParent(root.transform, false);
                    t.transform.localPosition = new Vector3(i * (w - 0.02f), 0f, 0f);
                    SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                    sr.sprite = bg;
                    sr.sortingOrder = -10;
                    sr.flipX = (Mathf.Abs(i) % 2) == 1; // espelha as vizinhas: emenda perfeita
                }
            }

            MakeFog("Fog_Back", "fog_a", -3, 0.55f, 0.6f, 0.35f, -2.0f);
            MakeFog("Fog_Front", "fog_b", 6, 0.35f, 0.95f, -0.5f, -4.2f);
        }

        void MakeFog(string name, string spriteName, int order, float alpha, float parallax, float drift, float yOffset)
        {
            Sprite sp = GGEditorUtil.LoadSprite(spriteName);
            GameObject root = new GameObject(name);
            float w = sp != null ? sp.rect.width / sp.pixelsPerUnit : 20f;
            Object[] tiles = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject t = new GameObject("Fog_" + i);
                t.transform.SetParent(root.transform, false);
                SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = order;
                sr.color = new Color(1f, 1f, 1f, alpha);
                tiles[i] = t.transform;
            }
            GGFogLayer fog = root.AddComponent<GGFogLayer>();
            GGEditorUtil.SetArray(fog, "tiles", tiles);
            GGEditorUtil.SetMany(fog, "tileWidth", w, "parallax", parallax, "driftSpeed", drift, "yOffsetFromCamera", yOffset);
        }

        // ------------------------------------------------------------------ abismo e player
        void CreateKillZone()
        {
            GameObject kz = new GameObject("KillZone");
            kz.transform.position = new Vector3((MinX + MaxX) * 0.5f, -9f, 0f);
            GGEditorUtil.AddBox(kz, new Vector2(MaxX - MinX + 20f, 4f), Vector2.zero, true);
            kz.AddComponent<GGKillZone>();
        }

        void CreatePlayer()
        {
            if (opt.playerPrefab == null) return;
            GameObject p = PrefabUtility.InstantiatePrefab(opt.playerPrefab) as GameObject;
            if (p == null) return;
            p.transform.position = spawnPoint.transform.position;
            if (!p.CompareTag("Player")) p.tag = "Player";
            if (p.GetComponentInChildren<GGPlayerHealth>() == null)
                Debug.LogWarning("[Fase3] O Player da cena não tem GGPlayerHealth: espinhos/inimigos não vão causar dano. "
                                 + "Use o botão 'Preparar Player/Projétil' ou adicione o componente.");
        }

        // ------------------------------------------------------------------ terreno
        int H(int x)
        {
            int i = x - MinX;
            return (i < 0 || i >= heights.Length) ? NoGround : heights[i];
        }

        bool Has(int x) { return H(x) != NoGround; }

        float SurfaceAt(float worldX) { return H(Mathf.FloorToInt(worldX)); }

        void Run(int x0, int x1, int h)
        {
            for (int x = x0; x <= x1; x++)
            {
                int i = x - MinX;
                if (i >= 0 && i < heights.Length) heights[i] = h;
            }
        }

        Tile T(string name)
        {
            Tile t;
            if (!tileCache.TryGetValue(name, out t))
            {
                t = GGTileFactory.LoadTile(name);
                tileCache[name] = t;
            }
            return t;
        }

        void Paint(Tilemap tm, int x, int y, string tileName)
        {
            Tile t = T(tileName);
            if (t != null) tm.SetTile(new Vector3Int(x, y, 0), t);
        }

        void FillRect(Tilemap tm, int x0, int y0, int x1, int y1, string tileName)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    Paint(tm, x, y, tileName);
        }

        string FillTile(int x, int y)
        {
            int r = ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;
            int k = r % 12;
            if (k < 6) return "Wall_Stone";
            if (k < 9) return "Wall_Mossy";
            if (k < 11) return "Wall_Cracked";
            return "Wall_Brick";
        }

        void DrawTerrain()
        {
            for (int x = MinX; x <= MaxX; x++)
            {
                if (!Has(x)) continue;
                int h = H(x);
                bool leftOpen = !Has(x - 1) || H(x - 1) < h;
                bool rightOpen = !Has(x + 1) || H(x + 1) < h;
                string top = (leftOpen && rightOpen) ? "Ground_Single" : leftOpen ? "Ground_Top_L" : rightOpen ? "Ground_Top_R" : "Ground_Top_M";
                Paint(tmGround, x, h - 1, top);
                for (int y = h - 2; y >= BottomY; y--) Paint(tmGround, x, y, FillTile(x, y));
            }
        }

        /// <summary>Plataforma fina (one-way) com a superfície de pisar em surfaceY. Células x0..x1.</summary>
        void PlatTiles(int x0, int x1, int surfaceY)
        {
            int y = surfaceY - 1;
            for (int x = x0; x <= x1; x++)
            {
                string n = x0 == x1 ? "Plat_Thin_Single" : x == x0 ? "Plat_Thin_L" : x == x1 ? "Plat_Thin_R" : "Plat_Thin_Single";
                Paint(tmPlatforms, x, y, n);
            }
        }

        // ------------------------------------------------------------------ instanciar prefabs
        GameObject Mid(string prefabName, Transform parent, float x, float y)
        {
            GameObject prefab = GGEditorUtil.LoadPrefab(prefabName);
            if (prefab == null) return null;
            GameObject go = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (go == null) return null;
            go.transform.position = new Vector3(x, y, 0f);
            GGEditorUtil.RecordInstance(go);
            return go;
        }

        /// <summary>Objeto "em pé" numa superfície (raiz no centro da célula, base do sprite na superfície).</summary>
        GameObject Stand(string prefabName, Transform parent, float x, float surfaceY)
        {
            return Mid(prefabName, parent, x, surfaceY + 0.5f);
        }

        GameObject OnGround(string prefabName, Transform parent, float x)
        {
            if (!Has(Mathf.FloorToInt(x)))
            {
                Debug.LogWarning("[Fase3] Sem chão em x=" + x + " para " + prefabName + " (ignorado).");
                return null;
            }
            return Stand(prefabName, parent, x, SurfaceAt(x));
        }

        /// <summary>Plataformas: a superfície de pisar fica em raiz + 0.5.</summary>
        GameObject Hang(string prefabName, Transform parent, float x, float surfaceY)
        {
            return Mid(prefabName, parent, x, surfaceY - 0.5f);
        }

        /// <summary>Raiz nos PÉS (Lobo, Boss).</summary>
        GameObject Feet(string prefabName, Transform parent, float x, float y)
        {
            return Mid(prefabName, parent, x, y);
        }

        void Many(string prefabName, Transform parent, params float[] xs)
        {
            for (int i = 0; i < xs.Length; i++) OnGround(prefabName, parent, xs[i]);
        }

        void Vinyls(params float[] xy)
        {
            for (int i = 0; i + 1 < xy.Length; i += 2) Mid("Pickup_Vinyl", pickups, xy[i], xy[i + 1]);
        }

        static void Cfg<T>(GameObject go, params object[] fieldValuePairs) where T : Component
        {
            if (go == null) return;
            T c = go.GetComponent<T>();
            if (c != null) GGEditorUtil.SetMany(c, fieldValuePairs);
        }

        static T Comp<T>(GameObject go) where T : Component
        {
            return go != null ? go.GetComponent<T>() : null;
        }
    }
}
