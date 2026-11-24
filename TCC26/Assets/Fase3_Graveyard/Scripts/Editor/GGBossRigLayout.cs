namespace PopAdventure.Fase3.EditorTools
{
    /// <summary>
    /// Medidas do desenho do boss (geradas a partir do recorte das suas peças — NÃO edite à mão).
    /// Posições em PIXELS da imagem original (2048x2048, y para BAIXO). O construtor converte para unidades (÷ Ppu, y para cima).
    /// </summary>
    public static class GGBossRigLayout
    {
        public const string ArtFolderName = "BossRig";
        public const float Ppu = 530.0f;                 // pixels por unidade das peças (o boss tem ~3.6 unidades de altura)
        public const float RootX = 1062.0f, RootY = 1934.0f; // pés (origem do Rig)
        public const float HipsX = 1085f, HipsY = 1010f; // quadril (pivô do Torso)
        public const float BackFootDropPx = 62f;     // pé de trás é 62 px mais baixo que o da frente no desenho

        public struct Part
        {
            public string name;    // nome do GameObject da junta
            public string file;    // sprite (arquivo em Art/BossRig, sem extensão)
            public string parent;  // junta pai
            public float jointX, jointY;   // articulação (px, imagem original)
            public float pivotX, pivotY;   // mesma articulação, normalizada DENTRO do recorte (y de baixo para cima)
            public int order;      // ordem de desenho (0 = atrás de tudo)

            public Part(string name, string file, string parent, float jx, float jy, float px, float py, int order)
            {
                this.name = name; this.file = file; this.parent = parent;
                jointX = jx; jointY = jy; pivotX = px; pivotY = py; this.order = order;
            }
        }

        /// <summary>Em ordem de hierarquia (pai antes do filho).</summary>
        public static readonly Part[] Parts =
        {
            new Part("LegBack_Thigh", "gboss_legback_thigh", "Hips", 1078.0f, 1010.0f, 0.8341346f, 0.9718121f, 0),
            new Part("LegBack_Calf", "gboss_legback_calf", "LegBack_Thigh", 795.0f, 1388.0f, 0.2386364f, 0.5805947f, 2),
            new Part("LegBack_Foot", "gboss_legback_foot", "LegBack_Calf", 938.0f, 1756.0f, 0.6185567f, 0.7828947f, 1),
            new Part("LegFront_Thigh", "gboss_legfront_thigh", "Hips", 1240.0f, 1030.0f, 0.8470948f, 0.9671302f, 3),
            new Part("LegFront_Calf", "gboss_legfront_calf", "LegFront_Thigh", 1030.0f, 1458.0f, 0.227758f, 0.5521669f, 5),
            new Part("LegFront_Foot", "gboss_legfront_foot", "LegFront_Calf", 1186.0f, 1802.0f, 0.6174699f, 0.7816092f, 4),
            new Part("Torso", "gboss_torso", "Hips", 1085.0f, 1010.0f, 0.466443f, 0.1893848f, 8),
            new Part("ArmBack_Upper", "gboss_armback_upper", "Torso", 1050.0f, 540.0f, 0.8127341f, 0.9466667f, 7),
            new Part("ArmBack_Lower", "gboss_armback_lower", "ArmBack_Upper", 885.0f, 860.0f, 0.9276944f, 0.119186f, 6),
            new Part("Head", "gboss_head", "Torso", 930.0f, 440.0f, 0.6273504f, 0.1442886f, 9),
            new Part("ArmFront_Upper", "gboss_armfront_upper", "Torso", 1140.0f, 600.0f, 0.869697f, 0.8547297f, 10),
            new Part("ArmFront_Lower", "gboss_armfront_lower", "ArmFront_Upper", 855.0f, 795.0f, 0.9580645f, 0.1076923f, 11),
        };
    }
}
