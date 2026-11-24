using System;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Biblioteca de movimentos do boss: funções puras (só matemática, sem UnityEngine) que devolvem uma GGBossPose.
    /// Tudo no referencial "olhando para a DIREITA", relativo à pose de repouso do desenho.
    /// Tempos em BATIDAS (t = batidas desde o início da animação), exceto Die/HurtDelta que usam segundos.
    /// Os números são de gosto: ajuste à vontade (amplitudes em graus / unidades).
    /// </summary>
    public static class GGBossMotion
    {
        public const float StepHalf = 0.40f;      // amplitude do passo (unidades)
        public const float StrideLength = 1.6f;   // distância percorrida por ciclo completo de passada

        const float Tau = 6.2831853f;
        const float Pi = 3.1415927f;

        static float Sin(float x) { return (float)Math.Sin(x); }
        static float Cos(float x) { return (float)Math.Cos(x); }
        static float Sm(float x) { if (x <= 0f) return 0f; if (x >= 1f) return 1f; return x * x * (3f - 2f * x); }
        static float Ramp(float t, float a, float b) { return Sm((t - a) / (b - a)); }
        static float Clamp01(float x) { return x < 0f ? 0f : (x > 1f ? 1f : x); }
        static float Frac(double x) { return (float)(x - Math.Floor(x)); }
        static float Acc(float u, float power) { float k = 1f - u; if (k < 0f) k = 0f; return (float)Math.Pow(k, power); }
        static float Pow(float x, float p) { return (float)Math.Pow(x, p); }

        /// <summary>Duração padrão (em batidas) de cada animação quando o GGBoss não informa.</summary>
        public static float DefaultBeats(GGBossAnim a)
        {
            switch (a)
            {
                case GGBossAnim.Intro: return 8f;
                case GGBossAnim.Dash: return 2f;
                case GGBossAnim.Hands: return 6f;
                case GGBossAnim.Spin: return 4f;
                case GGBossAnim.Orbs: return 5f;
                case GGBossAnim.Summon: return 4f;
                case GGBossAnim.PhaseChange: return 4f;
                default: return 0f;
            }
        }

        public static GGBossPose Evaluate(GGBossAnim anim, double beat, float t, float dur, int sub, float e)
        {
            switch (anim)
            {
                case GGBossAnim.Dormant: return Dormant(beat);
                case GGBossAnim.Intro: return Intro(beat, t, dur, e);
                case GGBossAnim.Dash: return Dash(beat, sub, t, dur, e);
                case GGBossAnim.Hands: return Hands(beat, t, dur, e);
                case GGBossAnim.Spin: return Spin(beat, t, dur, e);
                case GGBossAnim.Orbs: return Orbs(beat, t, dur, e);
                case GGBossAnim.Summon: return Summon(beat, t, dur, e);
                case GGBossAnim.PhaseChange: return PhaseChange(beat, t, dur, e);
                case GGBossAnim.Die: return Die(t);
                default: return Idle(beat, e);
            }
        }

        // ------------------------------------------------------------------ repouso / idle
        /// <summary>Largado, cabeça baixa, braços pendurados, respirando devagar.</summary>
        public static GGBossPose Dormant(double beat)
        {
            GGBossPose p = GGBossPose.Rest;
            float br = Sin(Tau * Frac(beat * 0.25));
            p.hipsY = -0.13f + 0.012f * br;
            p.hipsX = -0.05f;
            p.torso = -16f + 1.2f * br;
            p.head = -30f + 2f * br;
            p.armFU = -30f + 2f * br; p.armFL = -78f + 3f * br;
            p.armBU = -14f - 2f * br; p.armBL = -80f - 3f * br;
            return p;
        }

        /// <summary>Dança zumbi no ritmo: joelhos "afundam" a cada batida, cabeça balança, garras ondulam.</summary>
        public static GGBossPose Idle(double beat, float e)
        {
            GGBossPose p = GGBossPose.Rest;
            float u = Frac(beat);
            float b2 = Frac(beat * 0.5);
            float acc = Acc(u, 3f);
            float s1 = Sin(Tau * b2);
            p.hipsY = -0.05f * e - 0.075f * e * acc;
            p.hipsX = 0.015f * s1;
            p.torso = -2f * e * acc + 2.5f * s1;
            p.head = -9f * e * acc + 3f * Sin(Tau * b2 + 1f);
            p.armFU = 3f * s1 + 6f * e * acc;
            p.armFL = 8f * Sin(Tau * u + 0.5f);
            p.armBU = -3f * s1 + 5f * e * acc;
            p.armBL = 8f * Sin(Tau * u + 2f);
            return p;
        }

        // ------------------------------------------------------------------ caminhada (ciclos = distância / StrideLength)
        public static GGBossPose Walk(float cycles, float e)
        {
            GGBossPose p = GGBossPose.Rest;
            float s = Sin(Tau * cycles), c = Cos(Tau * cycles);
            float liftF = Math.Max(0f, -c), liftB = Math.Max(0f, c);
            p.footFX = -StepHalf * s;
            p.footFY = 0.17f * Pow(liftF, 0.75f);
            p.footFRot = 16f * liftF;
            p.footBX = StepHalf * s;
            p.footBY = 0.17f * Pow(liftB, 0.75f);
            p.footBRot = 16f * liftB;
            p.hipsY = -0.05f + 0.045f * Cos(2f * Tau * cycles);
            p.torso = -3f + 2f * s;
            p.head = -5f + 3f * Sin(2f * Tau * cycles + 0.7f);
            p.armFU = 6f * s; p.armFL = 10f * Sin(Tau * cycles + 1f);
            p.armBU = -6f * s; p.armBL = 10f * Sin(Tau * cycles + 2.4f);
            return p;
        }

        // ------------------------------------------------------------------ intro
        static GGBossPose Roar(double beat)
        {
            GGBossPose p = GGBossPose.Rest;
            float tr = Sin(Tau * Frac(beat * 5.0));
            p.hipsY = 0.04f; p.hipsX = -0.05f;
            p.torso = 20f + 2.5f * tr; p.head = 36f + 3f * tr;
            p.armFU = -39f + 4f * tr; p.armFL = 11f + 6f * tr;      // garras junto ao corpo, peito aberto: o rosto fica livre
            p.armBU = -32f - 4f * tr; p.armBL = 3f - 6f * tr;
            return p;
        }

        public static GGBossPose Intro(double beat, float t, float dur, float e)
        {
            float s = t * (8f / Math.Max(1f, dur));          // normaliza para 8 batidas
            GGBossPose p = Dormant(beat);
            // espasmos antes de acordar
            float tw = Sin(Tau * Frac(beat * 3.0)) * Ramp(s, 0.8f, 1.2f) * (1f - Ramp(s, 1.7f, 2.1f));
            p.head += 7f * tw; p.armFL += 6f * tw; p.armBL -= 6f * tw;
            p = GGBossPose.Lerp(p, Idle(beat, e), Ramp(s, 2f, 4f));
            float w = Ramp(s, 4f, 4.5f) * (1f - Ramp(s, 6.6f, 7.8f));
            p = GGBossPose.Lerp(p, Roar(beat), w);
            return p;
        }

        // ------------------------------------------------------------------ deslize (moonwalk)
        /// <summary>sub 0 = aviso (agacha e treme), 1 = deslizando, 2 = recuperando.</summary>
        public static GGBossPose Dash(double beat, int sub, float t, float dur, float e)
        {
            GGBossPose p = GGBossPose.Rest;
            if (sub == 0)
            {
                float s = Clamp01(t / Math.Max(0.5f, dur));
                float cr = Ramp(s, 0f, 0.35f);
                float tr = Sin(Tau * Frac(beat * 4.0)) * s;
                p.hipsY = -0.34f * cr + 0.012f * tr; p.hipsX = -0.12f * cr;
                p.torso = 14f * cr + 1.6f * tr; p.head = -10f * cr;
                p.armFU = -38f * cr; p.armFL = -22f * cr; p.armBU = -30f * cr; p.armBL = -25f * cr;
                p.footFX = -0.08f * cr; p.footBX = -0.08f * cr;
                return p;
            }
            if (sub == 1)
            {
                float c = Frac(beat);
                float sn = Sin(Tau * c), cs = Cos(Tau * c);
                const float A = 0.28f;
                p.footFX = -A * sn; p.footFY = 0.05f * Math.Max(0f, -cs); p.footFRot = 8f * Math.Max(0f, -cs);
                p.footBX = A * sn; p.footBY = 0.05f * Math.Max(0f, cs); p.footBRot = 8f * Math.Max(0f, cs);
                p.hipsY = -0.20f; p.hipsX = 0.06f;
                p.torso = -13f; p.head = 5f;
                p.armFU = 14f + 3f * sn; p.armFL = 6f * Sin(Tau * c + 1f);
                p.armBU = 10f - 3f * sn; p.armBL = 6f * Sin(Tau * c + 2f);
                return p;
            }
            GGBossPose idle = Idle(beat, e);
            idle.hipsY -= 0.20f * (1f - Ramp(t, 0f, 0.8f));
            idle.torso += 6f * (1f - Ramp(t, 0f, 0.8f));
            return idle;
        }

        // ------------------------------------------------------------------ mãos de zumbi
        public static GGBossPose Hands(double beat, float t, float dur, float e)
        {
            float up = Ramp(t, 0f, 0.9f) * (1f - Ramp(t, dur - 1.0f, dur - 0.2f));
            GGBossPose u = GGBossPose.Rest;
            u.armFU = 63f; u.armFL = -59f; u.armBU = 95f; u.armBL = -62f;
            u.torso = 8f; u.head = 10f; u.hipsY = -0.04f;
            GGBossPose p = GGBossPose.Lerp(Idle(beat, e), u, up);

            float ph = Frac(beat);
            float a = Acc(ph, 2.2f);
            bool even = ((long)Math.Floor(beat)) % 2L == 0L;
            float pf = even ? a : 0f, pb = even ? 0f : a;
            float any = (pf + pb) * up;
            p.armFU -= 65f * pf * up; p.armFL += 5f * pf * up;
            p.armBU -= 65f * pb * up; p.armBL += 5f * pb * up;
            p.torso -= 14f * any; p.head -= 12f * any; p.hipsY -= 0.10f * any;
            return p;
        }

        // ------------------------------------------------------------------ giro (ondas de choque)
        public static GGBossPose Spin(double beat, float t, float dur, float e)
        {
            dur = Math.Max(3f, dur);
            GGBossPose idle = Idle(beat, e);
            float wind = Ramp(t, 0f, 0.8f) * (1f - Ramp(t, 0.9f, 1.15f));
            float spinN = Clamp01((t - 1f) / (dur - 2f));
            float sp = Ramp(t, 0.9f, 1.25f) * (1f - Ramp(t, dur - 1.25f, dur - 0.95f));
            float turns = (float)Math.Max(1.0, Math.Round(dur - 2f));
            float ang = Tau * turns * Sm(spinN) ;
            float hop = Sin(Pi * spinN) * sp;

            GGBossPose p = GGBossPose.Lerp(idle, SpinCrouch(), wind);
            GGBossPose o = GGBossPose.Rest;                  // braços abertos, giro no ar
            o.armFU = 40f; o.armFL = -78f; o.armBU = 68f; o.armBL = -82f;
            o.torso = -6f; o.hipsY = 0.06f;
            o.footFY = 0.26f; o.footBY = 0.26f; o.footFX = 0.12f; o.footBX = -0.12f;
            p = GGBossPose.Lerp(p, o, sp);
            p.rigY = 0.30f * hop;

            // pouso: pisão
            float land = Ramp(t, dur - 1f, dur - 0.85f) * (1f - Ramp(t, dur - 0.85f, dur - 0.2f));
            GGBossPose st = GGBossPose.Rest;
            st.hipsY = -0.30f; st.torso = -8f; st.head = -10f;
            st.armFU = 40f; st.armFL = -45f; st.armBU = 35f; st.armBL = -45f;
            p = GGBossPose.Lerp(p, st, land);
            float tail = Ramp(t, dur - 0.5f, dur);
            p = GGBossPose.Lerp(p, idle, tail);

            float sx = (float)Math.Cos(ang);
            if (Math.Abs(sx) < 0.07f) sx = sx < 0f ? -0.07f : 0.07f;
            p.scaleX = (t >= 1f && t < dur - 0.9f) ? sx : 1f;
            return p;
        }

        static GGBossPose SpinCrouch()
        {
            GGBossPose c = GGBossPose.Rest;
            c.hipsY = -0.25f; c.torso = 6f; c.head = -6f;
            c.armFU = -25f; c.armFL = 30f; c.armBU = -20f; c.armBL = 30f;
            return c;
        }

        // ------------------------------------------------------------------ orbs
        public static GGBossPose Orbs(double beat, float t, float dur, float e)
        {
            float act = Ramp(t, 0f, 0.9f) * (1f - Ramp(t, dur - 1f, dur - 0.2f));
            GGBossPose cock = GGBossPose.Rest;
            cock.torso = 9f; cock.head = 6f; cock.hipsY = -0.06f; cock.hipsX = -0.06f;
            cock.armFU = -10f; cock.armFL = 12f; cock.armBU = -8f; cock.armBL = 24f;
            GGBossPose thr = GGBossPose.Rest;
            thr.torso = -10f; thr.head = -4f; thr.hipsY = -0.10f; thr.hipsX = 0.10f;
            thr.armFU = 45f; thr.armFL = -65f; thr.armBU = 30f; thr.armBL = -40f;
            float a = Acc(Frac(beat), 2.5f);
            GGBossPose act2 = GGBossPose.Lerp(cock, thr, a);
            return GGBossPose.Lerp(Idle(beat, e), act2, act);
        }

        // ------------------------------------------------------------------ invocar fantasmas
        public static GGBossPose Summon(double beat, float t, float dur, float e)
        {
            float raise = Ramp(t, 0f, 1.5f);
            float tr = Sin(Tau * Frac(beat * 6.0)) * raise * (t < 2f ? 1f : 0f);
            GGBossPose up = GGBossPose.Rest;
            up.armFU = 80f + 3f * tr; up.armFL = -49f + 4f * tr; up.armBU = 122f - 3f * tr; up.armBL = -57f - 4f * tr;
            up.torso = 16f + 1.5f * tr; up.head = 28f; up.hipsY = 0.03f;
            GGBossPose slam = GGBossPose.Rest;
            slam.armFU = -10f; slam.armFL = 5f; slam.armBU = -5f; slam.armBL = 5f;
            slam.torso = -22f; slam.head = -14f; slam.hipsY = -0.26f;
            GGBossPose p = GGBossPose.Lerp(Idle(beat, e), up, raise);
            p = GGBossPose.Lerp(p, slam, Ramp(t, 1.95f, 2.25f));
            p = GGBossPose.Lerp(p, Idle(beat, e), Ramp(t, 2.6f, 3.8f));
            return p;
        }

        // ------------------------------------------------------------------ mudança de fase
        public static GGBossPose PhaseChange(double beat, float t, float dur, float e)
        {
            GGBossPose idle = Idle(beat, e);
            float crouch = Ramp(t, 0f, 0.5f);
            float burst = Ramp(t, 0.5f, 0.9f) * (1f - Ramp(t, 3.2f, 3.9f));
            GGBossPose c = GGBossPose.Rest;
            c.hipsY = -0.30f; c.torso = -16f; c.head = -12f;
            c.armFU = -30f; c.armFL = -40f; c.armBU = -25f; c.armBL = -40f;
            GGBossPose r = Roar(beat);
            r.torso += 4f; r.head += 4f;
            GGBossPose p = GGBossPose.Lerp(idle, c, crouch);
            p = GGBossPose.Lerp(p, r, burst);
            p.hipsY -= 0.06f * Acc(Frac(beat), 3f) * burst;
            p = GGBossPose.Lerp(p, idle, Ramp(t, 3.4f, 4f));
            return p;
        }

        // ------------------------------------------------------------------ morte (segundos)
        public static GGBossPose Die(float sec)
        {
            GGBossPose p = GGBossPose.Rest;
            float hit = Ramp(sec, 0f, 0.15f);
            float fall = Ramp(sec, 0.25f, 1.35f);
            float f = Pow(fall, 1.7f);
            float limp = Ramp(sec, 0.9f, 1.8f);

            p.head = 45f * hit; p.torso = 24f * hit;
            p.hipsX = -0.10f * hit;

            p.rigRot = 88f * f;
            p.rigY = 0.38f * f;
            if (sec > 1.35f)
            {
                float k = sec - 1.35f;
                p.rigY += 0.12f * (float)(Math.Exp(-9.0 * k) * Math.Abs(Math.Sin(Tau * 3f * k)));
            }
            // largado no chão: braços abertos, pernas dobradas
            p.armFU = 120f * hit * (1f - limp) - 47f * limp; p.armFL = -65f * hit * (1f - limp) - 79f * limp;
            p.armBU = 120f * hit * (1f - limp) - 25f * limp; p.armBL = -70f * hit * (1f - limp) - 82f * limp;
            p.head = 45f * hit * (1f - limp) + 12f * limp;
            p.torso = 24f * hit * (1f - limp) + 0f * limp;
            p.hipsY = -0.10f * limp;
            p.footFX = 0.22f * limp; p.footFY = 0.30f * limp;
            p.footBX = 0.05f * limp; p.footBY = 0.18f * limp;
            return p;
        }

        /// <summary>Sobreposição de recuo ao levar dano (soma na pose atual). Decai em ~0.4 s.</summary>
        public static GGBossPose HurtDelta(float sec)
        {
            GGBossPose d = GGBossPose.Rest;
            if (sec < 0f || sec > 0.7f) return d;
            float k = (float)Math.Exp(-8.0 * sec);
            d.torso = 16f * k; d.head = 24f * k;
            d.armFU = 18f * k; d.armFL = -12f * k; d.armBU = 14f * k; d.armBL = -10f * k;
            d.hipsX = -0.12f * k; d.hipsY = -0.08f * k;
            d.rigRot = 3f * k;
            return d;
        }
    }
}
