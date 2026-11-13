namespace PopAdventure.MayhemTheatre.EditorTools
{
    /// <summary>
    /// Geometria base da Fase 4 (em células/unidades; chão com a superfície em y = 0).
    /// 5 seções lado a lado: Fase1 0-44 | Fase2 44-88 | Fase3 88-132 | Fase4 132-176 | Arena 176-212.
    /// Limites de salto usados: pulo 3-4 un. de altura, 5 un/s, buracos de até 3 células, degraus de até 2.
    /// </summary>
    public static class MTLevelLayout
    {
        public const int GroundRows = 5;
        public static readonly int[] Starts = { 0, 44, 88, 132, 176 };
        public static readonly int[] Ends = { 44, 88, 132, 176, 212 };

        /// <summary>Trechos de chão: { x0, x1 } (x1 exclusivo).</summary>
        public static int[][] Ground(MTMoment m)
        {
            switch (m)
            {
                case MTMoment.Fase1: return new[] { new[] { 0, 13 }, new[] { 21, 44 } };
                case MTMoment.Fase2: return new[] { new[] { 44, 52 }, new[] { 66, 88 } };
                case MTMoment.Fase3: return new[] { new[] { 88, 100 }, new[] { 103, 115 }, new[] { 118, 132 } };
                case MTMoment.Fase4: return new[] { new[] { 132, 150 }, new[] { 153, 176 } };
                default: return new[] { new[] { 176, 212 } };
            }
        }

        /// <summary>Plataformas de 1 célula de espessura: { xEsquerdo, yDoTopo, largura }.</summary>
        public static int[][] Platforms(MTMoment m)
        {
            switch (m)
            {
                case MTMoment.Fase1: return new[] { new[] { 24, 2, 4 }, new[] { 30, 4, 3 }, new[] { 35, 2, 4 } };
                case MTMoment.Fase2: return new[] { new[] { 68, 2, 4 }, new[] { 74, 4, 3 }, new[] { 80, 2, 4 }, new[] { 85, 3, 3 } };
                case MTMoment.Fase3: return new[] { new[] { 92, 2, 3 }, new[] { 108, 2, 3 }, new[] { 121, 2, 3 }, new[] { 126, 3, 3 } };
                case MTMoment.Fase4: return new[] { new[] { 137, 2, 4 }, new[] { 144, 3, 3 }, new[] { 156, 2, 3 }, new[] { 162, 4, 3 }, new[] { 167, 2, 4 } };
                default: return new[] { new[] { 181, 2, 4 }, new[] { 192, 3, 4 }, new[] { 203, 2, 4 } };
            }
        }
    }
}
