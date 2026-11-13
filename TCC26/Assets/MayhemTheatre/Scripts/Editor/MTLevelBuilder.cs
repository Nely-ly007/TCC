using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace PopAdventure.MayhemTheatre.EditorTools
{
    /// <summary>
    /// Monta a Fase 4 inteira na cena atual: 5 seções (Fase1, Fase2, Fase3, Fase4, Mix Harmônico), Tilemaps, plataformas no ritmo,
    /// inimigos, hazards, pickups, checkpoints, arena e boss (prefab). Menu: POP ADVENTURE ▸ Mayhem Theatre ▸ 2.
    /// </summary>
    public static class MTLevelBuilder
    {
        const string RootName = "MAYHEM_THEATRE_FASE4";

        static readonly string[] SectionNames =
        {
            "1_Fase1_DiscoFever", "2_Fase2_TheHive", "3_Fase3_GraveyardGroove", "4_Fase4_MayhemTheatre", "5_MixHarmonico_BossArena"
        };

        static readonly Color Pink = new Color(1f, 0.45f, 0.78f, 1f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f, 1f);
        static readonly Color Green = new Color(0.5f, 1f, 0.6f, 1f);
        static readonly Color Crimson = new Color(0.9f, 0.15f, 0.25f, 1f);
        static readonly Color Dim = new Color(0.62f, 0.55f, 0.68f, 1f);

        class Sec
        {
            public MTMoment moment;
            public int x0, x1;
            public Transform root, decor, dyn, hazards, enemies, pickups, triggers;
            public Tilemap bg, ground, plat, fg;
            public MTSectionSkin skin;
        }

        static GameObject vinylPrefab;
        static int missingSprites;

        // ================================================================== menus

        [MenuItem("POP ADVENTURE/Mayhem Theatre/2 - Criar Fase 4 na cena atual")]
        public static void Build()
        {
            if (!MTAssetSetup.Run(false)) return;

            GameObject old = FindRoot();
            if (old != null)
            {
                if (!EditorUtility.DisplayDialog("Mayhem Theatre",
                        "Já existe a Fase 4 nesta cena. Apagar e recriar?\n(alterações feitas à mão dentro dela serão perdidas)", "Recriar", "Cancelar"))
                    return;
                Undo.DestroyObjectImmediate(old);
            }

            missingSprites = 0;
            vinylPrefab = FindUserPrefab(new[] { "vinyl", "vinil", "vinis" }, new[] { "player", "toca", "turntable", "tocadisco" });

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Criar Fase 4 (Mayhem Theatre)");
            MTLevelController lc = root.AddComponent<MTLevelController>();

            // ---------- managers (relógio de batidas + música)
            GameObject mg = Obj(root.transform, "Managers", Vector2.zero);
            mg.AddComponent<MTBeatClock>();
            MTMusicDirector md = mg.AddComponent<MTMusicDirector>();
            md.moments = MakeMoments();

            // ---------- spawn
            GameObject spawn = Obj(root.transform, "PlayerSpawn", new Vector2(2f, 0.6f));
            lc.playerSpawn = spawn.transform;
            lc.startMoment = MTMoment.Fase1;

            // ---------- seções
            Sec[] secs = new Sec[5];
            for (int i = 0; i < 5; i++) secs[i] = CreateSection(root.transform, (MTMoment)i, i);

            BuildFase1(secs[0]);
            BuildFase2(secs[1]);
            BuildFase3(secs[2]);
            BuildFase4(secs[3]);
            BuildArena(secs[4], lc);

            for (int i = 0; i < 5; i++) FillSection(secs[i]);

            BuildBoundaries(root.transform);
            BuildCameraBounds(root.transform);
            WirePlayer();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            Debug.Log(Checklist());
        }

        [MenuItem("POP ADVENTURE/Mayhem Theatre/3 - Reaplicar tiles de todas as seções")]
        public static void ReapplyAll()
        {
            MTSectionSkin[] all = MTUtil.FindAll<MTSectionSkin>();
            if (all.Length == 0)
            {
                EditorUtility.DisplayDialog("Mayhem Theatre", "Nenhuma seção encontrada. Rode antes o menu 2 (Criar Fase 4).", "OK");
                return;
            }
            for (int i = 0; i < all.Length; i++) ReapplySection(all[i]);
            Debug.Log("[Mayhem Theatre] Tiles reaplicados em " + all.Length + " seções.");
        }

        [MenuItem("POP ADVENTURE/Mayhem Theatre/Remover Fase 4 da cena")]
        public static void Remove()
        {
            GameObject root = FindRoot();
            if (root == null)
            {
                EditorUtility.DisplayDialog("Mayhem Theatre", "A Fase 4 não está nesta cena.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Mayhem Theatre", "Remover " + RootName + " da cena?", "Remover", "Cancelar")) return;
            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // ================================================================== reaplicar tiles

        /// <summary>Preenche os Tilemaps de uma seção (chamado pelo botão do MTSectionSkin).</summary>
        public static void ReapplySection(MTSectionSkin skin)
        {
            if (skin == null) return;
            Sec s = SecFromSkin(skin);
            if (s == null)
            {
                Debug.LogWarning("[Mayhem Theatre] Esta seção não tem Grid/Ground/Platforms. Use o menu 2 para criar a fase.");
                return;
            }
            List<UnityEngine.Object> undoList = new List<UnityEngine.Object>();
            if (s.bg != null) undoList.Add(s.bg);
            if (s.ground != null) undoList.Add(s.ground);
            if (s.plat != null) undoList.Add(s.plat);
            Undo.RegisterCompleteObjectUndo(undoList.ToArray(), "Reaplicar tiles");
            FillSection(s);

            // plataformas dinâmicas (no ritmo / móveis) acompanham o tile de plataforma da seção
            TileBase pt = skin.platformTile;
            Tile t = pt as Tile;
            if (t != null && t.sprite != null && s.dyn != null)
            {
                SpriteRenderer[] rs = s.dyn.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < rs.Length; i++)
                {
                    Undo.RecordObject(rs[i], "Reaplicar tiles");
                    rs[i].sprite = t.sprite;
                    rs[i].color = Color.white;
                }
            }
            EditorSceneManager.MarkSceneDirty(skin.gameObject.scene);
        }

        static Sec SecFromSkin(MTSectionSkin skin)
        {
            Transform grid = skin.transform.Find("Grid");
            if (grid == null) return null;
            Sec s = new Sec();
            s.skin = skin;
            s.moment = skin.moment;
            s.root = skin.transform;
            s.dyn = skin.transform.Find("DynamicPlatforms");
            s.bg = TM(grid, "Background");
            s.ground = TM(grid, "Ground");
            s.plat = TM(grid, "Platforms");
            s.fg = TM(grid, "Foreground");
            if (s.ground == null || s.plat == null) return null;
            return s;
        }

        static Tilemap TM(Transform grid, string name)
        {
            Transform t = grid.Find(name);
            return t != null ? t.GetComponent<Tilemap>() : null;
        }

        // ================================================================== estrutura

        static Sec CreateSection(Transform parent, MTMoment m, int index)
        {
            Sec s = new Sec();
            s.moment = m;
            s.x0 = MTLevelLayout.Starts[index];
            s.x1 = MTLevelLayout.Ends[index];

            GameObject rootGo = Obj(parent, SectionNames[index], Vector2.zero);
            s.root = rootGo.transform;
            s.skin = rootGo.AddComponent<MTSectionSkin>();
            s.skin.moment = m;
            s.skin.paletteNote = "Arraste aqui os Tiles da Tile Palette da " + FaseLabel(m) + " e clique em 'Reaplicar tiles desta seção'.";

            GameObject gridGo = Obj(s.root, "Grid", Vector2.zero);
            Grid grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            s.bg = MakeTilemap(gridGo.transform, "Background", -2, false, false);
            s.ground = MakeTilemap(gridGo.transform, "Ground", 0, true, false);
            s.plat = MakeTilemap(gridGo.transform, "Platforms", 1, true, true);
            s.fg = MakeTilemap(gridGo.transform, "Foreground", 5, false, false);

            s.decor = Obj(s.root, "Decor", Vector2.zero).transform;
            s.dyn = Obj(s.root, "DynamicPlatforms", Vector2.zero).transform;
            s.hazards = Obj(s.root, "Hazards", Vector2.zero).transform;
            s.enemies = Obj(s.root, "Enemies", Vector2.zero).transform;
            s.pickups = Obj(s.root, "Pickups", Vector2.zero).transform;
            s.triggers = Obj(s.root, "Triggers", Vector2.zero).transform;

            // zona da seção (toca a música e salva checkpoint)
            float w = s.x1 - s.x0;
            GameObject zone = Obj(s.triggers, "Zone_" + m, new Vector2(s.x0 + w * 0.5f, 4f));
            BoxCollider2D bc = zone.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(w, 30f);
            MTSectionZone z = zone.AddComponent<MTSectionZone>();
            z.moment = m;
            z.makeCheckpoint = true;
            GameObject cp = Obj(zone.transform, "CheckpointPoint", new Vector2(s.x0 + 2f, 0.6f));
            z.checkpointPoint = cp.transform;
            return s;
        }

        static Tilemap MakeTilemap(Transform grid, string name, int order, bool solid, bool oneWay)
        {
            GameObject go = Obj(grid, name, Vector2.zero);
            Tilemap tm = go.AddComponent<Tilemap>();
            TilemapRenderer tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = order;

            if (solid)
            {
                Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;

                TilemapCollider2D tc = go.AddComponent<TilemapCollider2D>();
#if UNITY_2023_1_OR_NEWER
                tc.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
                tc.usedByComposite = true;
#endif
                CompositeCollider2D cc = go.AddComponent<CompositeCollider2D>();
                cc.geometryType = CompositeCollider2D.GeometryType.Polygons;

                if (oneWay)
                {
                    PlatformEffector2D eff = go.AddComponent<PlatformEffector2D>();
                    eff.useOneWay = true;
                    eff.useOneWayGrouping = true;
                    eff.surfaceArc = 170f;
                    cc.usedByEffector = true;
                }
                TryGround(go);
            }
            return tm;
        }

        // ================================================================== tiles

        static void DefaultTiles(MTMoment m, out TileBase top, out TileBase fill, out TileBase plat)
        {
            switch (m)
            {
                case MTMoment.Fase1:
                    top = MTAssetSetup.LoadTile("mt_grey_f1_top"); fill = MTAssetSetup.LoadTile("mt_grey_f1_fill"); plat = MTAssetSetup.LoadTile("mt_grey_f1_plat"); break;
                case MTMoment.Fase2:
                    top = MTAssetSetup.LoadTile("mt_grey_f2_top"); fill = MTAssetSetup.LoadTile("mt_grey_f2_fill"); plat = MTAssetSetup.LoadTile("mt_grey_f2_plat"); break;
                case MTMoment.Fase3:
                    top = MTAssetSetup.LoadTile("mt_grey_f3_top"); fill = MTAssetSetup.LoadTile("mt_grey_f3_fill"); plat = MTAssetSetup.LoadTile("mt_grey_f3_plat"); break;
                case MTMoment.Fase4:
                    top = MTAssetSetup.LoadTile("mt_tile_top_curtain_a"); fill = MTAssetSetup.LoadTile("mt_tile_brick"); plat = MTAssetSetup.LoadTile("mt_tile_top_lattice"); break;
                default:
                    top = MTAssetSetup.LoadTile("mt_tile_top_curtain_b"); fill = MTAssetSetup.LoadTile("mt_tile_brick_b"); plat = MTAssetSetup.LoadTile("mt_tile_top_lattice"); break;
            }
        }

        static Color GreyboxTint(MTMoment m)
        {
            switch (m)
            {
                case MTMoment.Fase1: return Pink;
                case MTMoment.Fase2: return Gold;
                case MTMoment.Fase3: return Green;
                default: return Color.white;
            }
        }

        static void FillSection(Sec s)
        {
            TileBase dTop, dFill, dPlat;
            DefaultTiles(s.moment, out dTop, out dFill, out dPlat);

            TileBase top = s.skin != null && s.skin.groundTopTile != null ? s.skin.groundTopTile : dTop;
            TileBase fill = s.skin != null && s.skin.groundFillTile != null ? s.skin.groundFillTile : dFill;
            TileBase plat = s.skin != null && s.skin.platformTile != null ? s.skin.platformTile : dPlat;
            TileBase bgTile = s.skin != null ? s.skin.backgroundFillTile : null;

            bool custom = s.skin != null && (s.skin.groundTopTile != null || s.skin.groundFillTile != null || s.skin.platformTile != null);
            Color tint = custom ? Color.white : GreyboxTint(s.moment);
            s.ground.color = tint;
            s.plat.color = tint;

            s.ground.ClearAllTiles();
            s.plat.ClearAllTiles();

            int[][] segs = MTLevelLayout.Ground(s.moment);
            for (int i = 0; i < segs.Length; i++)
            {
                for (int x = segs[i][0]; x < segs[i][1]; x++)
                {
                    s.ground.SetTile(new Vector3Int(x, -1, 0), top);
                    for (int y = -2; y >= -MTLevelLayout.GroundRows; y--)
                        s.ground.SetTile(new Vector3Int(x, y, 0), fill);
                }
            }

            int[][] pl = MTLevelLayout.Platforms(s.moment);
            for (int i = 0; i < pl.Length; i++)
                for (int x = pl[i][0]; x < pl[i][0] + pl[i][2]; x++)
                    s.plat.SetTile(new Vector3Int(x, pl[i][1] - 1, 0), plat);

            if (s.bg != null)
            {
                s.bg.ClearAllTiles();
                if (bgTile != null)
                    for (int x = s.x0; x < s.x1; x++)
                        for (int y = -MTLevelLayout.GroundRows; y < 16; y++)
                            s.bg.SetTile(new Vector3Int(x, y, 0), bgTile);
            }
        }

        // ================================================================== música

        static MTMomentConfig[] MakeMoments()
        {
            MTMomentConfig[] c = new MTMomentConfig[5];
            c[0] = Moment(MTMoment.Fase1, "Fase 1 - Disco Fever (reprise)", new Color(0.20f, 0.05f, 0.22f));
            c[1] = Moment(MTMoment.Fase2, "Fase 2 - The Hive (reprise)", new Color(0.18f, 0.13f, 0.02f));
            c[2] = Moment(MTMoment.Fase3, "Fase 3 - Graveyard Groove (reprise)", new Color(0.04f, 0.14f, 0.07f));
            c[3] = Moment(MTMoment.Fase4, "Fase 4 - Mayhem Theatre", new Color(0.20f, 0.02f, 0.05f));
            c[4] = Moment(MTMoment.MixHarmonico, "Mix Harmônico - Lady in Red (luta final)", new Color(0.04f, 0.02f, 0.07f));
            return c;
        }

        static MTMomentConfig Moment(MTMoment m, string label, Color bg)
        {
            MTMomentConfig c = new MTMomentConfig();
            c.moment = m;
            c.label = label;
            c.bpm = 64f;
            c.loop = true;
            c.volume = 1f;
            c.backgroundColor = bg;
            return c;
        }

        // ================================================================== seção 1 — Disco Fever

        static void BuildFase1(Sec s)
        {
            Backdrop(s, "mt_backdrop_auditorium_tall", new Color(1f, 0.55f, 0.85f, 1f));
            for (int i = 0; i < 3; i++)
            {
                float x = 7f + i * 15f;
                Deco(s, "decor", "mt_marquee_star", x, 6.8f, 2.2f, -1, Pink, true, true);
            }
            Deco(s, "lighting", "mt_lamp_post", 3f, 0f, 2.9f, -1, Pink, true, true);
            Deco(s, "lighting", "mt_lamp_post", 11f, 0f, 2.9f, -1, Pink, true, true);
            Deco(s, "lighting", "mt_lamp_post", 23f, 0f, 2.9f, -1, Pink, true, true);
            Deco(s, "lighting", "mt_lamp_post", 41f, 0f, 2.9f, -1, Pink, true, true);

            // plataformas que somem no beat sobre o buraco (13..21): nunca duas ocultas ao mesmo tempo
            Sprite ps = PlatSprite(s);
            BeatPlat(s, "BeatPlat_1", 14.5f, 0f, 2.4f, "1110", 0, ps, Pink);
            BeatPlat(s, "BeatPlat_2", 17f, 0f, 2.4f, "1110", 1, ps, Pink);
            BeatPlat(s, "BeatPlat_3", 19.5f, 0f, 2.4f, "1110", 2, ps, Pink);

            Spikes(s, 9.5f);
            Spikes(s, 34f);
            Spikes(s, 41.5f);

            Enemy(s, "Dancer_1", 6f, 0.05f, MTEnemyKind.Patroller, "mt_dummy_dance_a", Pink, 1.5f, 20f, 15f, 1.8f, 8f);
            Enemy(s, "Dancer_2", 27f, 0.05f, MTEnemyKind.Patroller, "mt_dummy_dance_b", Pink, 1.5f, 20f, 15f, 1.8f, 8f);
            Enemy(s, "Dancer_3", 38f, 0.05f, MTEnemyKind.Patroller, "mt_dummy_dance_a", Pink, 1.5f, 20f, 15f, 1.8f, 8f);

            float[][] v = { new[] { 5.5f, 1.3f }, new[] { 14.5f, 1.5f }, new[] { 17f, 1.5f }, new[] { 19.5f, 1.5f },
                            new[] { 26f, 3.3f }, new[] { 31.5f, 5.3f }, new[] { 37f, 3.3f } };
            for (int i = 0; i < v.Length; i++) Vinyl(s, v[i][0], v[i][1]);
            Note(s, 23f, 1f);
            Note(s, 40f, 1f);
        }

        // ================================================================== seção 2 — The Hive

        static void BuildFase2(Sec s)
        {
            Backdrop(s, "mt_backdrop_auditorium_tall", new Color(1f, 0.85f, 0.45f, 1f));
            float[] bx = { 47f, 60f, 72f, 84f };
            for (int i = 0; i < bx.Length; i++) Deco(s, "decor", "mt_banner_fleur_big", bx[i], 9.4f, 3.4f, -1, Gold, true, false);
            Deco(s, "columns", "mt_column_red_gold", 46f, 0f, 4.2f, -1, Gold);
            Deco(s, "columns", "mt_column_red_gold", 86f, 0f, 4.2f, -1, Gold);

            // plataformas móveis sincronizadas (meio-ciclo = 4 batidas) sobre o buraco 52..66
            Sprite ps = PlatSprite(s);
            MovingPlat(s, "Move_1", 53.5f, 0f, 3f, new Vector2(1.5f, 0f), 4, 0f, ps, Gold);
            MovingPlat(s, "Move_2", 58.5f, 0f, 3f, new Vector2(0f, 2f), 4, 0f, ps, Gold);
            MovingPlat(s, "Move_3", 63f, 0f, 3f, new Vector2(1.5f, 0f), 4, 2f, ps, Gold);

            Spikes(s, 70.5f);
            Spikes(s, 77.5f);
            Spikes(s, 83f);

            Enemy(s, "Bee_1", 60f, 3.8f, MTEnemyKind.Hoverer, "props/mt_mask_white", Gold, 0.9f, 20f, 15f, 2.2f, 9f);
            Enemy(s, "Bee_2", 77f, 4.4f, MTEnemyKind.Hoverer, "props/mt_mask_white", Gold, 0.9f, 20f, 15f, 2.2f, 9f);
            Enemy(s, "Guard_1", 82f, 0.05f, MTEnemyKind.Patroller, "mt_dummy_dance_b", Gold, 1.5f, 20f, 15f, 1.8f, 8f);

            float[][] v = { new[] { 58.5f, 3.6f }, new[] { 63f, 1.6f }, new[] { 70f, 3.3f }, new[] { 75.5f, 5.3f },
                            new[] { 82f, 3.3f }, new[] { 86.5f, 4.3f } };
            for (int i = 0; i < v.Length; i++) Vinyl(s, v[i][0], v[i][1]);
            Note(s, 67.5f, 1f);
            Checkpoint(s, "Checkpoint_F2", 70f, MTMoment.Fase2);
        }

        // ================================================================== seção 3 — Graveyard Groove

        static void BuildFase3(Sec s)
        {
            Backdrop(s, "mt_backdrop_auditorium_tall", new Color(0.55f, 0.85f, 0.65f, 1f));
            float[] cx = { 90f, 98f, 106f, 112f, 120f, 130f };
            for (int i = 0; i < cx.Length; i++) Deco(s, "lighting", "mt_candelabra_small", cx[i], 0f, 3.2f, -1, Green, true, true);

            // mãos que sobem do chão no beat 3 (cada uma com um deslocamento)
            Hand(s, "Hand_1", 96f, 0);
            Hand(s, "Hand_2", 106f, 1);
            Hand(s, "Hand_3", 112.5f, 2);
            Hand(s, "Hand_4", 123f, 3);
            Hand(s, "Hand_5", 129f, 1);

            Spikes(s, 90f);
            Spikes(s, 128f);

            Enemy(s, "Zombie_1", 94f, 0.05f, MTEnemyKind.Patroller, "mt_doll_gothic", Green, 1.6f, 20f, 15f, 1.5f, 8f);
            Enemy(s, "Zombie_2", 110f, 0.05f, MTEnemyKind.Patroller, "mt_doll_gothic", Green, 1.6f, 20f, 15f, 1.5f, 8f);
            Enemy(s, "Zombie_3", 124f, 0.05f, MTEnemyKind.Patroller, "mt_doll_gothic", Green, 1.6f, 20f, 15f, 1.5f, 8f);

            float[][] v = { new[] { 93.5f, 3.3f }, new[] { 101.5f, 2f }, new[] { 109.5f, 3.3f }, new[] { 116.5f, 2f },
                            new[] { 122.5f, 3.3f }, new[] { 127.5f, 4.3f } };
            for (int i = 0; i < v.Length; i++) Vinyl(s, v[i][0], v[i][1]);
            Note(s, 113f, 1f);
            Checkpoint(s, "Checkpoint_F3", 112f, MTMoment.Fase3);
        }

        // ================================================================== seção 4 — Mayhem Theatre

        static void BuildFase4(Sec s)
        {
            Backdrop(s, "mt_backdrop_auditorium_tall", Dim);
            float[] colx = { 134f, 148f, 160f, 174f };
            for (int i = 0; i < colx.Length; i++) Deco(s, "columns", (i % 2 == 0) ? "mt_column_red_gold" : "mt_column_tall_b", colx[i], 0f, 4.4f, -1, Color.white);
            Deco(s, "lighting", "mt_chandelier", 141f, 10f, 1.8f, -1, Color.white, true, true);
            Deco(s, "lighting", "mt_chandelier", 158f, 10f, 1.8f, -1, Color.white, true, true);
            Deco(s, "lighting", "mt_chandelier", 170f, 10f, 1.8f, -1, Color.white, true, true);
            Deco(s, "walls", "mt_window_arch_red", 138f, 1.4f, 2.6f, -1, Color.white);
            Deco(s, "walls", "mt_window_arch_red", 153f, 1.4f, 2.6f, -1, Color.white);
            Deco(s, "walls", "mt_window_tall_gold", 165f, 1.4f, 3.6f, -1, Color.white);
            Deco(s, "decor", "mt_banner_hat", 136f, 9.5f, 3f, -1, Color.white, true, false);
            Deco(s, "decor", "mt_banner_red_diamond", 171f, 9.5f, 3f, -1, Color.white, true, false);

            Spikes(s, 146f);
            Spikes(s, 170.5f);
            Saw(s, "Saw_Static", 140.5f, null, 0);
            Saw(s, "Saw_Moving_1", 159f, new Vector2(7f, 0f), 4);
            Saw(s, "Saw_Moving_2", 166f, new Vector2(-4f, 0f), 2);

            // marionetes: perseguem e atacam
            Enemy(s, "Marionette_Pierrot", 136f, 0.05f, MTEnemyKind.Chaser, "mt_marionette_pierrot", Color.white, 1.9f, 20f, 15f, 2.8f, 11f);
            Enemy(s, "Marionette_Ballerina", 146f, 0.05f, MTEnemyKind.Chaser, "mt_marionette_ballerina", Color.white, 1.9f, 20f, 15f, 3.0f, 11f);
            Enemy(s, "Marionette_Jester", 158f, 0.05f, MTEnemyKind.Chaser, "mt_marionette_jester", Color.white, 1.9f, 20f, 15f, 3.0f, 11f);
            Enemy(s, "Marionette_Wood", 172f, 0.05f, MTEnemyKind.Chaser, "mt_marionette_wood", Color.white, 2.0f, 20f, 15f, 2.8f, 11f);
            // inimigo forte (30 de dano)
            Enemy(s, "Jester_Big", 163f, 0.05f, MTEnemyKind.Patroller, "mt_jester_big", Crimson, 2.3f, 40f, 30f, 1.6f, 9f);
            // reprise dos inimigos das outras fases
            Enemy(s, "Reprise_Dancer", 141f, 0.05f, MTEnemyKind.Patroller, "mt_dummy_dance_a", Pink, 1.5f, 20f, 15f, 1.8f, 8f);
            Enemy(s, "Reprise_Bee", 166f, 4.8f, MTEnemyKind.Hoverer, "props/mt_mask_white", Gold, 0.9f, 20f, 15f, 2.2f, 9f);
            Enemy(s, "Reprise_Zombie", 156f, 0.05f, MTEnemyKind.Patroller, "mt_doll_gothic", Green, 1.6f, 20f, 15f, 1.5f, 8f);

            float[][] v = { new[] { 139f, 3.3f }, new[] { 145.5f, 4.3f }, new[] { 151.5f, 2.2f }, new[] { 157.5f, 3.3f },
                            new[] { 163.5f, 5.3f }, new[] { 169f, 3.3f } };
            for (int i = 0; i < v.Length; i++) Vinyl(s, v[i][0], v[i][1]);
            Note(s, 148f, 1f);
            Note(s, 174.5f, 1f);
            Checkpoint(s, "Checkpoint_F4", 154f, MTMoment.Fase4);
        }

        // ================================================================== seção 5 — Mix Harmônico (arena do boss)

        static void BuildArena(Sec s, MTLevelController lc)
        {
            Backdrop(s, "mt_backdrop_auditorium_tall", new Color(0.55f, 0.30f, 0.38f, 1f));
            Deco(s, "backdrops", "mt_backdrop_masks_stage", 194f, 6f, 12f, -3, Color.white);
            Deco(s, "columns", "mt_column_red_gold", 178.5f, 0f, 4.6f, -1, Color.white);
            Deco(s, "columns", "mt_column_red_gold", 209.5f, 0f, 4.6f, -1, Color.white);
            Deco(s, "lighting", "mt_chandelier", 186f, 10.5f, 2f, -1, Color.white, true, true);
            Deco(s, "lighting", "mt_chandelier", 194f, 11f, 2.2f, -1, Color.white, true, true);
            Deco(s, "lighting", "mt_chandelier", 202f, 10.5f, 2f, -1, Color.white, true, true);
            Deco(s, "decor", "mt_banner_red_diamond", 183f, 9.8f, 3.2f, -1, Color.white, true, false);
            Deco(s, "decor", "mt_banner_red_diamond", 205f, 9.8f, 3.2f, -1, Color.white, true, false);

            float[][] v = { new[] { 183f, 3.3f }, new[] { 194f, 4.3f }, new[] { 205f, 3.3f } };
            for (int i = 0; i < v.Length; i++) Vinyl(s, v[i][0], v[i][1]);

            // paredes: LockWall_Left (liga na luta) e Wall_Right (fixa)
            GameObject lockWall = Obj(s.triggers, "LockWall_Left", new Vector2(176.5f, 6f));
            BoxCollider2D lb = lockWall.AddComponent<BoxCollider2D>();
            lb.size = new Vector2(1f, 30f);
            lb.enabled = false;
            TryGround(lockWall);

            GameObject wallR = Obj(s.triggers, "Wall_Right", new Vector2(212.5f, 6f));
            BoxCollider2D rb = wallR.AddComponent<BoxCollider2D>();
            rb.size = new Vector2(1f, 30f);
            TryGround(wallR);

            // gatilho de entrada da luta
            GameObject trig = Obj(s.triggers, "ArenaTrigger", new Vector2(182f, 6f));
            BoxCollider2D tb = trig.AddComponent<BoxCollider2D>();
            tb.isTrigger = true;
            tb.size = new Vector2(4f, 14f);
            MTBossArena arena = trig.AddComponent<MTBossArena>();
            arena.lockWalls = new Collider2D[] { lb };

            // boss: o visual é o SEU prefab (aqui entra um placeholder para a luta já rodar)
            GameObject bossGo = Obj(s.root, "BossController_LadyInRed", new Vector2(194f, 0f));
            MTBossController boss = bossGo.AddComponent<MTBossController>();
            boss.arenaLeftX = 177f;
            boss.arenaRightX = 211f;
            boss.floorY = 0f;
            boss.bossPrefab = MakeBossPlaceholderPrefab();
            boss.placeholderSprite = MTAssetSetup.LoadSprite("boss", "mt_puppeteer_master");
            boss.handSprite = MTAssetSetup.LoadSprite("hazards", "mt_hand_emerge");
            boss.maskSprite = MTAssetSetup.LoadSprite("boss", "mt_mask_halfface");
            boss.curtainSprite = MTAssetSetup.LoadSprite("decor", "mt_curtain_grand");
            boss.noteSprite = MTAssetSetup.LoadSprite("collectibles", "mt_note_heal");
            boss.fragmentSprite = MTAssetSetup.LoadSprite("collectibles", "mt_fragment_gold");
            boss.summonSprites = new Sprite[]
            {
                MTAssetSetup.LoadSprite("enemies", "mt_marionette_pierrot"),
                MTAssetSetup.LoadSprite("enemies", "mt_marionette_ballerina"),
                MTAssetSetup.LoadSprite("enemies", "mt_marionette_jester"),
                MTAssetSetup.LoadSprite("enemies", "mt_marionette_wood")
            };
            boss.panelSettings = FindOrCreatePanelSettings();
            arena.boss = boss;

            lc.boss = boss;
            lc.arena = arena;
        }

        // ================================================================== limites, câmera, player

        static void BuildBoundaries(Transform root)
        {
            GameObject b = Obj(root, "Boundaries", Vector2.zero);

            GameObject ws = Obj(b.transform, "Wall_Start", new Vector2(-0.5f, 6f));
            BoxCollider2D wc = ws.AddComponent<BoxCollider2D>();
            wc.size = new Vector2(1f, 30f);
            TryGround(ws);

            // cortinas de transição entre os momentos (visual)
            int[] edges = { 44, 88, 132, 176 };
            Color[] tints = { Gold, Green, Color.white, Color.white };
            for (int i = 0; i < edges.Length; i++)
            {
                Sprite sp = MTAssetSetup.LoadSprite("decor", "mt_curtain_grand");
                Spr(b.transform, "Curtain_" + edges[i], sp, new Vector2(edges[i], 9.6f), 7.2f, -1, tints[i], false);
            }
        }

        static void BuildCameraBounds(Transform root)
        {
            GameObject cb = Obj(root, "CameraBounds", Vector2.zero);
            PolygonCollider2D pc = cb.AddComponent<PolygonCollider2D>();
            pc.isTrigger = true;
            pc.points = new Vector2[] { new Vector2(-1f, -6f), new Vector2(213f, -6f), new Vector2(213f, 14f), new Vector2(-1f, 14f) };
        }

        static void WirePlayer()
        {
            GameObject player = null;
            try { player = GameObject.FindWithTag("Player"); }
            catch (UnityException) { player = null; }

            if (player == null)
            {
                Debug.LogWarning("[Mayhem Theatre] Nenhum objeto com a tag Player na cena. Adicione o componente 'MT Player Hook' ao seu Player manualmente.");
                return;
            }
            if (player.GetComponent<MTPlayerHook>() == null)
            {
                Undo.AddComponent<MTPlayerHook>(player);
                Debug.Log("[Mayhem Theatre] MT Player Hook adicionado ao objeto '" + player.name + "'.");
            }
        }

        // ================================================================== helpers de objetos

        static GameObject Obj(Transform parent, string name, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            return go;
        }

        static GameObject Spr(Transform parent, string name, Sprite sp, Vector2 pos, float height, int order, Color tint, bool flipX)
        {
            if (sp == null) { missingSprites++; return null; }
            GameObject go = Obj(parent, name, pos);
            float h = Mathf.Max(0.001f, sp.bounds.size.y);
            float k = height > 0f ? height / h : 1f;
            go.transform.localScale = new Vector3(flipX ? -k : k, k, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = tint;
            sr.sortingOrder = order;
            return go;
        }

        static GameObject Deco(Sec s, string cat, string name, float x, float y, float height, int order, Color tint)
        {
            return Deco(s, cat, name, x, y, height, order, tint, false, false);
        }

        static GameObject Deco(Sec s, string cat, string name, float x, float y, float height, int order, Color tint, bool pulse, bool flash)
        {
            Sprite sp = MTAssetSetup.LoadSprite(cat, name);
            GameObject go = Spr(s.decor, name, sp, new Vector2(x, y), height, order, tint, false);
            if (go != null && pulse)
            {
                MTBeatPulse p = go.AddComponent<MTBeatPulse>();
                p.flashColor = flash;
                p.scalePunch = 0.05f;
            }
            return go;
        }

        static void Backdrop(Sec s, string name, Color tint)
        {
            Sprite sp = MTAssetSetup.LoadSprite("backdrops", name);
            if (sp == null) { missingSprites++; return; }
            float h = sp.bounds.size.y;
            GameObject go = Obj(s.decor, "Backdrop", new Vector2((s.x0 + s.x1) * 0.5f, -3f + h * 0.5f));
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(s.x1 - s.x0, h);
            sr.color = tint;
            sr.sortingOrder = -3;
        }

        static Sprite PlatSprite(Sec s)
        {
            TileBase dTop, dFill, dPlat;
            DefaultTiles(s.moment, out dTop, out dFill, out dPlat);
            Tile t = dPlat as Tile;
            return t != null ? t.sprite : null;
        }

        static void OneWay(GameObject go, Collider2D col)
        {
            PlatformEffector2D eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;
            col.usedByEffector = true;
        }

        static GameObject TiledPlatform(Sec s, string name, float cx, float top, float w, Sprite sp, Color tint)
        {
            GameObject go = Obj(s.dyn, name, new Vector2(cx, top - 0.5f));
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp != null ? sp : MTUtil.WhiteSprite;
            if (sp != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(w, 1f);
            }
            else
            {
                go.transform.localScale = new Vector3(w, 1f, 1f);
            }
            sr.color = tint;
            sr.sortingOrder = 1;

            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.size = sp != null ? new Vector2(w, 1f) : new Vector2(1f, 1f);
            OneWay(go, bc);
            TryGround(go);
            return go;
        }

        static void BeatPlat(Sec s, string name, float cx, float top, float w, string pattern, int offset, Sprite sp, Color tint)
        {
            GameObject go = TiledPlatform(s, name, cx, top, w, sp, tint);
            MTBeatPlatform bp = go.AddComponent<MTBeatPlatform>();
            bp.pattern = pattern;
            bp.beatOffset = offset;
        }

        static void MovingPlat(Sec s, string name, float cx, float top, float w, Vector2 offsetB, int beatsPerHalf, float phase, Sprite sp, Color tint)
        {
            GameObject go = TiledPlatform(s, name, cx, top, w, sp, tint);
            MTMovingPlatform mp = go.AddComponent<MTMovingPlatform>();
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
            mp.offsetB = offsetB;
            mp.beatsPerHalfCycle = beatsPerHalf;
            mp.phaseOffsetBeats = phase;
        }

        static void Spikes(Sec s, float x)
        {
            Sprite sp = MTAssetSetup.LoadSprite("hazards", "mt_spikes_curtain");
            GameObject go = Spr(s.hazards, "Spikes_" + x, sp, new Vector2(x, 0f), 0.8f, 1, Color.white, false);
            if (go == null) return;
            Vector3 b = sp.bounds.size;
            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(b.x * 0.75f, b.y * 0.7f);
            bc.offset = new Vector2(0f, b.y * 0.4f);
            MTContactDamage cd = go.AddComponent<MTContactDamage>();
            cd.damage = 15f;
        }

        static void Hand(Sec s, string name, float x, int offset)
        {
            Sprite sp = MTAssetSetup.LoadSprite("hazards", "mt_hand_emerge");
            GameObject go = Spr(s.hazards, name, sp, new Vector2(x, 0f), 1.9f, -1, Green, false);
            if (go == null) return;
            Vector3 b = sp.bounds.size;
            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(b.x * 0.6f, b.y * 0.85f);
            bc.offset = new Vector2(0f, b.y * 0.5f);
            MTBeatHazard bh = go.AddComponent<MTBeatHazard>();
            bh.pattern = "0010";
            bh.beatOffset = offset;
            bh.riseDistance = 1.9f;
            bh.damage = 15f;
        }

        static void Saw(Sec s, string name, float x, Vector2? move, int beatsPerHalf)
        {
            Sprite sp = MTAssetSetup.LoadSprite("hazards", "mt_saw_mask_wheel");
            if (sp == null) { missingSprites++; return; }

            GameObject holder = Obj(s.hazards, name, new Vector2(x, 0.6f));
            if (move.HasValue)
            {
                MTMovingPlatform mp = holder.AddComponent<MTMovingPlatform>();
                Rigidbody2D rb = holder.GetComponent<Rigidbody2D>();
                if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
                mp.offsetB = move.Value;
                mp.beatsPerHalfCycle = beatsPerHalf;
                mp.carryRiders = false;
            }

            GameObject blade = Spr(holder.transform, "Blade", sp, holder.transform.position, 1.2f, 1, Color.white, false);
            Vector3 b = sp.bounds.size;
            CircleCollider2D cc = blade.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = b.x * 0.42f;
            blade.AddComponent<MTSpinner>();
            MTContactDamage cd = blade.AddComponent<MTContactDamage>();
            cd.damage = 15f;
        }

        static void Enemy(Sec s, string name, float x, float y, MTEnemyKind kind, string sprite, Color tint, float height,
            float hp, float dmg, float speed, float aggro)
        {
            Sprite sp;
            if (sprite.IndexOf('/') >= 0)
            {
                string[] parts = sprite.Split('/');
                sp = MTAssetSetup.LoadSprite(parts[0], parts[1]);
            }
            else
            {
                sp = MTAssetSetup.LoadSprite("enemies", sprite);
            }
            if (sp == null) missingSprites++;

            GameObject go = Obj(s.enemies, name, new Vector2(x, y));
            MTEnemySpawner sw = go.AddComponent<MTEnemySpawner>();
            sw.kind = kind;
            sw.sprite = sp;
            sw.tint = tint;
            sw.spriteHeight = height;
            sw.health = hp;
            sw.damage = dmg;
            sw.speed = speed;
            sw.aggroRange = aggro;
            sw.noteSprite = MTAssetSetup.LoadSprite("collectibles", "mt_note_heal");
        }

        static void Vinyl(Sec s, float x, float y)
        {
            GameObject go;
            if (vinylPrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(vinylPrefab, s.pickups);
                go.transform.position = new Vector3(x, y, 0f);
                return;
            }
            Sprite sp = MTAssetSetup.LoadSprite("collectibles", "mt_vinyl_bw");
            go = MTPickupFactory.Create(MTPickup.PickupKind.Vinyl, new Vector3(x, y, 0f), null, sp, Color.white, 0.8f);
            go.name = "Vinyl";
            go.transform.SetParent(s.pickups, true);
        }

        static void Note(Sec s, float x, float y)
        {
            Sprite sp = MTAssetSetup.LoadSprite("collectibles", "mt_note_heal");
            GameObject go = MTPickupFactory.Create(MTPickup.PickupKind.Note, new Vector3(x, y, 0f), null, sp, Color.white, 0.7f);
            go.name = "HealNote";
            go.transform.SetParent(s.pickups, true);
        }

        static void Checkpoint(Sec s, string name, float x, MTMoment m)
        {
            GameObject go = Obj(s.triggers, name, new Vector2(x, 0.6f));
            BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.5f, 14f);
            bc.offset = new Vector2(0f, 5f);
            MTCheckpoint cp = go.AddComponent<MTCheckpoint>();
            cp.moment = m;
        }

        // ================================================================== boss, UI, assets do usuário

        static GameObject MakeBossPlaceholderPrefab()
        {
            MTAssetSetup.EnsureFolder(MTPaths.Prefabs);
            string path = MTPaths.Prefabs + "/MT_Boss_LadyInRed_Placeholder.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject go = new GameObject("MT_Boss_LadyInRed_Placeholder");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MTAssetSetup.LoadSprite("boss", "mt_puppeteer_master");
            sr.sortingOrder = 3;
            go.transform.localScale = Vector3.one * 0.8f;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        static PanelSettings FindOrCreatePanelSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:PanelSettings");
            PanelSettings ours = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                PanelSettings ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(p);
                if (ps == null) continue;
                if (p.StartsWith(MTPaths.Root)) { ours = ps; continue; }
                return ps; // reaproveita o do seu HUD
            }
            if (ours != null) return ours;

            MTAssetSetup.EnsureFolder(MTPaths.UI);
            PanelSettings created = ScriptableObject.CreateInstance<PanelSettings>();
            created.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            created.referenceResolution = new Vector2Int(1920, 1080);
            string[] themes = AssetDatabase.FindAssets("UnityDefaultRuntimeTheme t:ThemeStyleSheet");
            if (themes.Length > 0)
                created.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(themes[0]));
            AssetDatabase.CreateAsset(created, MTPaths.UI + "/MT_PanelSettings.asset");
            AssetDatabase.SaveAssets();
            return created;
        }

        static GameObject FindUserPrefab(string[] include, string[] exclude)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith(MTPaths.Root)) continue;
                string n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                bool bad = false;
                for (int k = 0; k < exclude.Length; k++) if (n.Contains(exclude[k])) bad = true;
                if (bad) continue;

                bool ok = false;
                for (int k = 0; k < include.Length; k++) if (n.Contains(include[k])) ok = true;
                if (!ok) continue;

                GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (g != null && g.GetComponentInChildren<Collider2D>() != null)
                {
                    Debug.Log("[Mayhem Theatre] Vinil: usando o seu prefab '" + path + "'.");
                    return g;
                }
            }
            return null;
        }

        // ================================================================== utilidades

        static void TryGround(GameObject go)
        {
            int layer = LayerMask.NameToLayer("Ground");
            if (layer >= 0) go.layer = layer;
            string[] tags = InternalEditorUtility.tags;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == "Ground") { go.tag = "Ground"; break; }
            }
        }

        static GameObject FindRoot()
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                if (roots[i].name == RootName) return roots[i];
            return null;
        }

        static string FaseLabel(MTMoment m)
        {
            switch (m)
            {
                case MTMoment.Fase1: return "Fase 1 (Disco Fever)";
                case MTMoment.Fase2: return "Fase 2 (The Hive)";
                case MTMoment.Fase3: return "Fase 3 (Graveyard Groove)";
                case MTMoment.Fase4: return "Fase 4 (Mayhem Theatre)";
                default: return "arena do boss";
            }
        }

        static string Checklist()
        {
            List<string> l = new List<string>();
            l.Add("[Mayhem Theatre] Fase 4 criada: " + RootName);
            l.Add("PRÓXIMOS PASSOS:");
            l.Add(" 1) MTMusicDirector (em Managers): arraste os 5 AudioClips e ajuste o BPM de cada momento ao BPM do clip.");
            l.Add(" 2) MTBossController: troque 'Boss Prefab' pelo SEU prefab da Lady in Red.");
            l.Add(" 3) Cada seção (MTSectionSkin): arraste Tiles das suas Tile Palettes e clique em 'Reaplicar tiles desta seção'.");
            l.Add(" 4) Player: componente MT Player Hook (onDamaged / onDied) e MTDamageSource no prefab do projétil.");
            l.Add(" 5) Câmera: use o objeto CameraBounds como Bounding Shape do Confiner 2D.");
            l.Add(" 6) MTLevelController: ligue onLevelComplete ao seu GameManager / SceneController.");
            if (missingSprites > 0) l.Add("AVISO: " + missingSprites + " sprite(s) não encontrados — rode o menu 1 e confira a pasta Sprites.");
            return string.Join("\n", l.ToArray());
        }
    }
}
