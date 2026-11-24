using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Animação AUTOMÁTICA do boss: um rig "cut-out" (peças do desenho presas por juntas) movido 100% por código.
    /// Não precisa de Animator nem de clips. Tudo roda no relógio de batidas (GGBeatClock, 114 BPM):
    ///   • Dormant     largado, cabeça baixa (antes da luta)
    ///   • Idle        dança no ritmo (padrão)  +  caminhada automática (medida pela velocidade real do boss)
    ///   • Intro / Dash / Hands / Spin / Orbs / Summon / PhaseChange / Die  — disparados pelo GGBoss
    ///   • Hurt        recuo sobreposto a qualquer animação
    /// As pernas usam IK de 2 ossos (pés sempre achatados e plantados). Os números das poses ficam em GGBossMotion.
    ///
    /// Hierarquia esperada (o menu "Pop Adventure > Fase 3 > Recriar Boss com Rig Procedural" monta tudo):
    ///   Visual (este script + SortingGroup)  ->  Rig  ->  Hips  ->  Torso (Head, ArmFront_*, ArmBack_*), Leg*_Thigh -> Calf -> Foot
    /// Cada junta é um GameObject cujo pivô de sprite está exatamente na articulação.
    ///
    /// TESTE RÁPIDO: arraste o prefab do boss para uma cena vazia, marque "Demo Loop" e dê Play —
    /// ele passa por todas as animações em sequência (o texto no canto da tela diz qual).
    /// </summary>
    [DisallowMultipleComponent]
    public class GGBossRig : MonoBehaviour
    {
        [Header("Juntas (preenchidas pelo construtor; ou use o menu de contexto 'Preencher juntas pelos nomes')")]
        [SerializeField] Transform rig;
        [SerializeField] Transform hips;
        [SerializeField] Transform torso;
        [SerializeField] Transform head;
        [SerializeField] Transform armFrontUpper;
        [SerializeField] Transform armFrontLower;
        [SerializeField] Transform armBackUpper;
        [SerializeField] Transform armBackLower;
        [SerializeField] Transform thighFront;
        [SerializeField] Transform calfFront;
        [SerializeField] Transform footFront;
        [SerializeField] Transform thighBack;
        [SerializeField] Transform calfBack;
        [SerializeField] Transform footBack;

        [Header("Desenho")]
        [Tooltip("Marque se o desenho original olha para a ESQUERDA (o zumbi enviado olha). O GGBoss se ajusta sozinho.")]
        [SerializeField] bool artFacesLeft = true;
        [Tooltip("Quanto o pé de TRÁS está mais baixo que o da frente no desenho (unidades). Valor medido do desenho: 62 / 530.")]
        [SerializeField] float backFootDrop = 62f / 530f;

        [Header("Animação")]
        [Tooltip("Tempo (s) da transição suave entre uma animação e outra.")]
        [SerializeField] float crossfadeSeconds = 0.12f;
        [Tooltip("Distância (unidades) percorrida em um ciclo completo de passos. Menor = pés mais rápidos.")]
        [SerializeField] float walkStride = GGBossMotion.StrideLength;
        [Tooltip("Velocidade mínima (u/s) para começar a andar.")]
        [SerializeField] float walkThreshold = 0.25f;
        [Tooltip("Objeto que anda de verdade (para medir a velocidade). Vazio = o GGBoss do pai.")]
        [SerializeField] Transform motionSource;

        [Header("Teste")]
        [Tooltip("Passa por todas as animações em loop (para ver o rig sem montar a luta). Use com o boss sozinho na cena.")]
        [SerializeField] bool demoLoop;

        // ---------------------------------------------------------------- estado
        GGBossRestData restData;
        bool ready;

        GGBossAnim current = GGBossAnim.Dormant;
        int sub;
        float duration;
        double startBeat;
        double startTime;
        float energy = 1f;

        GGBossPose lastPose = GGBossPose.Rest;
        GGBossPose fadeFrom = GGBossPose.Rest;
        float fadeT = 1f;
        float fadeDur = 0.12f;

        double hurtStart = -100.0;

        float lastX;
        bool hasLastX;
        float walkBlend;
        float walkCycles;

        // demo
        int demoIndex = -1;
        double demoNextAt;
        bool demoWalk;

        public bool ArtFacesLeft { get { return artFacesLeft; } }
        public GGBossAnim Current { get { return current; } }
        public bool IsDead { get { return current == GGBossAnim.Die; } }

        // ================================================================== API (usada pelo GGBoss)
        /// <summary>Intensidade da dança por fase da luta (1 = calma, 3 = frenética).</summary>
        public void SetPhase(int phase)
        {
            energy = 1f + 0.2f * (Mathf.Clamp(phase, 1, 3) - 1);
        }

        /// <summary>
        /// Toca uma animação. beats = duração em batidas (0 = padrão da animação). sub só vale para Dash
        /// (0 = aviso, 1 = deslize, 2 = recuperação). startBeat opcional: batida exata em que a animação "começou".
        /// Ao terminar (beats), volta sozinha para Idle. Die não volta: o boss fica no chão.
        /// </summary>
        public void Play(GGBossAnim anim, float beats = 0f, int subStage = 0, double beatStart = double.NaN)
        {
            if (!ready) return;
            if (current == GGBossAnim.Die && anim != GGBossAnim.Dormant) return;
            if (anim == GGBossAnim.Idle) { GoIdle(crossfadeSeconds); return; }
            if (anim == GGBossAnim.Dormant) { ResetToDormant(); return; }

            BeginFade(anim == GGBossAnim.Die ? 0.10f : crossfadeSeconds);
            current = anim;
            sub = subStage;
            duration = beats > 0f ? beats : GGBossMotion.DefaultBeats(anim);
            startBeat = double.IsNaN(beatStart) ? GGBeatClock.Position : beatStart;
            startTime = Time.timeAsDouble;
        }

        /// <summary>Recuo de dano (sobreposto à animação atual).</summary>
        public void Hurt()
        {
            if (!ready || current == GGBossAnim.Die) return;
            double now = Time.timeAsDouble;
            if (now - hurtStart < 0.2) return;
            hurtStart = now;
        }

        /// <summary>Volta para o estado "largado" (antes da luta / depois de reiniciar a luta).</summary>
        public void ResetToDormant()
        {
            if (!ready) return;
            BeginFade(0.35f);
            current = GGBossAnim.Dormant;
            sub = 0;
            duration = 0f;
            startBeat = GGBeatClock.Position;
            startTime = Time.timeAsDouble;
            hurtStart = -100.0;
            energy = 1f;
            walkBlend = 0f;
        }

        void GoIdle(float fade)
        {
            BeginFade(fade);
            current = GGBossAnim.Idle;
            sub = 0;
            duration = 0f;
            startBeat = GGBeatClock.Position;
            startTime = Time.timeAsDouble;
        }

        void BeginFade(float seconds)
        {
            fadeFrom = lastPose;
            fadeT = 0f;
            fadeDur = Mathf.Max(0.001f, seconds);
        }

        // ================================================================== setup
        void Awake()
        {
            if (rig == null || hips == null) AutoAssignJoints();
            if (!Validate()) { enabled = false; return; }

            CaptureRest();
            if (motionSource == null)
            {
                GGBoss boss = GetComponentInParent<GGBoss>();
                motionSource = boss != null ? boss.transform : (transform.parent != null ? transform.parent : transform);
            }
            ready = true;
            current = GGBossAnim.Dormant;
            startBeat = GGBeatClock.Position;
            startTime = Time.timeAsDouble;
            ApplyPose(GGBossMotion.Dormant(startBeat));
        }

        bool Validate()
        {
            Transform[] all = { rig, hips, torso, head, armFrontUpper, armFrontLower, armBackUpper, armBackLower,
                                thighFront, calfFront, footFront, thighBack, calfBack, footBack };
            string[] names = { "rig", "hips", "torso", "head", "armFrontUpper", "armFrontLower", "armBackUpper", "armBackLower",
                               "thighFront", "calfFront", "footFront", "thighBack", "calfBack", "footBack" };
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                {
                    Debug.LogError("GGBossRig: junta '" + names[i] + "' não atribuída. Rode Pop Adventure > Fase 3 > Recriar Boss com Rig Procedural "
                                   + "ou use o menu de contexto do componente ('Preencher juntas pelos nomes').", this);
                    return false;
                }
            }
            return true;
        }

        void CaptureRest()
        {
            GGBossRestData r = new GGBossRestData();
            r.hipsX = hips.localPosition.x;
            r.hipsY = hips.localPosition.y;
            r.legF = ReadLeg(thighFront, calfFront, footFront);
            r.legB = ReadLeg(thighBack, calfBack, footBack);
            r.backFootDrop = backFootDrop;
            restData = r;
        }

        static GGBossLegRest ReadLeg(Transform thigh, Transform calf, Transform foot)
        {
            GGBossLegRest l = new GGBossLegRest();
            l.hipX = thigh.localPosition.x; l.hipY = thigh.localPosition.y;     // relativo ao Hips
            l.kneeX = calf.localPosition.x; l.kneeY = calf.localPosition.y;     // relativo à coxa
            l.ankleX = foot.localPosition.x; l.ankleY = foot.localPosition.y;   // relativo à canela
            return l;
        }

        /// <summary>Procura as juntas pelos nomes dos filhos (Rig, Hips, Torso, Head, ArmFront_Upper, ...).</summary>
        [ContextMenu("Preencher juntas pelos nomes")]
        public void AutoAssignJoints()
        {
            rig = FindDeep(transform, "Rig");
            hips = FindDeep(transform, "Hips");
            torso = FindDeep(transform, "Torso");
            head = FindDeep(transform, "Head");
            armFrontUpper = FindDeep(transform, "ArmFront_Upper");
            armFrontLower = FindDeep(transform, "ArmFront_Lower");
            armBackUpper = FindDeep(transform, "ArmBack_Upper");
            armBackLower = FindDeep(transform, "ArmBack_Lower");
            thighFront = FindDeep(transform, "LegFront_Thigh");
            calfFront = FindDeep(transform, "LegFront_Calf");
            footFront = FindDeep(transform, "LegFront_Foot");
            thighBack = FindDeep(transform, "LegBack_Thigh");
            calfBack = FindDeep(transform, "LegBack_Calf");
            footBack = FindDeep(transform, "LegBack_Foot");
        }

        static Transform FindDeep(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c.name == name) return c;
                Transform r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        void Reset()
        {
            AutoAssignJoints();
        }

        // ================================================================== laço principal
        void LateUpdate()
        {
            if (!ready) return;
            float dt = Time.deltaTime;
            double beat = GGBeatClock.Position;

            UpdateDemo(beat);
            UpdateLocomotion(dt);

            // volta sozinho para Idle quando a animação acaba
            if (IsTimed(current) && duration > 0f && beat - startBeat >= duration) GoIdle(crossfadeSeconds);

            GGBossPose pose = Target(beat);
            if (fadeT < fadeDur)
            {
                fadeT += dt;
                float k = Mathf.Clamp01(fadeT / fadeDur);
                k = k * k * (3f - 2f * k);
                pose = GGBossPose.Lerp(fadeFrom, pose, k);
            }

            if (current != GGBossAnim.Die)
            {
                float hs = (float)(Time.timeAsDouble - hurtStart);
                if (hs >= 0f && hs < 0.7f) pose = GGBossPose.Add(pose, GGBossMotion.HurtDelta(hs));
            }

            lastPose = pose;
            ApplyPose(pose);
        }

        static bool IsTimed(GGBossAnim a)
        {
            return a != GGBossAnim.Dormant && a != GGBossAnim.Idle && a != GGBossAnim.Die;
        }

        GGBossPose Target(double beat)
        {
            switch (current)
            {
                case GGBossAnim.Dormant:
                    return GGBossMotion.Dormant(beat);
                case GGBossAnim.Idle:
                    {
                        GGBossPose idle = GGBossMotion.Idle(beat, energy);
                        if (walkBlend > 0.001f) idle = GGBossPose.Lerp(idle, GGBossMotion.Walk(walkCycles, energy), walkBlend);
                        return idle;
                    }
                case GGBossAnim.Die:
                    return GGBossMotion.Die((float)(Time.timeAsDouble - startTime));
                default:
                    {
                        float t = Mathf.Max(0f, (float)(beat - startBeat));
                        return GGBossMotion.Evaluate(current, beat, t, duration, sub, energy);
                    }
            }
        }

        // ---------------------------------------------------------------- caminhada pela velocidade real
        void UpdateLocomotion(float dt)
        {
            if (motionSource == null || dt <= 0f) return;
            float x = motionSource.position.x;
            if (!hasLastX) { lastX = x; hasLastX = true; }
            float vx = (x - lastX) / dt;
            lastX = x;

            float vf = vx * Facing();                       // velocidade "para frente" (negativa = andando de costas)
            if (demoLoop && demoWalk) vf = 2.5f;
            vf = Mathf.Clamp(vf, -6f, 6f);

            bool moving = Mathf.Abs(vf) > walkThreshold;
            walkBlend = Mathf.MoveTowards(walkBlend, moving ? 1f : 0f, dt * 7f);
            if (moving) walkCycles += vf * dt / Mathf.Max(0.1f, walkStride);
        }

        /// <summary>+1 se o boss está virado para a direita no mundo, -1 para a esquerda.</summary>
        float Facing()
        {
            float artDir = artFacesLeft ? -1f : 1f;
            return transform.lossyScale.x < 0f ? -artDir : artDir;
        }

        // ---------------------------------------------------------------- aplicar nos Transforms
        void ApplyPose(GGBossPose p)
        {
            GGBossJoints j = GGBossSolver.Solve(restData, p, artFacesLeft);

            rig.localPosition = new Vector3(j.rigX, j.rigY, 0f);
            rig.localRotation = Quaternion.Euler(0f, 0f, j.rigRot);
            rig.localScale = new Vector3(j.rigSX, j.rigSY, 1f);

            hips.localPosition = new Vector3(j.hipsX, j.hipsY, 0f);
            torso.localRotation = Quaternion.Euler(0f, 0f, j.torso);
            head.localRotation = Quaternion.Euler(0f, 0f, j.head);
            armFrontUpper.localRotation = Quaternion.Euler(0f, 0f, j.armFU);
            armFrontLower.localRotation = Quaternion.Euler(0f, 0f, j.armFL);
            armBackUpper.localRotation = Quaternion.Euler(0f, 0f, j.armBU);
            armBackLower.localRotation = Quaternion.Euler(0f, 0f, j.armBL);

            thighFront.localRotation = Quaternion.Euler(0f, 0f, j.thighF);
            calfFront.localRotation = Quaternion.Euler(0f, 0f, j.calfF);
            footFront.localRotation = Quaternion.Euler(0f, 0f, j.footF);
            thighBack.localRotation = Quaternion.Euler(0f, 0f, j.thighB);
            calfBack.localRotation = Quaternion.Euler(0f, 0f, j.calfB);
            footBack.localRotation = Quaternion.Euler(0f, 0f, j.footB);
        }

        // ================================================================== demo
        sealed class DemoStep
        {
            public string name;
            public GGBossAnim anim;
            public float beats;
            public int sub;
            public bool walk;
            public bool hurt;
            public DemoStep(string n, GGBossAnim a, float b, int s, bool w, bool h)
            {
                name = n; anim = a; beats = b; sub = s; walk = w; hurt = h;
            }
        }

        static readonly DemoStep[] DemoSteps =
        {
            new DemoStep("Dormant (antes da luta)", GGBossAnim.Dormant, 3f, 0, false, false),
            new DemoStep("Intro (acorda e ruge)", GGBossAnim.Intro, 8f, 0, false, false),
            new DemoStep("Idle (dança no ritmo)", GGBossAnim.Idle, 4f, 0, false, false),
            new DemoStep("Andando", GGBossAnim.Idle, 4f, 0, true, false),
            new DemoStep("Dash 1/3: aviso", GGBossAnim.Dash, 2f, 0, false, false),
            new DemoStep("Dash 2/3: deslize (moonwalk)", GGBossAnim.Dash, 2f, 1, false, false),
            new DemoStep("Dash 3/3: recuperação", GGBossAnim.Dash, 1f, 2, false, false),
            new DemoStep("Mãos de zumbi", GGBossAnim.Hands, 6f, 0, false, false),
            new DemoStep("Giro + pisão", GGBossAnim.Spin, 4f, 0, false, false),
            new DemoStep("Orbs", GGBossAnim.Orbs, 5f, 0, false, false),
            new DemoStep("Invocar fantasmas", GGBossAnim.Summon, 4f, 0, false, false),
            new DemoStep("Mudança de fase", GGBossAnim.PhaseChange, 4f, 0, false, false),
            new DemoStep("Levando dano", GGBossAnim.Idle, 3f, 0, false, true),
            new DemoStep("Morte", GGBossAnim.Die, 7f, 0, false, false),
        };

        string demoLabel = "";
        double demoHurtAt;

        void UpdateDemo(double beat)
        {
            if (!demoLoop) { demoWalk = false; return; }

            if (demoIndex >= 0 && beat < demoNextAt)
            {
                DemoStep cur = DemoSteps[demoIndex];
                if (cur.hurt && beat >= demoHurtAt) { Hurt(); demoHurtAt = beat + 1.0; }
                return;
            }

            demoIndex = (demoIndex + 1) % DemoSteps.Length;
            DemoStep s = DemoSteps[demoIndex];
            demoNextAt = beat + s.beats;
            demoHurtAt = beat + 0.5;
            demoWalk = s.walk;
            demoLabel = (demoIndex + 1) + "/" + DemoSteps.Length + "  " + s.name;

            if (s.anim == GGBossAnim.Dormant) ResetToDormant();
            else Play(s.anim, s.beats, s.sub);   // Idle -> GoIdle; as outras tocam e voltam sozinhas
        }

        void OnGUI()
        {
            if (!demoLoop || !ready) return;
            GUI.Label(new Rect(12f, 10f, 520f, 26f), "Demo do boss:  " + demoLabel);
        }
    }
}
