using System;

namespace PopAdventure.Fase3
{
    /// <summary>Medidas de uma perna no repouso (referencial do SPRITE, y para cima, unidades): offsets locais.</summary>
    public struct GGBossLegRest
    {
        public float hipX, hipY;     // articulação da coxa, relativa ao Hips
        public float kneeX, kneeY;   // joelho (filho da coxa), relativo à coxa
        public float ankleX, ankleY; // tornozelo (filho da canela), relativo ao joelho
    }

    /// <summary>Esqueleto de repouso lido da hierarquia do prefab (referencial do sprite).</summary>
    public struct GGBossRestData
    {
        public float hipsX, hipsY;       // posição local do Hips dentro do Rig
        public GGBossLegRest legF, legB; // perna da frente (F) e de trás (B)
        public float backFootDrop;       // quanto o pé de trás desce para ficar na linha do chão do pé da frente
    }

    /// <summary>Resultado: valores locais (referencial do sprite, graus anti-horário) prontos para aplicar nos Transforms.</summary>
    public struct GGBossJoints
    {
        public float rigX, rigY, rigRot, rigSX, rigSY;
        public float hipsX, hipsY;
        public float torso, head, armFU, armFL, armBU, armBL;
        public float thighF, calfF, footF, thighB, calfB, footB;
    }

    /// <summary>Converte uma GGBossPose em rotações/posições dos joints (com IK de 2 ossos nas pernas).</summary>
    public static class GGBossSolver
    {
        const float Rad2Deg = 57.29578f;
        const float Deg2Rad = 0.01745329f;

        static float Wrap(float a)
        {
            while (a > 180f) a -= 360f;
            while (a < -180f) a += 360f;
            return a;
        }

        public static GGBossJoints Solve(GGBossRestData r, GGBossPose p, bool artFacesLeft)
        {
            float m = artFacesLeft ? -1f : 1f; // espelha o referencial "olhando para a direita" para o do sprite
            GGBossJoints j = new GGBossJoints();

            j.rigX = p.rigX * m; j.rigY = p.rigY; j.rigRot = p.rigRot * m;
            j.rigSX = p.scaleX; j.rigSY = p.scaleY;
            j.hipsX = r.hipsX + p.hipsX * m; j.hipsY = r.hipsY + p.hipsY;
            j.torso = p.torso * m; j.head = p.head * m;
            j.armFU = p.armFU * m; j.armFL = p.armFL * m;
            j.armBU = p.armBU * m; j.armBL = p.armBL * m;

            SolveLeg(r.legF, r.hipsX, r.hipsY, j.hipsX, j.hipsY, p.footFX * m, p.footFY, p.footFRot * m, j.rigRot,
                out j.thighF, out j.calfF, out j.footF);
            SolveLeg(r.legB, r.hipsX, r.hipsY, j.hipsX, j.hipsY, p.footBX * m, p.footBY - r.backFootDrop, p.footBRot * m, j.rigRot,
                out j.thighB, out j.calfB, out j.footB);
            return j;
        }

        static void SolveLeg(GGBossLegRest leg, float restHipsX, float restHipsY, float hipsX, float hipsY,
            float dx, float dy, float footRot, float rigRot,
            out float thigh, out float calf, out float foot)
        {
            // posição da articulação da coxa (o Hips só se translada)
            float hx = hipsX + leg.hipX, hy = hipsY + leg.hipY;
            float l1 = (float)Math.Sqrt(leg.kneeX * leg.kneeX + leg.kneeY * leg.kneeY);
            float l2 = (float)Math.Sqrt(leg.ankleX * leg.ankleX + leg.ankleY * leg.ankleY);

            // alvo do tornozelo: repouso + offset da pose
            float tx = restHipsX + leg.hipX + leg.kneeX + leg.ankleX + dx;
            float ty = restHipsY + leg.hipY + leg.kneeY + leg.ankleY + dy;

            float cx = tx - hx, cy = ty - hy;
            float d = (float)Math.Sqrt(cx * cx + cy * cy);
            float dMax = (l1 + l2) * 0.999f;
            float dMin = Math.Abs(l1 - l2) * 1.001f + 0.001f;
            if (d > dMax) { float k = dMax / d; cx *= k; cy *= k; d = dMax; tx = hx + cx; ty = hy + cy; }
            else if (d < dMin)
            {
                if (d < 1e-5f) { cx = 0f; cy = -1f; d = 1f; }
                float k = dMin / d;
                cx *= k; cy *= k; d = dMin;
                tx = hx + cx; ty = hy + cy;
            }

            // lado em que o joelho dobra = o do repouso
            float rcx = leg.kneeX + leg.ankleX, rcy = leg.kneeY + leg.ankleY;
            float cross = rcx * leg.kneeY - rcy * leg.kneeX;
            float bend = cross >= 0f ? 1f : -1f;

            float cosA = (l1 * l1 + d * d - l2 * l2) / (2f * l1 * d);
            if (cosA > 1f) cosA = 1f; else if (cosA < -1f) cosA = -1f;
            float A = (float)Math.Acos(cosA);
            float a1 = (float)Math.Atan2(cy, cx) + bend * A;
            float kx = hx + l1 * (float)Math.Cos(a1);
            float ky = hy + l1 * (float)Math.Sin(a1);
            float a2 = (float)Math.Atan2(ty - ky, tx - kx);

            float a1Rest = (float)Math.Atan2(leg.kneeY, leg.kneeX);
            float a2Rest = (float)Math.Atan2(leg.ankleY, leg.ankleX);
            float thighD = Wrap((a1 - a1Rest) * Rad2Deg);
            float totalD = Wrap((a2 - a2Rest) * Rad2Deg);   // rotação total (coxa + canela) da canela
            thigh = thighD;
            calf = Wrap(totalD - thighD);
            foot = Wrap(footRot - rigRot - totalD);          // pé "achatado": compensa o corpo e a perna
        }
    }
}
