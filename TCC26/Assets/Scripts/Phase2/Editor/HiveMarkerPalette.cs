using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PopAdventure.Hive.EditorTools
{
    /// <summary>Janela com um botão por tipo de marcador. Cria no centro da Scene View (com snap).</summary>
    public class HiveMarkerPalette : EditorWindow
    {
        private float snap = 0.5f;
        private Vector2 scroll;
        private string stats = "";

        [MenuItem("Pop Adventure/Fase 2/Paleta de Marcadores", false, 1)]
        public static void Open()
        {
            var w = GetWindow<HiveMarkerPalette>("Hive Marcadores");
            w.minSize = new Vector2(320f, 400f);
        }

        private static readonly HiveMarkerType[][] Sections =
        {
            new[] { HiveMarkerType.PlayerSpawn, HiveMarkerType.Checkpoint },
            new[] { HiveMarkerType.BeatPlatform, HiveMarkerType.MovingPlatform, HiveMarkerType.CrumblePlatform },
            new[] { HiveMarkerType.HoneyPool, HiveMarkerType.HoneyDrip, HiveMarkerType.WaxThorns, HiveMarkerType.KillZone },
            new[] { HiveMarkerType.VinylRow, HiveMarkerType.HealNote },
            new[] { HiveMarkerType.WorkerBee, HiveMarkerType.HornetGuard },
            new[] { HiveMarkerType.BossArena, HiveMarkerType.LevelExit },
            new[] { HiveMarkerType.MusicCue },
            new[] { HiveMarkerType.BlockoutBlock },
        };

        private static readonly string[] SectionNames =
        {
            "Início e checkpoints", "Plataformas", "Perigos", "Coletáveis", "Inimigos", "Boss e saída", "Música", "Teste"
        };

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Clique num botão: o marcador nasce no centro da Scene View, dentro de [Markers]. " +
                "Depois ajuste no Inspector e arraste para posicionar. Em Play, o HiveLevelBootstrap cria os objetos reais.",
                MessageType.Info);

            snap = EditorGUILayout.FloatField("Snap (unidades, 0 = off)", snap);
            if (GUILayout.Button("Construir hierarquia na cena atual")) HiveSceneBuilder.BuildHierarchy(true);

            var wrap = new GUIStyle(EditorStyles.miniLabel);
            wrap.wordWrap = true;

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int s = 0; s < Sections.Length; s++)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField(SectionNames[s], EditorStyles.boldLabel);
                foreach (var t in Sections[s])
                {
                    if (GUILayout.Button(t.ToString(), GUILayout.Height(24f))) Spawn(t);
                    EditorGUILayout.LabelField(HiveMarker.Describe(t), wrap);
                }
            }

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("Contar marcadores / vinis da fase")) Count();
            if (!string.IsNullOrEmpty(stats)) EditorGUILayout.HelpBox(stats, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void Spawn(HiveMarkerType t)
        {
            Vector3 pos = Vector3.zero;
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) pos = sv.pivot;
            pos = HiveMarkerFactory.Snap(pos, snap);
            HiveMarkerFactory.Create(t, pos, true);
        }

        private void Count()
        {
            var all = Object.FindObjectsByType<HiveMarker>(FindObjectsSortMode.None);
            var counts = new Dictionary<HiveMarkerType, int>();
            int vinyls = 0;
            foreach (var m in all)
            {
                int c;
                counts.TryGetValue(m.type, out c);
                counts[m.type] = c + 1;
                if (m.type == HiveMarkerType.VinylRow) vinyls += Mathf.Max(1, m.count);
            }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Total de marcadores: " + all.Length);
            sb.AppendLine("Vinis na fase: " + vinyls);
            foreach (var kv in counts) sb.AppendLine(kv.Key + ": " + kv.Value);
            stats = sb.ToString();
        }
    }
}
