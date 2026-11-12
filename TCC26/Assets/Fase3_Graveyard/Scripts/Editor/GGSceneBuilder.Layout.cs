using UnityEngine;
using Object = UnityEngine.Object;

namespace PopAdventure.Fase3.EditorTools
{
    /// <summary>
    /// Layout base da fase (~265 unidades). Todas as coordenadas são do MUNDO; o chão principal fica em y = 0.
    /// A  entrada (0–45)         corvos, espinhos fixos e rítmicos, plataforma de espinhos
    /// B  ponte rítmica (45–77)  plataformas que piscam / sobem / trilho sobre o abismo
    /// C  cripta (77–107)        mãos de zumbi, lobo, checkpoint
    /// D  escalada (107–137)     pêndulos de espinhos e plataformas que sobem
    /// E  platô (137–177)        fantasmas, lobo, serra, área secreta
    /// F  descida (177–213)      degraus, puzzle de ressonância (3 lanternas abrem o portão), checkpoint
    /// G  arena do boss (213–249)
    /// </summary>
    public partial class GGSceneBuilder
    {
        void BuildEmptyLayout()
        {
            Run(-8, 60, 0);
        }

        void BuildLayout()
        {
            // ---------------------------------------------------------------- terreno (colunas = células)
            Run(-8, 44, 0);        // A
            Run(77, 105, 0);       // C (coluna 105 tem rampa por cima)
            Run(106, 106, 1);      // rampa 2
            Run(107, 114, 2);      // landing
            Run(137, 176, 10);     // E platô
            Run(177, 182, 8);      // F degraus
            Run(183, 188, 6);
            Run(189, 194, 4);
            Run(195, 205, 2);      // puzzle
            Run(206, 256, 0);      // arena

            LayoutA();
            LayoutB();
            LayoutC();
            LayoutD();
            LayoutE();
            LayoutF();
            LayoutBoss();
        }

        // ================================================================== A — entrada
        void LayoutA()
        {
            Many("Prop_DeadTree", decor, -4f, 12f, 37f);
            Many("Prop_StreetLamp", decor, 3f, 20f, 33f);
            Many("Prop_Tombstone", decor, 6f, 9f, 15.5f, 29f);
            Many("Prop_GraveCross", decor, 8f, 23.5f);
            Many("Prop_IronFence", decor, 10.2f, 11.6f, 13.0f);
            Many("Prop_Bench", decor, 1.5f);
            Many("Prop_Skull", decor, 16f);
            Many("Prop_Bones", decor, 27.5f);
            Many("Prop_Rubble", decor, 28.5f);
            Many("Prop_AngelStatue", decor, 42f);
            Many("Prop_Lantern", decor, 5f, 35.5f);
            OnGround("Crate_Wooden", decor, 31f);

            Stand("Spikes_Small", hazards, 18f, 0f);
            Stand("Spikes_Large", hazards, 25f, 0f);
            GameObject t1 = Stand("SpikeTrap_Rhythm", hazards, 34f, 0f);
            GameObject t2 = Stand("SpikeTrap_Rhythm", hazards, 36f, 0f);
            Cfg<GGSpikeTrap>(t1, "offsetBeats", 0f);
            Cfg<GGSpikeTrap>(t2, "offsetBeats", 2f);
            GameObject sp = Stand("SpikePlatform_Moving", hazards, 40f, 0f);
            Cfg<GGRailMover>(sp, "travel", new Vector2(3.5f, 0f));

            OnGround("Enemy_Crow", enemies, 14f);
            OnGround("Enemy_Crow", enemies, 38.5f);
            GameObject hc = Mid("Enemy_Crow", enemies, 30f, 3.6f);
            Cfg<GGCrow>(hc, "hoverMode", true);

            Vinyls(17f, 1.6f, 18f, 2.4f, 19f, 1.6f, 24f, 2.0f, 25f, 2.8f, 26f, 2.0f,
                   21.8f, 4.3f, 22.5f, 4.3f, 23.2f, 4.3f, 27.8f, 6.3f, 28.5f, 6.3f, 29.2f, 6.3f);
            Mid("Pickup_Note", pickups, 32.5f, 1.2f);
        }

        // ================================================================== B — ponte rítmica
        void LayoutB()
        {
            OnGround("Prop_StonePillar", decor, 44f);
            OnGround("Prop_StonePillar", decor, 78.2f);

            float[] bx = { 49f, 53f, 57f, 61f };
            float[] bs = { 0f, 1f, 2f, 1f };
            for (int i = 0; i < bx.Length; i++)
            {
                GameObject p = Hang("Platform_Glowing_Blink", rhythm, bx[i], bs[i]);
                Cfg<GGRhythmPlatform>(p, "periodBeats", 4, "onBeats", 3, "offsetBeats", (float)i);
                Vinyls(bx[i], bs[i] + 1.8f);
            }

            GameObject r1 = Hang("Platform_Glowing_Rise", rhythm, 65f, 1f);
            Cfg<GGRhythmPlatform>(r1, "periodBeats", 4, "onBeats", 2, "offsetBeats", 0f);
            Vinyls(65f, 2.9f);

            GameObject rail = Hang("Platform_Hanging_Rail", rhythm, 70f, 0f);
            Cfg<GGRailMover>(rail, "travel", new Vector2(5f, 0f), "beatsPerCycle", 8f);
            Vinyls(72.5f, 1.9f);

            GameObject c1 = Mid("Enemy_Crow", enemies, 57f, 5.2f);
            GameObject c2 = Mid("Enemy_Crow", enemies, 67f, 4.2f);
            Cfg<GGCrow>(c1, "hoverMode", true);
            Cfg<GGCrow>(c2, "hoverMode", true);
        }

        // ================================================================== C — cripta
        void LayoutC()
        {
            Stand("Checkpoint_Flag", interact, 79f, 0f);

            Many("Prop_StoneArch", decor, 83f, 99f);
            Many("Prop_StonePillar", decor, 86f, 94f);
            Many("Prop_GothicWindow", decor, 90f);
            Many("Prop_Lantern", decor, 82f, 91.5f, 97f);
            Many("Prop_Skull", decor, 87.5f);
            Many("Prop_Bones", decor, 95.5f);
            Many("Prop_Bench", decor, 80.8f);

            float[] hx = { 84f, 88f, 92f };
            float[] ho = { 0f, 2f, 4f };
            for (int i = 0; i < hx.Length; i++)
            {
                GameObject h = OnGround("ZombieHand", hazards, hx[i]);
                Cfg<GGZombieHand>(h, "periodBeats", 6f, "offsetBeats", ho[i]);
            }
            GameObject hp = OnGround("ZombieHand", hazards, 96.5f);
            Cfg<GGZombieHand>(hp, "periodBeats", 6f, "offsetBeats", 1f, "proximityOnly", true);

            GameObject wolf = Feet("Enemy_Wolf", enemies, 101f, 0f);
            Cfg<GGWolf>(wolf, "patrolRange", 3f);

            Vinyls(84.5f, 2.4f, 92.5f, 2.4f, 89.5f, 4.3f, 95.5f, 4.3f);
            Mid("Pickup_Note", pickups, 103.5f, 1.2f);

            // topo da rampa e landing
            Mid("Pickup_Heart", pickups, 111f, 3.3f);
            Many("Prop_Tombstone", decor, 108.5f, 110.5f);
            Stand("Spikes_Small", hazards, 113f, 2f);
        }

        // ================================================================== D — escalada
        void LayoutD()
        {
            GameObject pend1 = Mid("SpikedBall_Swing_Long", hazards, 122.5f, 10.5f);
            Cfg<GGPendulum>(pend1, "offsetBeats", 0f);

            GameObject r1 = Hang("Platform_Glowing_Rise", rhythm, 126f, 5f);
            Cfg<GGRhythmPlatform>(r1, "periodBeats", 4, "onBeats", 2, "offsetBeats", 0f);
            GameObject r2 = Hang("Platform_Glowing_Rise", rhythm, 130f, 7f);
            Cfg<GGRhythmPlatform>(r2, "periodBeats", 4, "onBeats", 2, "offsetBeats", 2f);

            GameObject pend2 = Mid("SpikedBall_Swing_Short", hazards, 130f, 11.5f);
            Cfg<GGPendulum>(pend2, "offsetBeats", 2f);

            GameObject crow = Mid("Enemy_Crow", enemies, 119f, 8.5f);
            Cfg<GGCrow>(crow, "hoverMode", true);

            Vinyls(118f, 5.3f, 122.5f, 5.3f, 126f, 6.4f, 130f, 8.4f, 134f, 9.3f);
        }

        // ================================================================== E — platô
        void LayoutE()
        {
            Stand("Checkpoint_Flag", interact, 139f, 10f);

            Many("Prop_DeadTree", decor, 142f, 173f);
            Many("Prop_AngelStatue", decor, 146.5f);
            Many("Prop_StreetLamp", decor, 141f, 154f, 175f);
            Many("Prop_Tombstone", decor, 148f, 150f, 165f);
            Many("Prop_GraveBanner", decor, 156f);
            Many("Prop_Fountain", decor, 152f);
            Many("Prop_Mausoleum", decor, 170f);
            Many("Prop_Lantern", decor, 143f, 168f);

            GameObject gear = Mid("Gear_Rail", hazards, 144.8f, 11.3f);
            Cfg<GGRailMover>(gear, "travel", new Vector2(0f, 3.2f), "beatsPerCycle", 8f);

            GameObject wolf = Feet("Enemy_Wolf", enemies, 151f, 10f);
            Cfg<GGWolf>(wolf, "patrolRange", 3f);

            GameObject h1 = OnGround("ZombieHand", hazards, 156.5f);
            GameObject h2 = OnGround("ZombieHand", hazards, 158.5f);
            Cfg<GGZombieHand>(h1, "periodBeats", 6f, "offsetBeats", 0f);
            Cfg<GGZombieHand>(h2, "periodBeats", 6f, "offsetBeats", 3f);

            GameObject s1 = Stand("SpikeTrap_Rhythm", hazards, 163f, 10f);
            GameObject s2 = Stand("SpikeTrap_Rhythm", hazards, 164.6f, 10f);
            GameObject s3 = Stand("SpikeTrap_Rhythm", hazards, 166.2f, 10f);
            Cfg<GGSpikeTrap>(s1, "offsetBeats", 0f);
            Cfg<GGSpikeTrap>(s2, "offsetBeats", 2f);
            Cfg<GGSpikeTrap>(s3, "offsetBeats", 0f);

            Mid("Enemy_Ghost", enemies, 160f, 12.8f);
            Mid("Enemy_Ghost", enemies, 168f, 13.2f);
            OnGround("Enemy_Crow", enemies, 144f);
            GameObject hc = Mid("Enemy_Crow", enemies, 156f, 14.5f);
            Cfg<GGCrow>(hc, "hoverMode", true);

            // área secreta no alto
            Vinyls(159.5f, 13.3f, 163.5f, 15.3f, 164.5f, 15.3f, 165.5f, 15.3f, 166.5f, 15.3f,
                   149f, 11.5f, 153f, 11.5f, 140.5f, 11.5f, 148f, 11.5f);
            Mid("Pickup_Heart", pickups, 165f, 16.6f);
            Mid("Pickup_Note", pickups, 175.5f, 11.2f);
        }

        // ================================================================== F — descida + puzzle de ressonância
        void LayoutF()
        {
            Stand("Checkpoint_Flag", interact, 179f, 8f);

            GameObject t1 = Stand("SpikeTrap_Rhythm", hazards, 180.8f, 8f);
            GameObject t2 = Stand("SpikeTrap_Rhythm", hazards, 182.2f, 8f);
            Cfg<GGSpikeTrap>(t1, "offsetBeats", 0f);
            Cfg<GGSpikeTrap>(t2, "offsetBeats", 2f);
            GameObject hc = Mid("Enemy_Crow", enemies, 181f, 11.8f);
            Cfg<GGCrow>(hc, "hoverMode", true);

            Stand("Spikes_Large", hazards, 186f, 6f);
            Mid("Enemy_Ghost", enemies, 185f, 8.8f);
            Mid("Pickup_Note", pickups, 188f, 7.2f);

            GameObject h1 = OnGround("ZombieHand", hazards, 191.5f);
            GameObject h2 = OnGround("ZombieHand", hazards, 193.5f);
            Cfg<GGZombieHand>(h1, "periodBeats", 6f, "offsetBeats", 0f);
            Cfg<GGZombieHand>(h2, "periodBeats", 6f, "offsetBeats", 3f);
            Many("Prop_StoneArch", decor, 180f, 197f);
            Many("Prop_GraveBanner", decor, 190.5f);
            Vinyls(178.5f, 9.4f, 191.5f, 5.4f, 193.5f, 5.4f, 185f, 7.4f, 187f, 7.4f);

            // puzzle: acenda as 3 lanternas ao mesmo tempo (a nota mantém acesa por 10 batidas) para abrir o portão
            Many("Prop_StreetLamp", decor, 196.3f, 202.2f);
            GameObject l1 = Stand("Resonance_Lantern", interact, 197.8f, 2f);
            GameObject l2 = Stand("Resonance_Tombstone", interact, 199.6f, 2f);
            GameObject l3 = Stand("Resonance_Lantern", interact, 201.4f, 2f);
            GameObject gate = Stand("Gate_Iron", interact, 204f, 2f);
            Cfg<GGGate>(gate, "startClosed", true);
            GameObject door = Mid("Door_Resonance", interact, 199.6f, 3f);
            GGResonanceDoor rd = Comp<GGResonanceDoor>(door);
            if (rd != null)
            {
                GGEditorUtil.SetArray(rd, "targets", new Object[]
                {
                    Comp<GGResonanceTarget>(l1), Comp<GGResonanceTarget>(l2), Comp<GGResonanceTarget>(l3)
                });
                GGEditorUtil.Set(rd, "gate", Comp<GGGate>(gate));
            }
            Mid("Enemy_Ghost", enemies, 199.6f, 5.2f);
            Vinyls(197.8f, 4.2f, 199.6f, 4.2f, 201.4f, 4.2f);

            // antes da arena
            Stand("Checkpoint_Flag", interact, 207f, 0f);
            Mid("Pickup_Note", pickups, 208.6f, 1.2f);
            Mid("Pickup_Note", pickups, 209.6f, 1.2f);
            Many("Prop_Tombstone", decor, 207.8f);
        }

        // ================================================================== G — arena do boss
        void LayoutBoss()
        {
            GameObject gate = Stand("Gate_Iron", bossFight, 211.5f, 0f);
            GameObject arena = Mid("BossArena", bossFight, 231f, 0f);
            GameObject boss = Feet(GGPrefabFactory.BossPrefabName, bossFight, 243f, 0f);

            Transform left = arena != null ? arena.transform.Find("LeftLimit") : null;
            Transform right = arena != null ? arena.transform.Find("RightLimit") : null;
            GGBoss bossComp = Comp<GGBoss>(boss);
            if (bossComp != null) GGEditorUtil.SetMany(bossComp, "leftLimit", left, "rightLimit", right);
            GGBossArena arenaComp = Comp<GGBossArena>(arena);
            if (arenaComp != null)
            {
                GGEditorUtil.Set(arenaComp, "boss", bossComp);
                GGEditorUtil.SetArray(arenaComp, "gates", new Object[] { Comp<GGGate>(gate) });
            }

            Many("Prop_StoneArch", decor, 231f);
            Many("Prop_StonePillar", decor, 216f, 246f);
            Many("Prop_GothicWindow", decor, 224f, 238f);
            Many("Prop_StreetLamp", decor, 220f, 242f);
            Many("Prop_Lantern", decor, 218.5f, 244f);
        }

        // ================================================================== tiles extras (depois do terreno)
        void FinishLayout()
        {
            if (!opt.generateLayout) return;

            // paredes: limite esquerdo e fim da arena
            FillRect(tmGround, -12, BottomY, -9, 18, "Stone_Block");
            FillRect(tmGround, 249, BottomY, 254, 18, "Stone_Block");

            // rampas (subindo para a direita)
            Paint(tmGround, 105, 0, "Ramp_Up_Right");
            Paint(tmGround, 106, 1, "Ramp_Up_Right");

            // plataformas finas (one-way)
            PlatTiles(21, 23, 3);
            PlatTiles(27, 29, 5);
            PlatTiles(88, 90, 3);
            PlatTiles(94, 96, 3);
            PlatTiles(117, 118, 4);
            PlatTiles(121, 123, 4);
            PlatTiles(133, 134, 8);
            PlatTiles(158, 160, 12);
            PlatTiles(163, 166, 14);

            // parede de fundo da cripta (atrás de tudo, mais escura)
            FillRect(tmBackground, 80, 0, 103, 8, "Wall_Brick");
        }
    }
}
