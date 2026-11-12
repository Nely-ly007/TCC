using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Anchor = PopAdventure.Fase3.EditorTools.GGEditorUtil.Anchor;

namespace PopAdventure.Fase3.EditorTools
{
    /// <summary>
    /// Gera todos os prefabs da fase. Convenção: a RAIZ de cada prefab fica no CENTRO de uma célula (1x1) e a base do sprite
    /// encosta na base da célula — assim o GameObject Brush pinta tudo "em pé" no chão. Exceções: Lobo e Boss (raiz nos PÉS)
    /// e plataformas (superfície de pisar = raiz + 0.5).
    /// </summary>
    public static class GGPrefabFactory
    {
        const string DirProps = GGPaths.Prefabs + "/Props";
        const string DirPlatforms = GGPaths.Prefabs + "/Platforms";
        const string DirHazards = GGPaths.Prefabs + "/Hazards";
        const string DirPickups = GGPaths.Prefabs + "/Pickups";
        const string DirInteract = GGPaths.Prefabs + "/Interactables";
        const string DirEnemies = GGPaths.Prefabs + "/Enemies";
        const string DirBoss = GGPaths.Prefabs + "/Boss";
        const string DirFx = GGPaths.Prefabs + "/FX";

        public const string BossPrefabName = "Boss_Graveyard";

        public static void CreateAll()
        {
            GGEditorUtil.ClearCaches();

            CreatePickups();
            CreateProps();
            CreatePlatforms();
            CreateHazards();
            CreateInteractables();
            CreateFx();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GGEditorUtil.ClearCaches();

            CreateEnemies();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GGEditorUtil.ClearCaches();

            CreateBoss();
            CreateArena();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GGEditorUtil.ClearCaches();
        }

        /// <summary>Lista (em ordem) do que vai para a paleta de objetos.</summary>
        public static List<GGTileFactory.PaletteEntry> PaletteEntries()
        {
            List<GGTileFactory.PaletteEntry> l = new List<GGTileFactory.PaletteEntry>();
            string[] decor =
            {
                "Prop_DeadTree", "Prop_Mausoleum", "Prop_AngelStatue", "Prop_Fountain", "Prop_StoneArch", "Prop_StonePillar",
                "Prop_PillarPost", "Prop_GothicWindow", "Prop_GraveBanner", "Prop_Tombstone", "Prop_GraveCross", "Prop_Bench",
                "Prop_IronFence", "Prop_SmallFence", "Prop_Ladder", "Prop_Rubble", "Prop_Barrel", "Prop_Skull", "Prop_Bones",
                "Prop_Lantern", "Prop_StreetLamp", "Crate_Wooden", "Crate_Reinforced"
            };
            string[] plats = { "Platform_Glowing_Blink", "Platform_Glowing_Rise", "Platform_Hanging_Rail" };
            string[] hazards =
            {
                "Spikes_Small", "Spikes_Large", "SpikeTrap_Rhythm", "SpikePlatform_Moving", "SpikedBall_Swing_Long",
                "SpikedBall_Swing_Short", "Gear_Rail", "ZombieHand"
            };
            string[] pickups = { "Pickup_Vinyl", "Pickup_Note", "Pickup_Heart", "Pickup_Fragment" };
            string[] interact = { "Checkpoint_Flag", "Resonance_Lantern", "Resonance_Tombstone", "Gate_Iron" };
            string[] enemies = { "Enemy_Crow", "Enemy_Wolf", "Enemy_Ghost" };

            AddGroup(l, decor, 0);
            AddGroup(l, plats, 1);
            AddGroup(l, hazards, 2);
            AddGroup(l, pickups, 3);
            AddGroup(l, interact, 4);
            AddGroup(l, enemies, 5);
            return l;
        }

        static void AddGroup(List<GGTileFactory.PaletteEntry> l, string[] names, int g)
        {
            for (int i = 0; i < names.Length; i++) l.Add(new GGTileFactory.PaletteEntry(names[i], g));
        }

        // ================================================================== pickups
        static void CreatePickups()
        {
            MakePickup("Pickup_Vinyl", "vinyl_record", 0.7f, GGPickup.Kind.Vinyl, 1, Color.white);
            MakePickup("Pickup_Note", "music_note", 0.85f, GGPickup.Kind.HealNote, 15, Color.white);
            MakePickup("Pickup_Heart", "heart_collectible", 0.7f, GGPickup.Kind.Heart, 30, Color.white);
            MakePickup("Pickup_Fragment", "vinyl_record", 1.3f, GGPickup.Kind.Fragment, 1, new Color(1f, 0.82f, 0.25f, 1f));
        }

        static void MakePickup(string name, string sprite, float height, GGPickup.Kind kind, int amount, Color tint)
        {
            GameObject root = new GameObject(name);
            SpriteRenderer sr = GGEditorUtil.AddSprite(root.transform, "Visual", sprite, height, 0f, Anchor.Center, 3);
            sr.color = tint;
            GGEditorUtil.AddCircle(root, Mathf.Max(0.45f, height * 0.5f), Vector2.zero, true);
            GGPickup p = root.AddComponent<GGPickup>();
            GGEditorUtil.SetMany(p, "kind", (int)kind, "amount", amount, "visual", sr.transform);
            GGEditorUtil.SavePrefab(root, DirPickups, name);
        }

        // ================================================================== props
        static void CreateProps()
        {
            Prop("Prop_DeadTree", "dead_tree", 5.5f, 0f);
            Prop("Prop_Mausoleum", "mausoleum", 5.5f, 0f);
            Prop("Prop_AngelStatue", "angel_statue", 3.2f, 0f);
            Prop("Prop_Fountain", "fountain", 2.8f, 0f);
            Prop("Prop_StoneArch", "stone_arch", 4.5f, 0f);
            Prop("Prop_StonePillar", "stone_pillar", 3.5f, 0f);
            Prop("Prop_PillarPost", "pillar_post", 2.0f, 0f);
            Prop("Prop_GothicWindow", "gothic_window", 3.5f, 0f);
            Prop("Prop_GraveBanner", "grave_banner", 2.8f, 0f);
            Prop("Prop_Tombstone", "tombstone", 1.2f, 0f);
            Prop("Prop_GraveCross", "grave_cross", 1.5f, 0f);
            Prop("Prop_Bench", "bench", 0f, 1.8f);
            Prop("Prop_IronFence", "iron_fence", 1.3f, 0f);
            Prop("Prop_SmallFence", "small_fence", 1.4f, 0f);
            Prop("Prop_Ladder", "ladder", 2.5f, 0f);
            Prop("Prop_Rubble", "rubble", 0f, 1.8f);
            Prop("Prop_Barrel", "barrel", 0.9f, 0f);
            Prop("Prop_Skull", "skull", 0.5f, 0f);
            Prop("Prop_Bones", "bones", 0f, 1.0f);
            Prop("Prop_Lantern", "lantern", 1.3f, 0f, -1, true, false);
            Prop("Prop_StreetLamp", "street_lamp", 4.2f, 0f, -1, true, false);
            Prop("Crate_Wooden", "wooden_crate", 1.1f, 0f, 0, false, true);
            Prop("Crate_Reinforced", "reinforced_crate", 1.1f, 0f, 0, false, true);
        }

        static void Prop(string name, string sprite, float fitH, float fitW, int order = -1, bool pulse = false, bool solid = false)
        {
            GameObject root = new GameObject(name);
            Vector2 size;
            SpriteRenderer sr = GGEditorUtil.AddSprite(root.transform, "Sprite", sprite, fitH, fitW, Anchor.BottomOfCell, order, out size);
            if (pulse)
            {
                GGBeatPulse bp = sr.gameObject.AddComponent<GGBeatPulse>();
                GGEditorUtil.SetMany(bp, "scaleAmount", 0.05f, "brightness", 0.45f);
            }
            if (solid)
                GGEditorUtil.AddBox(root, size * 0.96f, new Vector2(0f, -0.5f + size.y * 0.5f), false);
            GGEditorUtil.SavePrefab(root, DirProps, name);
        }

        // ================================================================== plataformas
        static void CreatePlatforms()
        {
            GlowingPlatform("Platform_Glowing_Blink", false);
            GlowingPlatform("Platform_Glowing_Rise", true);
            HangingRailPlatform();
        }

        static void OneWay(GameObject root, Collider2D col)
        {
            col.usedByEffector = true;
            PlatformEffector2D eff = root.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 160f;
            eff.useSideFriction = false;
            eff.useSideBounce = false;
        }

        static void GlowingPlatform(string name, bool rise)
        {
            GameObject root = new GameObject(name);
            Vector2 size;
            GGEditorUtil.AddSprite(root.transform, "Sprite", "platform_glowing", 0f, 2.2f, Anchor.SurfaceTop, 1, out size, 0.04f);
            BoxCollider2D col = GGEditorUtil.AddBox(root, new Vector2(size.x * 0.96f, 0.3f), new Vector2(0f, 0.35f), false);
            OneWay(root, col);
            GGEditorUtil.AddKinematic(root);
            GGRhythmPlatform rp = root.AddComponent<GGRhythmPlatform>();
            GGEditorUtil.SetMany(rp, "mode", rise ? 1 : 0, "periodBeats", 4, "onBeats", rise ? 2 : 3,
                "offsetBeats", 0f, "hiddenDrop", 2.5f, "solid", col);
            if (rise) root.AddComponent<GGRiderTracker>();
            GGEditorUtil.SavePrefab(root, DirPlatforms, name);
        }

        static void HangingRailPlatform()
        {
            GameObject root = new GameObject("Platform_Hanging_Rail");
            Vector2 size;
            // a laje fica na parte de baixo do sprite (as correntes ficam em cima): topo da laje ≈ 63% da altura
            GGEditorUtil.AddSprite(root.transform, "Sprite", "platform_hanging", 0f, 2.6f, Anchor.SurfaceTop, 1, out size, 0.63f);
            BoxCollider2D col = GGEditorUtil.AddBox(root, new Vector2(size.x * 0.96f, 0.3f), new Vector2(0f, 0.35f), false);
            OneWay(root, col);
            GGEditorUtil.AddKinematic(root);
            root.AddComponent<GGRiderTracker>();
            GGRailMover rm = root.AddComponent<GGRailMover>();
            GGEditorUtil.SetMany(rm, "travel", new Vector2(5f, 0f), "beatsPerCycle", 8f, "offsetBeats", 0f, "smooth", true);
            GGEditorUtil.SavePrefab(root, DirPlatforms, "Platform_Hanging_Rail");
        }

        // ================================================================== hazards
        static void CreateHazards()
        {
            StaticSpikes("Spikes_Small", 1.0f);
            StaticSpikes("Spikes_Large", 2.0f);
            SpikeTrap();
            SpikePlatformMoving();
            SpikedBall("SpikedBall_Swing_Long", 5.5f);
            SpikedBall("SpikedBall_Swing_Short", 3.5f);
            GearRail();
            ZombieHandPrefab();
        }

        static void StaticSpikes(string name, float fitW)
        {
            GameObject root = new GameObject(name);
            Vector2 size;
            GGEditorUtil.AddSprite(root.transform, "Sprite", "spikes", 0f, fitW, Anchor.BottomOfCell, 1, out size);
            GGEditorUtil.AddBox(root, new Vector2(size.x * 0.85f, size.y * 0.6f), new Vector2(0f, -0.5f + size.y * 0.38f), true);
            root.AddComponent<GGHazard>();
            GGEditorUtil.SavePrefab(root, DirHazards, name);
        }

        static void SpikeTrap()
        {
            GameObject root = new GameObject("SpikeTrap_Rhythm");
            GameObject vis = GGEditorUtil.NewChild(root.transform, "Visual", Vector3.zero);
            Vector2 size;
            // ordem -1: escondido ATRÁS do chão (Ground = 0) quando recolhido
            GGEditorUtil.AddSprite(vis.transform, "Sprite", "spikes", 0f, 1.4f, Anchor.BottomOfCell, -1, out size);
            GGEditorUtil.AddBox(root, new Vector2(size.x * 0.8f, size.y * 0.6f), new Vector2(0f, -0.5f + size.y * 0.4f), true);
            GGSpikeTrap st = root.AddComponent<GGSpikeTrap>();
            GGEditorUtil.SetMany(st, "visual", vis.transform, "periodBeats", 4, "activeBeats", 1, "offsetBeats", 0f, "retractDistance", 0.9f);
            GGEditorUtil.SavePrefab(root, DirHazards, "SpikeTrap_Rhythm");
        }

        static void SpikePlatformMoving()
        {
            GameObject root = new GameObject("SpikePlatform_Moving");
            Vector2 size;
            GGEditorUtil.AddSprite(root.transform, "Sprite", "spike_platform", 0f, 2.0f, Anchor.BottomOfCell, 1, out size);
            GGEditorUtil.AddBox(root, new Vector2(size.x * 0.9f, size.y * 0.85f), new Vector2(0f, -0.5f + size.y * 0.5f), true);
            GGEditorUtil.AddKinematic(root);
            root.AddComponent<GGHazard>();
            GGRailMover rm = root.AddComponent<GGRailMover>();
            GGEditorUtil.SetMany(rm, "travel", new Vector2(5f, 0f), "beatsPerCycle", 8f, "offsetBeats", 0f, "smooth", true);
            GGEditorUtil.SavePrefab(root, DirHazards, "SpikePlatform_Moving");
        }

        static void SpikedBall(string name, float length)
        {
            GameObject root = new GameObject(name); // raiz = PIVÔ (ponto de fixação)
            GameObject arm = GGEditorUtil.NewChild(root.transform, "Arm", Vector3.zero);
            GameObject ball = GGEditorUtil.NewChild(arm.transform, "Ball", new Vector3(0f, -length, 0f));
            GGEditorUtil.AddSprite(ball.transform, "Sprite", "spiked_ball", 1.1f, 0f, Anchor.Center, 2);
            GGEditorUtil.AddCircle(ball, 0.48f, Vector2.zero, true);
            GGEditorUtil.AddKinematic(ball);
            ball.AddComponent<GGHazard>();

            LineRenderer lr = root.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.12f;
            lr.startColor = new Color(0.55f, 0.55f, 0.62f, 1f);
            lr.endColor = new Color(0.55f, 0.55f, 0.62f, 1f);
            lr.sortingOrder = 1;

            GGPendulum pd = root.AddComponent<GGPendulum>();
            GGEditorUtil.SetMany(pd, "arm", arm.transform, "ball", ball.transform, "chain", lr,
                "maxAngle", 55f, "periodBeats", 4f, "offsetBeats", 0f);
            GGEditorUtil.SavePrefab(root, DirHazards, name);
        }

        static void GearRail()
        {
            GameObject root = new GameObject("Gear_Rail");
            GameObject spriteGo = GGEditorUtil.NewChild(root.transform, "Spin", Vector3.zero);
            GGEditorUtil.AddSprite(spriteGo.transform, "Sprite", "gear_hazard", 1.6f, 0f, Anchor.Center, 1);
            GGSpinner sp = spriteGo.AddComponent<GGSpinner>();
            GGEditorUtil.SetMany(sp, "degreesPerBeat", 90f, "pulsed", true);
            GGEditorUtil.AddCircle(root, 0.7f, Vector2.zero, true);
            GGEditorUtil.AddKinematic(root);
            root.AddComponent<GGHazard>();
            GGRailMover rm = root.AddComponent<GGRailMover>();
            GGEditorUtil.SetMany(rm, "travel", new Vector2(0f, 4f), "beatsPerCycle", 8f, "offsetBeats", 0f, "smooth", true);
            GGEditorUtil.SavePrefab(root, DirHazards, "Gear_Rail");
        }

        static void ZombieHandPrefab()
        {
            GameObject root = new GameObject("ZombieHand");
            Vector2 size;
            SpriteRenderer sr = GGEditorUtil.AddSprite(root.transform, "Sprite", "zombie_hand_up", 1.4f, 0f, Anchor.BottomOfCell, 0, out size);
            GGEditorUtil.AddBox(root, new Vector2(0.55f, 1.0f), new Vector2(0f, -0.5f + 0.55f), true);
            GGZombieHand zh = root.AddComponent<GGZombieHand>();
            GGEditorUtil.SetMany(zh, "sr", sr,
                "moundSprite", GGEditorUtil.LoadSprite("zombie_hand_mound"),
                "risingSprite", GGEditorUtil.LoadSprite("zombie_hand_rising"),
                "upSprite", GGEditorUtil.LoadSprite("zombie_hand_up"),
                "periodBeats", 6f, "offsetBeats", 0f, "proximityOnly", false);
            GGEditorUtil.SavePrefab(root, DirHazards, "ZombieHand");
        }

        // ================================================================== interativos
        static void CreateInteractables()
        {
            // Checkpoint
            {
                GameObject root = new GameObject("Checkpoint_Flag");
                Vector2 size;
                SpriteRenderer flag = GGEditorUtil.AddSprite(root.transform, "Flag", "checkpoint_flag", 2.2f, 0f, Anchor.BottomOfCell, 1, out size);
                GameObject rp = GGEditorUtil.NewChild(root.transform, "Respawn", new Vector3(0f, -0.35f, 0f));
                GGEditorUtil.AddBox(root, new Vector2(1.4f, 2.4f), new Vector2(0f, -0.5f + 1.2f), true);
                GGCheckpoint cp = root.AddComponent<GGCheckpoint>();
                GGEditorUtil.SetMany(cp, "respawnPoint", rp.transform, "flag", flag);
                GGEditorUtil.SavePrefab(root, DirInteract, "Checkpoint_Flag");
            }

            ResonanceTarget("Resonance_Lantern", "lantern", 1.5f);
            ResonanceTarget("Resonance_Tombstone", "tombstone", 1.5f);

            // Portão de ferro (arena do boss e porta de ressonância)
            {
                GameObject root = new GameObject("Gate_Iron");
                GameObject leaf = GGEditorUtil.NewChild(root.transform, "Leaf", Vector3.zero);
                Vector2 size;
                GGEditorUtil.AddSprite(leaf.transform, "Sprite", "iron_gate", 4.2f, 0f, Anchor.BottomOfCell, 1, out size);
                BoxCollider2D blocker = GGEditorUtil.AddBox(leaf, new Vector2(size.x * 0.85f, size.y), new Vector2(0f, -0.5f + size.y * 0.5f), false);
                GGGate g = root.AddComponent<GGGate>();
                GGEditorUtil.SetMany(g, "leaf", leaf.transform, "blocker", blocker,
                    "openOffset", new Vector3(0f, 4.6f, 0f), "speed", 7f, "startClosed", false);
                GGEditorUtil.SavePrefab(root, DirInteract, "Gate_Iron");
            }

            // Porta de ressonância (lógica; ligue Targets + Gate no Inspector)
            {
                GameObject root = new GameObject("Door_Resonance");
                root.AddComponent<GGResonanceDoor>();
                GGEditorUtil.SavePrefab(root, DirInteract, "Door_Resonance");
            }
        }

        static void ResonanceTarget(string name, string sprite, float fitH)
        {
            GameObject root = new GameObject(name);
            Vector2 size;
            SpriteRenderer sr = GGEditorUtil.AddSprite(root.transform, "Sprite", sprite, fitH, 0f, Anchor.BottomOfCell, 0, out size);
            GGEditorUtil.AddBox(root, new Vector2(Mathf.Max(0.9f, size.x * 1.1f), size.y), new Vector2(0f, -0.5f + size.y * 0.5f), true);
            GGResonanceTarget rt = root.AddComponent<GGResonanceTarget>();
            GGEditorUtil.SetMany(rt, "sr", sr, "holdBeats", 10f);
            GGEditorUtil.SavePrefab(root, DirInteract, name);
        }

        // ================================================================== FX do boss
        static void CreateFx()
        {
            // Onda de choque (corre pelo chão, perfura)
            {
                GameObject root = new GameObject("BossShockwave");
                GGEditorUtil.AddSprite(root.transform, "Sprite", "green_flame", 1.2f, 0f, Anchor.Center, 3);
                GGEditorUtil.AddBox(root, new Vector2(0.8f, 1.0f), Vector2.zero, true);
                GGEditorUtil.AddKinematic(root);
                GGBossProjectile bp = root.AddComponent<GGBossProjectile>();
                GGEditorUtil.SetMany(bp, "damage", 15, "piercing", true, "shootable", false, "rotateToVelocity", false);
                GGEditorUtil.SavePrefab(root, DirFx, "BossShockwave");
            }
            // Orb (pode ser destruído pela nota do Player)
            {
                GameObject root = new GameObject("BossOrb");
                GGEditorUtil.AddSprite(root.transform, "Sprite", "ghost_flame", 0.95f, 0f, Anchor.Center, 3);
                GGEditorUtil.AddCircle(root, 0.35f, Vector2.zero, true);
                GGEditorUtil.AddKinematic(root);
                GGBossProjectile bp = root.AddComponent<GGBossProjectile>();
                GGEditorUtil.SetMany(bp, "damage", 15, "piercing", false, "shootable", true, "rotateToVelocity", true);
                GGEditorUtil.SavePrefab(root, DirFx, "BossOrb");
            }
        }

        // ================================================================== inimigos
        static GGSpriteAnimator.Clip MakeClip(string name, float fps, params string[] frames)
        {
            GGSpriteAnimator.Clip c = new GGSpriteAnimator.Clip();
            c.name = name;
            c.fps = fps;
            c.loop = true;
            c.frames = new Sprite[frames.Length];
            for (int i = 0; i < frames.Length; i++) c.frames[i] = GGEditorUtil.LoadSprite(frames[i]);
            return c;
        }

        static void CreateEnemies()
        {
            GameObject note = GGEditorUtil.LoadPrefab("Pickup_Note");

            // Corvo
            {
                GameObject root = new GameObject("Enemy_Crow");
                SpriteRenderer sr = GGEditorUtil.AddSprite(root.transform, "Sprite", "crow_idle_0", 0f, 0f, Anchor.BottomOfCell, 2);
                GGEditorUtil.AddCircle(root, 0.38f, new Vector2(0f, -0.1f), true);
                GGEditorUtil.AddKinematic(root);
                GGSpriteAnimator anim = root.AddComponent<GGSpriteAnimator>();
                anim.Setup(sr, new[]
                {
                    MakeClip("Idle", 6f, "crow_idle_0", "crow_idle_1", "crow_idle_2", "crow_idle_3"),
                    MakeClip("Caw", 6f, "crow_flyup"),
                    MakeClip("Fly", 10f, "crow_fly_0", "crow_fly_1", "crow_fly_2", "crow_fly_3"),
                    MakeClip("Dive", 6f, "crow_dive"),
                });
                GGCrow crow = root.AddComponent<GGCrow>();
                GGEditorUtil.SetMany(crow, "maxHealth", 20, "contactDamage", 15, "visual", sr.transform,
                    "spriteRenderer", sr, "animator", anim, "dropPrefab", note, "dropChance", 0.3f);
                GGEditorUtil.SavePrefab(root, DirEnemies, "Enemy_Crow");
            }

            // Lobo (raiz nos PÉS)
            {
                GameObject root = new GameObject("Enemy_Wolf");
                SpriteRenderer sr = GGEditorUtil.AddSprite(root.transform, "Sprite", "wolf_run_0", 0f, 0f, Anchor.FeetAtRoot, 2);
                GGEditorUtil.AddBox(root, new Vector2(1.25f, 0.85f), new Vector2(0f, 0.45f), true);
                GGEditorUtil.AddKinematic(root);
                GGSpriteAnimator anim = root.AddComponent<GGSpriteAnimator>();
                anim.Setup(sr, new[]
                {
                    MakeClip("Run", 10f, "wolf_run_0", "wolf_run_1", "wolf_run_2", "wolf_run_3"),
                    MakeClip("Howl", 6f, "wolf_howl"),
                    MakeClip("Crouch", 6f, "wolf_crouch"),
                });
                GGWolf wolf = root.AddComponent<GGWolf>();
                GGEditorUtil.SetMany(wolf, "maxHealth", 40, "contactDamage", 30, "visual", sr.transform,
                    "spriteRenderer", sr, "animator", anim, "dropPrefab", note, "dropChance", 0.5f);
                GGEditorUtil.SavePrefab(root, DirEnemies, "Enemy_Wolf");
            }

            // Fantasma
            {
                GameObject root = new GameObject("Enemy_Ghost");
                SpriteRenderer sr = GGEditorUtil.AddSprite(root.transform, "Sprite", "ghost", 1.3f, 0f, Anchor.Center, 3);
                GGEditorUtil.AddCircle(root, 0.45f, Vector2.zero, true);
                GGEditorUtil.AddKinematic(root);
                GGGhost ghost = root.AddComponent<GGGhost>();
                GGEditorUtil.SetMany(ghost, "maxHealth", 10, "contactDamage", 15, "visual", sr.transform,
                    "spriteRenderer", sr, "dropPrefab", note, "dropChance", 0.2f);
                GGEditorUtil.SavePrefab(root, DirEnemies, "Enemy_Ghost");
            }
        }

        // ================================================================== boss
        static void CreateBoss()
        {
            string path = DirBoss + "/" + BossPrefabName + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log("[Fase3] " + BossPrefabName + " já existe e foi MANTIDO (para não apagar seu sprite/animações). "
                          + "Para recriar, apague " + path + " e rode o passo 1 de novo.");
                return;
            }

            UnityEditor.Animations.AnimatorController ctrl = GGBossAnimatorFactory.GetOrCreate();

            GameObject root = new GameObject(BossPrefabName); // raiz = PÉS do boss
            GGEditorUtil.AddKinematic(root);
            GGEditorUtil.AddBox(root, new Vector2(1.6f, 3.6f), new Vector2(0f, 1.8f), true); // hurtbox / dano de contato

            // Visual: origem nos pés; o código vira o scale.x aqui (NÃO anime o scale.x do Visual).
            GameObject visual = GGEditorUtil.NewChild(root.transform, "Visual", Vector3.zero);
            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;

            // Body: aqui fica o SEU sprite (troque o sprite do SpriteRenderer) e é o objeto que você anima.
            Vector2 size;
            SpriteRenderer body = GGEditorUtil.AddSprite(visual.transform, "Body", "boss_placeholder", 0f, 0f, Anchor.FeetAtRoot, 2, out size);

            GameObject muzzle = GGEditorUtil.NewChild(root.transform, "Muzzle", new Vector3(1.0f, 2.4f, 0f));
            GameObject barGo = GGEditorUtil.NewChild(root.transform, "HealthBar", new Vector3(0f, 5.0f, 0f));
            GGWorldHealthBar bar = barGo.AddComponent<GGWorldHealthBar>();

            GGBoss boss = root.AddComponent<GGBoss>();
            GGEditorUtil.SetMany(boss,
                "bossName", "Groove King",
                "visual", visual.transform,
                "animator", animator,
                "muzzle", muzzle.transform,
                "healthBar", bar,
                "spriteFacesRight", true,
                "zombieHandPrefab", GGEditorUtil.LoadPrefab("ZombieHand"),
                "shockwavePrefab", GGEditorUtil.LoadPrefab("BossShockwave"),
                "orbPrefab", GGEditorUtil.LoadPrefab("BossOrb"),
                "ghostPrefab", GGEditorUtil.LoadPrefab("Enemy_Ghost"),
                "healNotePrefab", GGEditorUtil.LoadPrefab("Pickup_Note"),
                "fragmentPrefab", GGEditorUtil.LoadPrefab("Pickup_Fragment"));

            GGEditorUtil.SavePrefab(root, DirBoss, BossPrefabName);
        }

        static void CreateArena()
        {
            GameObject root = new GameObject("BossArena"); // raiz = centro da arena, na altura do chão
            GGEditorUtil.AddBox(root, new Vector2(36f, 16f), new Vector2(0f, 6f), true); // y de -2 a 14: a câmera trava com o chão acima da borda de baixo
            GGEditorUtil.NewChild(root.transform, "LeftLimit", new Vector3(-16.5f, 0f, 0f));
            GGEditorUtil.NewChild(root.transform, "RightLimit", new Vector3(16.5f, 0f, 0f));
            root.AddComponent<GGBossArena>();
            GGEditorUtil.SavePrefab(root, DirBoss, "BossArena");
        }
    }
}
