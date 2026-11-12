using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PopAdventure.Fase3.EditorTools
{
    /// <summary>Importação dos sprites, criação dos Tiles e das duas Tile Palettes (Tiles e Objetos).</summary>
    public static class GGTileFactory
    {
        struct TileDef
        {
            public string asset;
            public string sprite;
            public Tile.ColliderType collider;
            public TileDef(string a, string s, Tile.ColliderType c) { asset = a; sprite = s; collider = c; }
        }

        static readonly TileDef[] Defs =
        {
            new TileDef("Ground_Top_L",    "ground_top_l",    Tile.ColliderType.Grid),
            new TileDef("Ground_Top_M",    "ground_top_m",    Tile.ColliderType.Grid),
            new TileDef("Ground_Top_R",    "ground_top_r",    Tile.ColliderType.Grid),
            new TileDef("Ground_Single",   "ground_single",   Tile.ColliderType.Grid),
            new TileDef("Wall_Stone",      "wall_stone",      Tile.ColliderType.Grid),
            new TileDef("Wall_Mossy",      "wall_mossy",      Tile.ColliderType.Grid),
            new TileDef("Wall_Cracked",    "wall_cracked",    Tile.ColliderType.Grid),
            new TileDef("Wall_Brick",      "wall_brick",      Tile.ColliderType.Grid),
            new TileDef("Stone_Block",     "stone_block",     Tile.ColliderType.Grid),
            new TileDef("Ramp_Up_Right",   "ramp_up_right",   Tile.ColliderType.Sprite),
            new TileDef("Ramp_Up_Left",    "ramp_up_left",    Tile.ColliderType.Sprite),
            new TileDef("Plat_Thin_L",     "plat_thin_l",     Tile.ColliderType.Sprite),
            new TileDef("Plat_Thin_Single","plat_thin_single",Tile.ColliderType.Sprite),
            new TileDef("Plat_Thin_R",     "plat_thin_r",     Tile.ColliderType.Sprite),
        };

        // ------------------------------------------------------------------ sprites
        static float PpuFor(string n)
        {
            if (n.StartsWith("wolf_")) return 200f;
            if (n.StartsWith("crow_")) return 240f;
            if (n.StartsWith("background_")) return 18.4f;
            if (n.StartsWith("fog_")) return 51.2f;
            if (n.StartsWith("boss_")) return 100f;
            return 140f; // tiles (1 célula = 140 px = 1 unidade), props, hazards, coletáveis
        }

        public static int ConfigureSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { GGPaths.Art });
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (ConfigureOne(path)) changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
            GGEditorUtil.ClearCaches();
            return changed;
        }

        static bool ConfigureOne(string path)
        {
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return false;

            string n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            float ppu = PpuFor(n);
            bool isTile = path.Contains("/Tiles/");

            TextureImporterSettings s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            SpriteMeshType mesh = isTile ? SpriteMeshType.FullRect : SpriteMeshType.Tight;

            bool same = ti.textureType == TextureImporterType.Sprite
                        && ti.spriteImportMode == SpriteImportMode.Single
                        && Mathf.Approximately(ti.spritePixelsPerUnit, ppu)
                        && ti.filterMode == FilterMode.Bilinear
                        && !ti.mipmapEnabled
                        && s.spriteAlignment == (int)SpriteAlignment.Center
                        && s.spriteMeshType == mesh
                        && ti.textureCompression == TextureImporterCompression.Uncompressed;
            if (same) return false;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.npotScale = TextureImporterNPOTScale.None;

            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteMeshType = mesh;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
            return true;
        }

        // ------------------------------------------------------------------ tiles
        public static Dictionary<string, Tile> CreateTiles()
        {
            GGEditorUtil.EnsureFolder(GGPaths.Tiles);
            Dictionary<string, Tile> map = new Dictionary<string, Tile>();
            for (int i = 0; i < Defs.Length; i++)
            {
                TileDef d = Defs[i];
                string path = GGPaths.Tiles + "/" + d.asset + ".asset";
                Tile t = AssetDatabase.LoadAssetAtPath<Tile>(path);
                bool isNew = t == null;
                if (isNew) t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = GGEditorUtil.LoadSprite(d.sprite);
                t.colliderType = d.collider;
                t.color = Color.white;
                if (isNew) AssetDatabase.CreateAsset(t, path);
                else EditorUtility.SetDirty(t);
                map[d.asset] = t;
            }
            AssetDatabase.SaveAssets();
            return map;
        }

        public static Tile LoadTile(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<Tile>(GGPaths.Tiles + "/" + assetName + ".asset");
        }

        // ------------------------------------------------------------------ palettes
        static GameObject NewPaletteRoot(string name, out Transform layer)
        {
            GameObject root = new GameObject(name);
            Grid grid = root.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
            GameObject l = new GameObject("Layer1");
            l.transform.SetParent(root.transform, false);
            l.AddComponent<Tilemap>();
            l.AddComponent<TilemapRenderer>();
            layer = l.transform;
            return root;
        }

        static void SavePalette(GameObject root, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            EnsureGridPalette(path);
        }

        /// <summary>
        /// Uma Tile Palette é um prefab com um sub-asset "GridPalette". O tipo é internal no Unity,
        /// então é criado por reflexão. Se falhar, os Tiles continuam prontos para arrastar para uma paleta manual.
        /// </summary>
        static void EnsureGridPalette(string path)
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].GetType().Name == "GridPalette") return;

            System.Type t = GGEditorUtil.FindScriptableType("GridPalette");
            if (t == null)
            {
                Debug.LogWarning("[Fase3] Tipo GridPalette não encontrado nesta versão do Unity. Crie a paleta manualmente "
                                 + "(Window > 2D > Tile Palette > Create New Palette) e arraste os assets de " + GGPaths.Tiles + ".");
                return;
            }

            ScriptableObject gp = ScriptableObject.CreateInstance(t);
            gp.name = "Palette Settings";
            SerializedObject so = new SerializedObject(gp);
            SerializedProperty it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyType == SerializedPropertyType.Enum && it.name.ToLowerInvariant().Contains("cellsizing"))
                    it.enumValueIndex = 1; // Manual
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.AddObjectToAsset(gp, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
        }

        public static void CreateTilePalette(Dictionary<string, Tile> tiles)
        {
            GGEditorUtil.EnsureFolder(GGPaths.Palettes);
            Transform layer;
            GameObject root = NewPaletteRoot("Graveyard_Tiles", out layer);
            Tilemap tm = layer.GetComponent<Tilemap>();

            string[][] rows =
            {
                new[] { "Ground_Top_L", "Ground_Top_M", "Ground_Top_R", "Ground_Single" },
                new[] { "Wall_Stone", "Wall_Mossy", "Wall_Cracked", "Wall_Brick" },
                new[] { "Stone_Block", "Ramp_Up_Right", "Ramp_Up_Left" },
                new[] { "Plat_Thin_L", "Plat_Thin_Single", "Plat_Thin_R" },
            };
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    Tile t;
                    if (tiles.TryGetValue(rows[y][x], out t))
                        tm.SetTile(new Vector3Int(x, -y, 0), t);
                }

            SavePalette(root, GGPaths.Palettes + "/Graveyard_Tiles.prefab");
        }

        public struct PaletteEntry
        {
            public string prefabName;
            public int group; // linhas novas a cada mudança de grupo
            public PaletteEntry(string n, int g) { prefabName = n; group = g; }
        }

        /// <summary>
        /// Paleta de objetos (use com o GameObject Brush). Cada prefab fica no CENTRO de uma célula; clique na célula da BASE do objeto.
        /// </summary>
        public static void CreateObjectsPalette(List<PaletteEntry> entries)
        {
            GGEditorUtil.EnsureFolder(GGPaths.Palettes);
            Transform layer;
            GameObject root = NewPaletteRoot("Graveyard_Objects", out layer);

            const int perRow = 8;
            const int stepX = 6;
            const int stepY = 8;
            int row = 0, col = 0, lastGroup = int.MinValue;
            for (int i = 0; i < entries.Count; i++)
            {
                PaletteEntry e = entries[i];
                if (e.group != lastGroup)
                {
                    if (lastGroup != int.MinValue && col != 0) row++;
                    col = 0;
                    lastGroup = e.group;
                }
                GameObject prefab = GGEditorUtil.LoadPrefab(e.prefabName);
                if (prefab == null) continue;

                GameObject inst = PrefabUtility.InstantiatePrefab(prefab, layer) as GameObject;
                if (inst == null) continue;
                int cx = col * stepX;
                int cy = -row * stepY;
                inst.transform.localPosition = new Vector3(cx + 0.5f, cy + 0.5f, 0f);

                col++;
                if (col >= perRow) { col = 0; row++; }
            }

            SavePalette(root, GGPaths.Palettes + "/Graveyard_Objects.prefab");
        }
    }
}
