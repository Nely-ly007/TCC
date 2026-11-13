using UnityEngine;
using UnityEngine.Tilemaps;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Liga uma seção da Fase 4 à Tile Palette de outra fase.
    /// Arraste aqui os Tiles (da palette da Fase 1, 2 ou 3) e use o botão "Reaplicar tiles desta seção" no Inspector.
    /// Campo vazio = usa o tile greybox (seções 1-3) ou o tile do Mayhem Theatre (seção 4 e arena).
    /// Você também pode pintar à mão nos Tilemaps Background/Ground/Platforms/Foreground desta seção.
    /// </summary>
    public class MTSectionSkin : MonoBehaviour
    {
        public MTMoment moment;
        [TextArea(1, 3)] public string paletteNote = "";

        [Header("Tiles desta seção (opcional)")]
        public TileBase groundTopTile;
        public TileBase groundFillTile;
        public TileBase platformTile;
        public TileBase backgroundFillTile;
    }
}
