using UnityEditor;
using UnityEngine;

namespace PopAdventure.Hive.EditorTools
{
    /// <summary>Cria marcadores já no grupo certo de [Markers] com os valores padrão do tipo.</summary>
    public static class HiveMarkerFactory
    {
        public static string GroupFor(HiveMarkerType t)
        {
            switch (t)
            {
                case HiveMarkerType.PlayerSpawn:
                case HiveMarkerType.Checkpoint:
                    return "00_Spawn_Checkpoints";
                case HiveMarkerType.BeatPlatform:
                case HiveMarkerType.MovingPlatform:
                case HiveMarkerType.CrumblePlatform:
                    return "01_Platforms";
                case HiveMarkerType.HoneyPool:
                case HiveMarkerType.HoneyDrip:
                case HiveMarkerType.KillZone:
                case HiveMarkerType.WaxThorns:
                    return "02_Hazards";
                case HiveMarkerType.VinylRow:
                case HiveMarkerType.HealNote:
                    return "03_Pickups";
                case HiveMarkerType.WorkerBee:
                case HiveMarkerType.HornetGuard:
                    return "04_Enemies";
                case HiveMarkerType.BossArena:
                case HiveMarkerType.LevelExit:
                    return "05_Boss_Exit";
                case HiveMarkerType.MusicCue:
                    return "06_Music";
                default:
                    return "99_Blockout";
            }
        }

        public static HiveMarker Create(HiveMarkerType type, Vector3 position, bool select)
        {
            Transform parent = HiveSceneBuilder.GetMarkerGroup(GroupFor(type));

            var go = new GameObject(type.ToString());
            Undo.RegisterCreatedObjectUndo(go, "Criar marcador " + type);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);

            var m = go.AddComponent<HiveMarker>();
            m.type = type;
            m.ApplyDefaults();

            if (select) Selection.activeGameObject = go;
            return m;
        }

        public static Vector3 Snap(Vector3 p, float step)
        {
            if (step <= 0f) return p;
            return new Vector3(Mathf.Round(p.x / step) * step, Mathf.Round(p.y / step) * step, 0f);
        }
    }
}
