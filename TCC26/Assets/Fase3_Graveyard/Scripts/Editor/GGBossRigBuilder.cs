using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PopAdventure.Fase3.EditorTools
{
    /// <summary>
    /// Monta o boss com as PEÇAS do zumbi (Art/BossRig/gboss_*.png) e liga a animação automática (GGBossRig).
    ///
    /// Menu: Pop Adventure > Fase 3 > Recriar Boss com Rig Procedural
    ///   • configura o importador das 12 peças (PPU 530, pivô = articulação de cada peça);
    ///   • cria o prefab Boss_Graveyard (se não existir) ou ATUALIZA o existente no mesmo arquivo — a instância na cena
    ///     e os campos que você já ajustou no GGBoss (limites, prefabs, vida...) são mantidos;
    ///   • troca o filho "Visual" antigo (placeholder + Animator) pelo novo Visual com o rig.
    /// </summary>
    public static class GGBossRigBuilder
    {
        const string ArtFolder = GGPaths.Art + "/" + GGBossRigLayout.ArtFolderName;

        [MenuItem("Pop Adventure/Fase 3/Recriar Boss com Rig Procedural")]
        public static void MenuRebuild()
        {
            string path = BossPath();
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            if (exists)
            {
                bool ok = EditorUtility.DisplayDialog("Boss com rig procedural",
                    "Isto vai trocar o filho 'Visual' do prefab\n" + path + "\npelo rig automático (peças do zumbi).\n\n"
                    + "O Animator/sprite antigo do Visual será removido. Os campos do GGBoss (limites, prefabs, vida) e a instância na cena são mantidos.\n\nContinuar?",
                    "Continuar", "Cancelar");
                if (!ok) return;
            }
            try { Build(true); }
            catch (System.Exception e) { Debug.LogError("[Fase3] Erro ao montar o boss: " + e); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        public static string BossPath()
        {
            return GGPrefabFactory.DirBoss + "/" + GGPrefabFactory.BossPrefabName + ".prefab";
        }

        // ------------------------------------------------------------------ importador das peças
        /// <summary>Configura as 12 peças como sprites com pivô na articulação. Devolve false se faltar alguma.</summary>
        public static bool ConfigureImporters()
        {
            List<string> missing = new List<string>();
            GGBossRigLayout.Part[] parts = GGBossRigLayout.Parts;
            for (int i = 0; i < parts.Length; i++)
            {
                string path = ArtFolder + "/" + parts[i].file + ".png";
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) { missing.Add(parts[i].file + ".png"); continue; }

                Vector2 pivot = new Vector2(parts[i].pivotX, parts[i].pivotY);
                TextureImporterSettings s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                bool same = ti.textureType == TextureImporterType.Sprite
                            && ti.spriteImportMode == SpriteImportMode.Single
                            && Mathf.Approximately(ti.spritePixelsPerUnit, GGBossRigLayout.Ppu)
                            && s.spriteAlignment == (int)SpriteAlignment.Custom
                            && Vector2.Distance(s.spritePivot, pivot) < 0.0005f
                            && ti.filterMode == FilterMode.Bilinear
                            && !ti.mipmapEnabled
                            && ti.textureCompression == TextureImporterCompression.Uncompressed;
                if (same) continue;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = GGBossRigLayout.Ppu;
                ti.filterMode = FilterMode.Bilinear;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.maxTextureSize = 2048;

                ti.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = pivot;
                s.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(s);
                ti.SaveAndReimport();
            }

            if (missing.Count > 0)
            {
                string msg = "Faltam peças em " + ArtFolder + ":\n• " + string.Join("\n• ", missing.ToArray())
                             + "\n\nCopie a pasta Fase3_Graveyard inteira (com Art/BossRig) para dentro de Assets.";
                Debug.LogError("[Fase3] " + msg);
                EditorUtility.DisplayDialog("Boss com rig procedural", msg, "OK");
                return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ prefab
        /// <summary>Cria ou atualiza o prefab do boss. fresh = true quando chamado pelo menu (mesmo comportamento; só muda o log).</summary>
        public static bool Build(bool fresh)
        {
            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                EditorUtility.DisplayDialog("Boss com rig procedural",
                    "Pasta " + ArtFolder + " não encontrada. Copie a pasta Fase3_Graveyard inteira para dentro de Assets.", "OK");
                return false;
            }

            EditorUtility.DisplayProgressBar("Boss", "Configurando as peças...", 0.2f);
            if (!ConfigureImporters()) return false;
            AssetDatabase.Refresh();
            GGEditorUtil.ClearCaches();

            GGEditorUtil.EnsureFolder(GGPrefabFactory.DirBoss);
            string path = BossPath();
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

            EditorUtility.DisplayProgressBar("Boss", "Montando o rig...", 0.6f);
            if (exists)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ApplyRig(contents);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            else
            {
                GameObject root = NewBossRoot();
                ApplyRig(root);
                GGEditorUtil.SavePrefab(root, GGPrefabFactory.DirBoss, GGPrefabFactory.BossPrefabName);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GGEditorUtil.ClearCaches();
            Debug.Log("[Fase3] Boss com rig procedural " + (exists ? "ATUALIZADO" : "CRIADO") + ": " + path
                      + ". Teste rápido: arraste o prefab para uma cena vazia, marque 'Demo Loop' no GGBossRig e dê Play.");
            return true;
        }

        /// <summary>Raiz nova (só usada quando o prefab ainda não existe): raiz = PÉS, hurtbox, Muzzle, barra de vida e GGBoss.</summary>
        static GameObject NewBossRoot()
        {
            GameObject root = new GameObject(GGPrefabFactory.BossPrefabName);
            GGEditorUtil.AddKinematic(root);
            GGEditorUtil.AddBox(root, new Vector2(1.6f, 3.6f), new Vector2(0f, 1.8f), true); // hurtbox / dano de contato

            GameObject muzzle = GGEditorUtil.NewChild(root.transform, "Muzzle", new Vector3(MuzzleX, MuzzleY, 0f));
            GameObject barGo = GGEditorUtil.NewChild(root.transform, "HealthBar", new Vector3(0f, 4.3f, 0f));
            GGWorldHealthBar bar = barGo.AddComponent<GGWorldHealthBar>();

            GGBoss boss = root.AddComponent<GGBoss>();
            GGEditorUtil.SetMany(boss,
                "bossName", "Groove King",
                "muzzle", muzzle.transform,
                "healthBar", bar,
                "zombieHandPrefab", GGEditorUtil.LoadPrefab("ZombieHand"),
                "shockwavePrefab", GGEditorUtil.LoadPrefab("BossShockwave"),
                "orbPrefab", GGEditorUtil.LoadPrefab("BossOrb"),
                "ghostPrefab", GGEditorUtil.LoadPrefab("Enemy_Ghost"),
                "healNotePrefab", GGEditorUtil.LoadPrefab("Pickup_Note"),
                "fragmentPrefab", GGEditorUtil.LoadPrefab("Pickup_Fragment"));
            return root;
        }

        // posição das mãos no empurrão dos orbs (para frente / altura do ombro)
        const float MuzzleX = 1.9f;
        const float MuzzleY = 2.25f;

        /// <summary>Troca o Visual antigo pelo novo (hierarquia de juntas + GGBossRig + SortingGroup) e liga o GGBoss.</summary>
        static void ApplyRig(GameObject root)
        {
            // 1) remove o Visual antigo (placeholder + Animator) e qualquer rig anterior
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Transform c = root.transform.GetChild(i);
                if (c.name == "Visual" || c.GetComponentInChildren<GGBossRig>(true) != null)
                    Object.DestroyImmediate(c.gameObject);
            }

            // 2) Visual: origem nos PÉS; o GGBoss vira o scale.x dele (NÃO anime o scale.x do Visual)
            GameObject visual = GGEditorUtil.NewChild(root.transform, "Visual", Vector3.zero);
            SortingGroup sg = visual.AddComponent<SortingGroup>();
            sg.sortingOrder = 2;
            GGBossRig rig = visual.AddComponent<GGBossRig>();

            // 3) Rig (pés) -> Hips -> peças
            GameObject rigGo = GGEditorUtil.NewChild(visual.transform, "Rig", Vector3.zero);
            Vector3 hipsLocal = new Vector3(
                (GGBossRigLayout.HipsX - GGBossRigLayout.RootX) / GGBossRigLayout.Ppu,
                (GGBossRigLayout.RootY - GGBossRigLayout.HipsY) / GGBossRigLayout.Ppu, 0f);
            GameObject hips = GGEditorUtil.NewChild(rigGo.transform, "Hips", hipsLocal);

            Dictionary<string, Transform> joints = new Dictionary<string, Transform>();
            Dictionary<string, Vector2> jointPx = new Dictionary<string, Vector2>();
            joints["Hips"] = hips.transform;
            jointPx["Hips"] = new Vector2(GGBossRigLayout.HipsX, GGBossRigLayout.HipsY);

            GGBossRigLayout.Part[] parts = GGBossRigLayout.Parts;
            for (int i = 0; i < parts.Length; i++)
            {
                GGBossRigLayout.Part p = parts[i];
                Transform parent;
                Vector2 parentPx;
                if (!joints.TryGetValue(p.parent, out parent) || !jointPx.TryGetValue(p.parent, out parentPx))
                {
                    Debug.LogError("[Fase3] Peça '" + p.name + "': pai '" + p.parent + "' ainda não foi criado (ordem do layout).");
                    continue;
                }
                Vector3 local = new Vector3(
                    (p.jointX - parentPx.x) / GGBossRigLayout.Ppu,
                    -(p.jointY - parentPx.y) / GGBossRigLayout.Ppu, 0f);
                GameObject go = GGEditorUtil.NewChild(parent, p.name, local);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GGEditorUtil.LoadSprite(p.file);
                sr.sortingOrder = p.order;
                joints[p.name] = go.transform;
                jointPx[p.name] = new Vector2(p.jointX, p.jointY);
            }

            // 4) GGBossRig
            rig.AutoAssignJoints();
            GGEditorUtil.SetMany(rig,
                "artFacesLeft", true,
                "backFootDrop", GGBossRigLayout.BackFootDropPx / GGBossRigLayout.Ppu);
            EditorUtility.SetDirty(rig);

            // 5) GGBoss
            GGBoss boss = root.GetComponent<GGBoss>();
            if (boss == null) boss = root.AddComponent<GGBoss>();
            GGEditorUtil.SetMany(boss,
                "visual", visual.transform,
                "rig", rig,
                "animator", null,
                "spriteFacesRight", false);

            // Muzzle: se ainda está no valor padrão do boss antigo (1.0, 2.4), leva para a altura das mãos do rig
            Transform muzzle = root.transform.Find("Muzzle");
            if (muzzle != null && Mathf.Approximately(muzzle.localPosition.x, 1f) && Mathf.Approximately(muzzle.localPosition.y, 2.4f))
                muzzle.localPosition = new Vector3(MuzzleX, MuzzleY, 0f);

            if (root.GetComponent<Collider2D>() == null)
                GGEditorUtil.AddBox(root, new Vector2(1.6f, 3.6f), new Vector2(0f, 1.8f), true);
        }
    }
}
