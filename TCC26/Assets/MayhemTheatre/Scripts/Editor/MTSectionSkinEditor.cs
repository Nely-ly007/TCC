using UnityEditor;
using UnityEngine;

namespace PopAdventure.MayhemTheatre.EditorTools
{
    [CustomEditor(typeof(MTSectionSkin))]
    public class MTSectionSkinEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Arraste Tiles da Tile Palette da fase correspondente nos campos acima e clique no botão. " +
                "Campo vazio = tile padrão da seção. Isto reescreve só os Tilemaps Ground e Platforms (e Background, se houver tile de fundo).",
                MessageType.Info);
            if (GUILayout.Button("Reaplicar tiles desta seção"))
                MTLevelBuilder.ReapplySection((MTSectionSkin)target);
        }
    }
}
