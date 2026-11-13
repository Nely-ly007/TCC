using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PopAdventure.MayhemTheatre.EditorTools
{
    [Serializable]
    public class MTSpriteEntry
    {
        public string path;
        public float ppu = 64f;
        public float px = 0.5f;
        public float py = 0.5f;
        public bool tile;
    }

    [Serializable]
    public class MTSpriteManifest
    {
        public MTSpriteEntry[] items;
    }

    public static class MTPaths
    {
        public const string Root = "Assets/MayhemTheatre";
        public const string Sprites = Root + "/Sprites";
        public const string Tiles = Root + "/Tiles";
        public const string Prefabs = Root + "/Prefabs";
        public const string UI = Root + "/UI";
    }

    /// <summary>Configura os PNGs como Sprites (PPU/pivô) e cria os Tile assets. Menu: POP ADVENTURE ▸ Mayhem Theatre ▸ 1.</summary>
    public static class MTAssetSetup
    {
        [MenuItem("POP ADVENTURE/Mayhem Theatre/1 - Configurar sprites e Tiles")]
        static void Menu()
        {
            Run(true);
        }

        public static Sprite LoadSprite(string category, string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(MTPaths.Sprites + "/" + category + "/" + name + ".png");
        }

        public static TileBase LoadTile(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TileBase>(MTPaths.Tiles + "/" + name + ".asset");
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static bool Run(bool verbose)
        {
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(MTPaths.Sprites + "/MT_SpriteManifest.json");
            if (ta == null)
            {
                EditorUtility.DisplayDialog("Mayhem Theatre",
                    "Não achei " + MTPaths.Sprites + "/MT_SpriteManifest.json.\nColoque a pasta MayhemTheatre dentro de Assets/ (sem renomear).", "OK");
                return false;
            }

            MTSpriteManifest man = JsonUtility.FromJson<MTSpriteManifest>(ta.text);
            if (man == null || man.items == null) return false;

            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < man.items.Length; i++)
                    if (ConfigureTexture(man.items[i])) changed++;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();

            EnsureFolder(MTPaths.Tiles);
            int tiles = 0;
            for (int i = 0; i < man.items.Length; i++)
                if (man.items[i].tile) tiles += MakeTile(man.items[i]);
            AssetDatabase.SaveAssets();

            if (verbose)
                Debug.Log("[Mayhem Theatre] Sprites configurados: " + changed + " | Tiles criados: " + tiles +
                          " (pasta " + MTPaths.Tiles + "). Arraste essa pasta para uma Tile Palette nova.");
            return true;
        }

        static bool ConfigureTexture(MTSpriteEntry e)
        {
            string path = MTPaths.Sprites + "/" + e.path;
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return false;

            Vector2 pivot = new Vector2(e.px, e.py);
            TextureImporterSettings s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);

            bool ok = ti.textureType == TextureImporterType.Sprite
                      && ti.spriteImportMode == SpriteImportMode.Single
                      && Mathf.Approximately(ti.spritePixelsPerUnit, e.ppu)
                      && ti.filterMode == FilterMode.Point
                      && s.spriteAlignment == (int)SpriteAlignment.Custom
                      && (s.spritePivot - pivot).sqrMagnitude < 0.0001f;
            if (ok) return false;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = e.ppu;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = pivot;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spritePixelsPerUnit = e.ppu;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
            return true;
        }

        static int MakeTile(MTSpriteEntry e)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(MTPaths.Sprites + "/" + e.path);
            if (sp == null) return 0;

            string name = Path.GetFileNameWithoutExtension(e.path);
            string tp = MTPaths.Tiles + "/" + name + ".asset";
            Tile t = AssetDatabase.LoadAssetAtPath<Tile>(tp);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = sp;
                t.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(t, tp);
                return 1;
            }
            if (t.sprite != sp)
            {
                t.sprite = sp;
                EditorUtility.SetDirty(t);
            }
            return 0;
        }
    }
}
