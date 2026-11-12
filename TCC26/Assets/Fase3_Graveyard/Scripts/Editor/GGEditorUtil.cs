using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PopAdventure.Fase3.EditorTools
{
    public static class GGPaths
    {
        public const string Root = "Assets/Fase3_Graveyard";
        public const string Art = Root + "/Art";
        public const string Generated = Root + "/Generated";
        public const string Tiles = Generated + "/Tiles";
        public const string Prefabs = Generated + "/Prefabs";
        public const string Palettes = Generated + "/Palettes";
        public const string Animation = Generated + "/Animation";
        public const string Scenes = "Assets/Scenes";
        public const string SceneFile = Scenes + "/Fase3_GraveyardGroove.unity";
    }

    public static class GGEditorUtil
    {
        static Dictionary<string, Sprite> spriteCache;
        static Dictionary<string, GameObject> prefabCache;

        public static void ClearCaches()
        {
            spriteCache = null;
            prefabCache = null;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Carrega um sprite pelo nome do arquivo (sem extensão) em qualquer subpasta de Art.</summary>
        public static Sprite LoadSprite(string name)
        {
            if (spriteCache == null)
            {
                spriteCache = new Dictionary<string, Sprite>();
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { GGPaths.Art });
                for (int i = 0; i < guids.Length; i++)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (s != null) spriteCache[Path.GetFileNameWithoutExtension(p)] = s;
                }
            }
            Sprite found;
            if (spriteCache.TryGetValue(name, out found)) return found;
            Debug.LogWarning("[Fase3] Sprite não encontrado: " + name);
            return null;
        }

        /// <summary>Carrega um prefab gerado pelo nome (sem extensão) em qualquer subpasta de Generated/Prefabs.</summary>
        public static GameObject LoadPrefab(string name)
        {
            if (prefabCache == null)
            {
                prefabCache = new Dictionary<string, GameObject>();
                if (AssetDatabase.IsValidFolder(GGPaths.Prefabs))
                {
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { GGPaths.Prefabs });
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                        if (go != null) prefabCache[Path.GetFileNameWithoutExtension(p)] = go;
                    }
                }
            }
            GameObject found;
            if (prefabCache.TryGetValue(name, out found)) return found;
            Debug.LogWarning("[Fase3] Prefab não encontrado: " + name + " (rode o passo 1 do construtor).");
            return null;
        }

        // ---------------- SerializedObject helpers (campos privados [SerializeField]) ----------------
        public static void Set(Object target, string field, object value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning("[Fase3] Campo não encontrado: " + target.GetType().Name + "." + field);
                return;
            }
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: p.intValue = Convert.ToInt32(value); break;
                case SerializedPropertyType.Float: p.floatValue = Convert.ToSingle(value); break;
                case SerializedPropertyType.Boolean: p.boolValue = Convert.ToBoolean(value); break;
                case SerializedPropertyType.String: p.stringValue = (string)value; break;
                case SerializedPropertyType.Enum: p.enumValueIndex = Convert.ToInt32(value); break;
                case SerializedPropertyType.ObjectReference: p.objectReferenceValue = (Object)value; break;
                case SerializedPropertyType.Vector2: p.vector2Value = (Vector2)value; break;
                case SerializedPropertyType.Vector3: p.vector3Value = (Vector3)value; break;
                case SerializedPropertyType.Color: p.colorValue = (Color)value; break;
                case SerializedPropertyType.Rect: p.rectValue = (Rect)value; break;
                default:
                    Debug.LogWarning("[Fase3] Tipo não suportado em Set: " + p.propertyType + " (" + field + ")");
                    break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null || !p.isArray)
            {
                Debug.LogWarning("[Fase3] Array não encontrado: " + target.GetType().Name + "." + field);
                return;
            }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        // ---------------- criação de objetos ----------------
        public static GameObject NewChild(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public enum Anchor { Center, BottomOfCell, FeetAtRoot, SurfaceTop }

        /// <summary>
        /// Cria um filho com SpriteRenderer. Convenção dos prefabs da fase: a RAIZ fica no centro de uma célula;
        /// "BottomOfCell" alinha a base do sprite com a base da célula (-0.5). "FeetAtRoot" alinha a base na raiz.
        /// Todos os sprites usam pivô central.
        /// </summary>
        public static SpriteRenderer AddSprite(Transform parent, string objName, string spriteName,
            float fitHeight, float fitWidth, Anchor anchor, int order, out Vector2 sizeUnits, float surfaceFrac = 0f)
        {
            Sprite sp = LoadSprite(spriteName);
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = order;
            sizeUnits = Vector2.one;
            if (sp != null)
            {
                Vector2 natural = new Vector2(sp.rect.width, sp.rect.height) / sp.pixelsPerUnit;
                float scale = 1f;
                if (fitHeight > 0f) scale = fitHeight / natural.y;
                else if (fitWidth > 0f) scale = fitWidth / natural.x;
                go.transform.localScale = new Vector3(scale, scale, 1f);
                sizeUnits = natural * scale;
                float y = 0f;
                if (anchor == Anchor.BottomOfCell) y = -0.5f + sizeUnits.y * 0.5f;
                else if (anchor == Anchor.FeetAtRoot) y = sizeUnits.y * 0.5f;
                else if (anchor == Anchor.SurfaceTop) y = 0.5f - sizeUnits.y * (0.5f - surfaceFrac); // topo da laje em root.y + 0.5
                go.transform.localPosition = new Vector3(0f, y, 0f);
            }
            return sr;
        }

        public static BoxCollider2D AddBox(GameObject go, Vector2 size, Vector2 offset, bool trigger)
        {
            BoxCollider2D c = go.AddComponent<BoxCollider2D>();
            c.size = size;
            c.offset = offset;
            c.isTrigger = trigger;
            return c;
        }

        public static CircleCollider2D AddCircle(GameObject go, float radius, Vector2 offset, bool trigger)
        {
            CircleCollider2D c = go.AddComponent<CircleCollider2D>();
            c.radius = radius;
            c.offset = offset;
            c.isTrigger = trigger;
            return c;
        }

        public static Rigidbody2D AddKinematic(GameObject go)
        {
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            return rb;
        }


        public static SpriteRenderer AddSprite(Transform parent, string objName, string spriteName,
            float fitHeight, float fitWidth, Anchor anchor, int order)
        {
            Vector2 size;
            return AddSprite(parent, objName, spriteName, fitHeight, fitWidth, anchor, order, out size);
        }

        public static void SetMany(Object target, params object[] fieldValuePairs)
        {
            for (int i = 0; i + 1 < fieldValuePairs.Length; i += 2)
                Set(target, (string)fieldValuePairs[i], fieldValuePairs[i + 1]);
        }

        /// <summary>Procura um tipo (inclusive internal) pelo nome simples em todos os assemblies carregados.</summary>
        public static Type FindScriptableType(string simpleName)
        {
            System.Reflection.Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < asms.Length; a++)
            {
                Type[] types;
                try { types = asms[a].GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types; }
                catch (Exception) { continue; }
                if (types == null) continue;
                for (int i = 0; i < types.Length; i++)
                {
                    Type t = types[i];
                    if (t != null && t.Name == simpleName && typeof(ScriptableObject).IsAssignableFrom(t)) return t;
                }
            }
            return null;
        }

        public static void RecordInstance(GameObject go)
        {
            if (go != null && PrefabUtility.IsPartOfPrefabInstance(go))
                PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }

        public static GameObject SavePrefab(GameObject temp, string folder, string name)
        {
            EnsureFolder(folder);
            string path = folder + "/" + name + ".prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return saved;
        }
    }
}
