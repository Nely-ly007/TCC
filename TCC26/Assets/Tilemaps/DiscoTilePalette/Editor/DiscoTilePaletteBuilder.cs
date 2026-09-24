#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

// A pasta raiz e detectada sozinha (pasta pai de "Editor"), pode estar em qualquer lugar do projeto.
// Menu: Tools > Disco Tile Palette > 1. Construir Tudo
public static class DiscoTilePaletteBuilder
{
    const string PaletteName = "DiscoPalette_Auto";
    const int    PPU         = 64;   // 1 celula = 64 px = 1 unidade

    static string Root, SpritesDir, TilesDir, PaletteDir;

    static bool FindRoot()
    {
        foreach (var g in AssetDatabase.FindAssets("DiscoTilePaletteBuilder t:MonoScript"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) != "DiscoTilePaletteBuilder") continue;
            string editorDir = Path.GetDirectoryName(p).Replace("\\", "/");
            Root       = Path.GetDirectoryName(editorDir).Replace("\\", "/");
            SpritesDir = Root + "/Sprites";
            TilesDir   = Root + "/Tiles";
            PaletteDir = Root + "/Palette";
            if (!AssetDatabase.IsValidFolder(SpritesDir))
            {
                Debug.LogError("[DiscoTilePalette] Pasta nao encontrada: " + SpritesDir + ". A pasta 'Sprites' precisa ficar ao lado de 'Editor'.");
                return false;
            }
            Debug.Log("[DiscoTilePalette] Raiz detectada: " + Root);
            return true;
        }
        Debug.LogError("[DiscoTilePalette] Nao achei o script DiscoTilePaletteBuilder.cs no projeto.");
        return false;
    }

    [MenuItem("Tools/Disco Tile Palette/1. Construir Tudo")]
    public static void BuildAll()
    {
        if (!FindRoot()) return;
        int sprites = ImportSpritesInternal();
        if (sprites == 0) { Debug.LogError("[DiscoTilePalette] Nenhum PNG encontrado em " + SpritesDir); return; }
        int tiles = CreateTiles();
        int placed = CreatePalette();
        Debug.Log("[DiscoTilePalette] PNGs: " + sprites + " | Tiles criados: " + tiles + " | Tiles na paleta: " + placed
                  + ". Abra Window > 2D > Tile Palette e escolha '" + PaletteName + "'.");
    }

    [MenuItem("Tools/Disco Tile Palette/2. So reimportar sprites")]
    public static void ImportSprites()
    {
        if (FindRoot()) ImportSpritesInternal();
    }

    static int ImportSpritesInternal()
    {
        int n = 0;
        foreach (var path in GetPngs())
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            ReadPngSize(path, out int w, out int h);
            int cellsW = Mathf.Max(1, w / PPU);
            int cellsH = Mathf.Max(1, h / PPU);

            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = PPU;
            imp.filterMode          = FilterMode.Bilinear;
            imp.wrapMode            = TextureWrapMode.Clamp;
            imp.mipmapEnabled       = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize      = 2048;

            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType  = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot     = new Vector2(0.5f / cellsW, 0.5f / cellsH);
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
            n++;
        }
        return n;
    }

    static int CreateTiles()
    {
        int n = 0;
        foreach (var path in GetPngs())
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) { Debug.LogWarning("[DiscoTilePalette] Sem sprite em: " + path); continue; }

            string cat = Path.GetFileName(Path.GetDirectoryName(path));
            string dir = TilesDir + "/" + cat;
            EnsureFolder(dir);
            string tilePath = dir + "/" + Path.GetFileNameWithoutExtension(path) + ".asset";

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            tile.color  = Color.white;
            tile.colliderType = ColliderFor(cat);
            EditorUtility.SetDirty(tile);
            n++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return n;
    }

    static Tile.ColliderType ColliderFor(string cat)
    {
        if (cat.StartsWith("01_") || cat.StartsWith("02_")) return Tile.ColliderType.Grid;
        if (cat.StartsWith("06_") || cat.StartsWith("07_") || cat.StartsWith("08_")) return Tile.ColliderType.Sprite;
        return Tile.ColliderType.None;
    }

    static int CreatePalette()
    {
        EnsureFolder(PaletteDir);
        string prefabPath = PaletteDir + "/" + PaletteName + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            var go = GridPaletteUtility.CreateNewPalette(PaletteDir, PaletteName,
                GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic,
                new Vector3(1, 1, 0),
                GridLayout.CellSwizzle.XYZ);
            if (go != null) prefabPath = AssetDatabase.GetAssetPath(go);
        }
        return FillPalette(prefabPath);
    }

    [MenuItem("Tools/Disco Tile Palette/3. Preencher paleta selecionada")]
    public static void FillSelectedPalette()
    {
        if (!FindRoot()) return;
        string p = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(p) || !p.EndsWith(".prefab"))
        {
            Debug.LogError("[DiscoTilePalette] Selecione o prefab da paleta no Project.");
            return;
        }
        int n = FillPalette(p);
        Debug.Log("[DiscoTilePalette] Tiles colocados: " + n);
    }

    // Uma linha por categoria, de cima para baixo, 1 celula de espaco entre itens.
    static int FillPalette(string prefabPath)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        var map  = root.GetComponentInChildren<Tilemap>();
        if (map == null)
        {
            Debug.LogError("[DiscoTilePalette] Nenhum Tilemap dentro de " + prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return 0;
        }
        map.ClearAllTiles();

        var cats = new List<string>(AssetDatabase.GetSubFolders(TilesDir));
        cats.Sort();

        int cursorY = 0, total = 0;
        foreach (var catDir in cats)
        {
            var paths = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Tile", new[] { catDir }))
                paths.Add(AssetDatabase.GUIDToAssetPath(g));
            paths.Sort();

            int rowH = 1, x = 0;
            var placed = new List<(Tile t, int w, int h)>();
            foreach (var tp in paths)
            {
                var t = AssetDatabase.LoadAssetAtPath<Tile>(tp);
                if (t == null || t.sprite == null) continue;
                int w = Mathf.Max(1, Mathf.RoundToInt(t.sprite.rect.width  / PPU));
                int h = Mathf.Max(1, Mathf.RoundToInt(t.sprite.rect.height / PPU));
                rowH = Mathf.Max(rowH, h);
                placed.Add((t, w, h));
            }

            int baseY = cursorY - rowH;
            foreach (var p in placed)
            {
                map.SetTile(new Vector3Int(x, baseY, 0), p.t);
                x += p.w + 1;
                total++;
            }
            cursorY = baseY - 1;
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return total;
    }

    // ---- helpers ----
    static string[] GetPngs()
    {
        var list = new List<string>();
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith(".png")) list.Add(p);
        }
        list.Sort();
        return list.ToArray();
    }

    static void ReadPngSize(string assetPath, out int w, out int h)
    {
        var b = File.ReadAllBytes(assetPath);
        w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
