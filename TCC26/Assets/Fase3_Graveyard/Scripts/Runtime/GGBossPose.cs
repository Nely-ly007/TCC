using System;

namespace PopAdventure.Fase3
{
    /// <summary>Animações automáticas do rig do boss (GGBossRig).</summary>
    public enum GGBossAnim
    {
        Dormant = 0,     // largado, cabeça baixa (antes da luta)
        Idle = 1,        // dança no ritmo (padrão durante a luta)
        Intro = 2,       // acorda e ruge (~8 batidas)
        Dash = 3,        // agacha (aviso) e desliza (moonwalk)
        Hands = 4,       // levanta os braços e "regência" das mãos de zumbi
        Spin = 5,        // giro com pulinho + pisões
        Orbs = 6,        // dispara os orbs (empurra a mão no tempo)
        Summon = 7,      // braços para o alto, invoca fantasmas
        PhaseChange = 8, // ruge e treme (mudança de fase)
        Die = 9          // cai para trás e fica no chão
    }

    /// <summary>
    /// Pose do boss em graus/unidades RELATIVAS à pose de repouso (a do desenho original).
    /// Referencial "olhando para a DIREITA": x positivo = para frente, ângulo positivo = anti-horário
    /// (braço sobe para frente, tronco inclina para trás, cabeça levanta). O GGBossRig converte para o
    /// seu sprite (que olha para a esquerda) automaticamente.
    /// </summary>
    public struct GGBossPose
    {
        // corpo inteiro (pivô nos pés)
        public float rigX, rigY, rigRot, scaleX, scaleY;
        // quadril (translação) e articulações
        public float hipsX, hipsY;
        public float torso, head;
        public float armFU, armFL, armBU, armBL;       // braço da frente (F) e de trás (B): U = ombro, L = cotovelo
        // alvos dos tornozelos (offset do repouso, em unidades) e rotação desejada dos pés (graus, "no mundo")
        public float footFX, footFY, footBX, footBY;
        public float footFRot, footBRot;

        public static GGBossPose Rest
        {
            get
            {
                GGBossPose p = new GGBossPose();
                p.scaleX = 1f;
                p.scaleY = 1f;
                return p;
            }
        }

        static float L(float a, float b, float t) { return a + (b - a) * t; }

        public static GGBossPose Lerp(GGBossPose a, GGBossPose b, float t)
        {
            GGBossPose r = new GGBossPose();
            r.rigX = L(a.rigX, b.rigX, t); r.rigY = L(a.rigY, b.rigY, t); r.rigRot = L(a.rigRot, b.rigRot, t);
            r.scaleX = L(a.scaleX, b.scaleX, t); r.scaleY = L(a.scaleY, b.scaleY, t);
            r.hipsX = L(a.hipsX, b.hipsX, t); r.hipsY = L(a.hipsY, b.hipsY, t);
            r.torso = L(a.torso, b.torso, t); r.head = L(a.head, b.head, t);
            r.armFU = L(a.armFU, b.armFU, t); r.armFL = L(a.armFL, b.armFL, t);
            r.armBU = L(a.armBU, b.armBU, t); r.armBL = L(a.armBL, b.armBL, t);
            r.footFX = L(a.footFX, b.footFX, t); r.footFY = L(a.footFY, b.footFY, t);
            r.footBX = L(a.footBX, b.footBX, t); r.footBY = L(a.footBY, b.footBY, t);
            r.footFRot = L(a.footFRot, b.footFRot, t); r.footBRot = L(a.footBRot, b.footBRot, t);
            return r;
        }

        /// <summary>Soma um delta (sobreposição, ex.: recuo ao levar dano). A escala é multiplicada.</summary>
        public static GGBossPose Add(GGBossPose a, GGBossPose d)
        {
            GGBossPose r = a;
            r.rigX += d.rigX; r.rigY += d.rigY; r.rigRot += d.rigRot;
            r.hipsX += d.hipsX; r.hipsY += d.hipsY;
            r.torso += d.torso; r.head += d.head;
            r.armFU += d.armFU; r.armFL += d.armFL; r.armBU += d.armBU; r.armBL += d.armBL;
            r.footFX += d.footFX; r.footFY += d.footFY; r.footBX += d.footBX; r.footBY += d.footBY;
            r.footFRot += d.footFRot; r.footBRot += d.footBRot;
            return r;
        }

        /// <summary>Delta vazio (para sobreposições): tudo zero, escala 1.</summary>
        public static GGBossPose Zero { get { return Rest; } }
    }
}
