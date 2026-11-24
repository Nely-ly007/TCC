using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PopAdventure.Fase3.EditorTools
{
    /// <summary>
    /// Deixa as plataformas rítmicas mais LENTAS sem reconstruir a fase.
    /// Converte (cena aberta + prefabs Platform_Glowing_*) toda GGRhythmPlatform que ainda está no ciclo antigo (Period Beats = 4):
    ///   Period Beats 4 → 8,  On Beats → 6,  Offset Beats: Blink × 2 (0,1,2,3 → 0,2,4,6), Rise × 1,5 (0,2 → 0,3).
    /// Plataformas que você já ajustou (Period Beats diferente de 4) não são tocadas. Pode rodar quantas vezes quiser.
    /// </summary>
    public static class GGRhythmTuner
    {
        [MenuItem("Pop Adventure/Fase 3/Deixar plataformas rítmicas mais lentas")]
        public static void Run()
        {
            int prefabs = 0, scene = 0;

            // 1) prefabs gerados
            if (AssetDatabase.IsValidFolder(GGPaths.Prefabs))
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { GGPaths.Prefabs });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null) continue;
                    GGRhythmPlatform rp = go.GetComponent<GGRhythmPlatform>();
                    if (rp != null && Convert(rp, true)) { prefabs++; EditorUtility.SetDirty(rp); }
                }
                AssetDatabase.SaveAssets();
            }

            // 2) instâncias nas cenas abertas
            GGRhythmPlatform[] all = Resources.FindObjectsOfTypeAll<GGRhythmPlatform>();
            for (int i = 0; i < all.Length; i++)
            {
                GGRhythmPlatform rp = all[i];
                if (rp == null || EditorUtility.IsPersistent(rp)) continue;      // ignora assets (já tratados acima)
                Scene sc = rp.gameObject.scene;
                if (!sc.IsValid() || !sc.isLoaded) continue;
                if (Convert(rp, false)) { scene++; EditorSceneManager.MarkSceneDirty(sc); }
            }

            string msg = "Plataformas rítmicas convertidas: " + scene + " na cena, " + prefabs + " prefab(s).\n\n"
                         + "Agora: Period Beats 8, On Beats 6 (3,2 s presentes, 1 s ausentes). Salve a cena (Ctrl+S).";
            if (scene == 0 && prefabs == 0)
                msg = "Nada para converter: nenhuma plataforma com Period Beats = 4 (a cena precisa estar aberta).";
            Debug.Log("[Fase3] " + msg.Replace("\n\n", " "));
            EditorUtility.DisplayDialog("Plataformas rítmicas", msg, "OK");
        }

        static bool Convert(GGRhythmPlatform rp, bool prefabAsset)
        {
            SerializedObject so = new SerializedObject(rp);
            SerializedProperty period = so.FindProperty("periodBeats");
            SerializedProperty on = so.FindProperty("onBeats");
            SerializedProperty offset = so.FindProperty("offsetBeats");
            SerializedProperty mode = so.FindProperty("mode");
            if (period == null || on == null || offset == null || mode == null) return false;
            if (period.intValue != 4) return false;

            bool rise = mode.enumValueIndex == 1;
            period.intValue = 8;
            on.intValue = 6;
            offset.floatValue = rise ? Mathf.Round(offset.floatValue * 1.5f) : offset.floatValue * 2f;
            if (prefabAsset) so.ApplyModifiedPropertiesWithoutUndo();
            else so.ApplyModifiedProperties();
            return true;
        }
    }
}
