using UnityEditor;
using UnityEngine;

namespace PopAdventure.Hive.EditorTools
{
    /// <summary>Inspector enxuto: mostra só os campos que importam para o tipo + handles na Scene View.</summary>
    [CustomEditor(typeof(HiveMarker))]
    [CanEditMultipleObjects]
    public class HiveMarkerEditor : Editor
    {
        private void Prop(string name)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var typeProp = serializedObject.FindProperty("type");
            EditorGUILayout.PropertyField(typeProp);
            var t = (HiveMarkerType)typeProp.enumValueIndex;

            EditorGUILayout.HelpBox(HiveMarker.Describe(t), MessageType.None);

            switch (t)
            {
                case HiveMarkerType.BeatPlatform:
                    Prop("size"); Prop("pattern"); Prop("offset"); Prop("tier");
                    break;
                case HiveMarkerType.MovingPlatform:
                    Prop("size"); Prop("travel"); Prop("beats"); Prop("offset");
                    break;
                case HiveMarkerType.CrumblePlatform:
                    Prop("size"); Prop("beats"); Prop("count");
                    break;
                case HiveMarkerType.HoneyPool:
                case HiveMarkerType.KillZone:
                case HiveMarkerType.WaxThorns:
                    Prop("size");
                    break;
                case HiveMarkerType.HoneyDrip:
                    Prop("beats"); Prop("offset");
                    break;
                case HiveMarkerType.VinylRow:
                    Prop("count"); Prop("spacing"); Prop("arc");
                    break;
                case HiveMarkerType.WorkerBee:
                case HiveMarkerType.HornetGuard:
                    Prop("travel"); Prop("value");
                    break;
                case HiveMarkerType.BossArena:
                    Prop("size"); Prop("value");
                    break;
                case HiveMarkerType.BlockoutBlock:
                    Prop("size"); Prop("tier");
                    break;
                case HiveMarkerType.MusicCue:
                    Prop("size"); Prop("tier");   // tier 1 = LOOP PRINCIPAL · 2 = COMBATE
                    break;
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Aplicar valores padrão do tipo"))
            {
                foreach (var o in targets)
                {
                    var m = (HiveMarker)o;
                    Undo.RecordObject(m, "Valores padrão do marcador");
                    m.ApplyDefaults();
                    EditorUtility.SetDirty(m);
                }
            }
        }

        private void OnSceneGUI()
        {
            var m = (HiveMarker)target;

            if (m.type == HiveMarkerType.MovingPlatform)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 np = Handles.PositionHandle(m.TravelPoint, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m, "Mover ponto B");
                    m.travel = new Vector2(np.x - m.transform.position.x, np.y - m.transform.position.y);
                    EditorUtility.SetDirty(m);
                }
            }
            else if (m.type == HiveMarkerType.WorkerBee || m.type == HiveMarkerType.HornetGuard)
            {
                Vector3 p = m.transform.position;
                EditorGUI.BeginChangeCheck();
                Vector3 np = Handles.PositionHandle(p + new Vector3(m.travel.x * 0.5f, 0f, 0f), Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m, "Alcance da patrulha");
                    m.travel = new Vector2(Mathf.Max(0f, (np.x - p.x) * 2f), 0f);
                    EditorUtility.SetDirty(m);
                }
            }
        }
    }
}
