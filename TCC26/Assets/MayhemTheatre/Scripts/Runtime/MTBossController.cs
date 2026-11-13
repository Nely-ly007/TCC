using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// LADY IN RED — boss final da Fase 4 (Mix Harmônico). O visual é o SEU PREFAB: este script só o instancia,
    /// move, detecta dano e dispara ataques sincronizados ao relógio de batidas. Ele NUNCA troca sprites nem toca
    /// animações por conta própria: se o Animator do prefab tiver os parâmetros listados abaixo, eles são acionados
    /// (os que não existirem são ignorados) e todos os momentos também disparam UnityEvents.
    ///
    /// Formas (transformações): 1 = 100%..66% | 2 = 66%..33% | 3 = 33%..0%.
    /// Ataques herdam os chefes anteriores: patins (Donna), enxame (Queen Bee), mãos (Zombie Jackson),
    /// mais holofotes, cortina e máscaras da própria Lady in Red.
    /// Ritmo: aviso na batida 4 -> ataque na batida 1 (forma 3 alterna aviso na 2 -> ataque na 3).
    /// </summary>
    public class MTBossController : MTBeatListener, IMTDamageable
    {
        enum BossState { Idle, Intro, Fighting, Transforming, Dead }
        enum Atk { Skates = 0, Spotlights = 1, Swarm = 2, Curtain = 3, Hands = 4, Masks = 5 }

        static readonly Atk[][] Patterns =
        {
            new[] { Atk.Skates, Atk.Spotlights, Atk.Skates, Atk.Spotlights },
            new[] { Atk.Swarm, Atk.Curtain, Atk.Skates, Atk.Spotlights, Atk.Swarm, Atk.Curtain },
            new[] { Atk.Hands, Atk.Masks, Atk.Curtain, Atk.Swarm, Atk.Spotlights, Atk.Skates, Atk.Hands, Atk.Masks }
        };

        [Header("PREFAB DO BOSS (arraste o seu prefab aqui)")]
        public GameObject bossPrefab;
        [Tooltip("Opcional: boss que já está na cena (fica inativo até a luta). Tem prioridade sobre o prefab.")]
        public GameObject existingInstance;
        [Tooltip("Só usado se não houver prefab nem instância (o builder preenche com o mestre de marionetes).")]
        public Sprite placeholderSprite;
        public float placeholderHeight = 4.5f;
        public string bossName = "LADY IN RED";
        public bool faceThePlayer = true;
        public bool spriteFacesRight = true;
        [Tooltip("Se o prefab tiver filhos chamados Form1, Form2, Form3, só o da forma atual fica ativo.")]
        public string formChildPrefix = "Form";
        public bool destroyBodyOnDeath = true;

        [Header("Arena (o builder preenche)")]
        public float arenaLeftX = 176f;
        public float arenaRightX = 212f;
        public float floorY = 0f;
        public float arenaHeight = 12f;
        [Tooltip("Altura da base do boss quando está alto (atacando).")]
        public float highBottom = 4.5f;
        [Tooltip("Altura da base do boss na janela de ataque (vulnerável, ao alcance do Player).")]
        public float lowBottom = 0.6f;
        public float swayAmplitude = 3f;
        public float moveSmooth = 0.35f;

        [Header("Vida e formas")]
        public float maxHealth = 600f;
        [Range(0.05f, 0.95f)] public float form2At = 0.66f;
        [Range(0.05f, 0.95f)] public float form3At = 0.33f;
        public float contactDamage = 30f;
        public int introBeats = 8;
        public int transformBeats = 4;
        public int deathBeats = 4;

        [Header("Animator do prefab (parâmetros opcionais — vazio/inexistente = ignora)")]
        public string pIntro = "Intro";
        public string pTelegraph = "Telegraph";
        public string pAttack = "Attack";
        public string pAttackId = "AttackId";   // int: 0 patins, 1 holofotes, 2 enxame, 3 cortina, 4 mãos, 5 máscaras
        public string pHurt = "Hurt";
        public string pTransform = "Transform";
        public string pDie = "Die";
        public string pForm = "Form";           // int 1..3
        public string pVulnerable = "Vulnerable"; // bool

        [Header("Visuais dos ataques (opcionais — vazio = formas de cor)")]
        public Sprite skateSprite;
        public Sprite beeSprite;
        public Sprite handSprite;
        public Sprite maskSprite;
        public Sprite curtainSprite;
        public Sprite[] summonSprites = new Sprite[0];
        public Sprite noteSprite;
        public GameObject notePrefab;
        public Sprite fragmentSprite;
        [Tooltip("Prefab do Fragmento do Disco Dourado (precisa de um Collider2D trigger; use MTPickup kind=Fragment ou chame MTLevelController.CompleteLevel).")]
        public GameObject fragmentPrefab;
        [Tooltip("PanelSettings do UI Toolkit para a barra de vida (o builder reaproveita o do seu HUD).")]
        public PanelSettings panelSettings;

        [Header("Eventos")]
        public UnityEvent onFightStarted = new UnityEvent();
        public UnityEvent onIntro = new UnityEvent();
        public UnityEvent<int> onTelegraph = new UnityEvent<int>();
        public UnityEvent<int> onAttack = new UnityEvent<int>();
        public UnityEvent<int> onTransform = new UnityEvent<int>();
        public UnityEvent onHurt = new UnityEvent();
        public UnityEvent onDefeated = new UnityEvent();

        public float Health { get { return health; } }
        public float Health01 { get { return maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f; } }
        public int Form { get { return form; } }
        public bool IsDead { get { return state == BossState.Dead; } }
        public bool FightActive { get { return state != BossState.Idle && state != BossState.Dead; } }

        BossState state = BossState.Idle;
        float health;
        int form = 1;
        int atkIndex;
        int notesDropped;
        bool windowOpen;
        bool invulnerable;

        Transform body;
        Rigidbody2D bodyRb;
        SpriteRenderer[] bodyRenderers = new SpriteRenderer[0];
        Color[] bodyBase = new Color[0];
        Animator anim;
        readonly Dictionary<string, AnimatorControllerParameterType> animParams = new Dictionary<string, AnimatorControllerParameterType>();
        Vector2 center;
        Vector2 centerVel;
        float halfH, halfW, offsetY, baseScaleX = 1f;
        Transform player;
        Coroutine mainCo;
        MTBossHealthBar bar;

        readonly List<GameObject> tracked = new List<GameObject>();
        readonly List<MTEnemy> adds = new List<MTEnemy>();

        int tick;
        int lastBeatInBar = -1;
        int lastHitId;
        float lastHitTime = -10f;
        float flashUntil;
        Color flashColor = Color.white;
        bool wasFlashing;

        // ---------------------------------------------------------------- batidas
        protected override void OnBeat(int beatAbs, int beatInBar, int bar)
        {
            tick++;
            lastBeatInBar = beatInBar;
        }

        IEnumerator WaitBeats(int n)
        {
            int target = tick + Mathf.Max(1, n);
            while (tick < target) yield return null;
        }

        IEnumerator WaitForBeatInBar(int b)
        {
            int start = tick;
            while (!(tick > start && lastBeatInBar == b)) yield return null;
        }

        IEnumerator WaitBars(int bars)
        {
            int count = 0;
            int last = tick;
            while (count < bars)
            {
                yield return null;
                if (tick != last)
                {
                    last = tick;
                    if (lastBeatInBar == 0) count++;
                }
            }
        }

        float BeatSec
        {
            get
            {
                MTBeatClock c = MTBeatClock.Instance;
                return c != null ? (float)c.SecondsPerBeat : 60f / 64f;
            }
        }

        float CenterX { get { return (arenaLeftX + arenaRightX) * 0.5f; } }

        // ---------------------------------------------------------------- ciclo da luta
        [ContextMenu("Iniciar luta (teste)")]
        public void BeginFight()
        {
            if (state != BossState.Idle) return;
            health = maxHealth;
            form = 1;
            atkIndex = 0;
            notesDropped = 0;
            windowOpen = false;

            SpawnBody();
            state = BossState.Intro;
            invulnerable = true;

            bar = MTBossHealthBar.Create(this, panelSettings, bossName);
            if (MTMusicDirector.Instance != null) MTMusicDirector.Instance.PlayMoment(MTMoment.MixHarmonico);
            onFightStarted.Invoke();
            mainCo = StartCoroutine(MainRoutine());
        }

        IEnumerator MainRoutine()
        {
            // espera o Mix Harmônico entrar (a troca acontece no próximo compasso)
            MTMusicDirector md = MTMusicDirector.Instance;
            float timeout = 12f;
            while (md != null && !(md.HasMoment && md.Current == MTMoment.MixHarmonico) && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Trig(pIntro);
            onIntro.Invoke();
            yield return WaitBeats(introBeats);

            invulnerable = false;
            yield return FightLoop();
        }

        IEnumerator FightLoop()
        {
            state = BossState.Fighting;
            while (state == BossState.Fighting)
            {
                // 1) janela de ataque: boss desce, fica vulnerável
                OpenWindow(true);
                int recover = form == 1 ? 2 : 1;
                yield return WaitBars(recover);
                OpenWindow(false);

                // 2) aviso (batida 4, ou batida 2 na variação da forma 3) -> 3) ataque na batida seguinte (1 ou 3)
                bool variation = form >= 3 && (atkIndex % 2 == 1);
                yield return WaitForBeatInBar(variation ? 1 : 3);

                Atk atk = NextAttack();
                SetInt(pAttackId, (int)atk);
                Trig(pTelegraph);
                onTelegraph.Invoke((int)atk);
                FlashTint(new Color(1f, 0.35f, 0.35f), BeatSec * 0.9f);
                yield return WaitBeats(1);

                Trig(pAttack);
                onAttack.Invoke((int)atk);
                yield return RunAttack(atk);
            }
        }

        Atk NextAttack()
        {
            Atk[] pat = Patterns[Mathf.Clamp(form, 1, 3) - 1];
            Atk a = pat[atkIndex % pat.Length];
            atkIndex++;
            return a;
        }

        void OpenWindow(bool open)
        {
            windowOpen = open;
            SetBool(pVulnerable, open);
        }

        // ---------------------------------------------------------------- dano / formas / morte
        public void HandleContact(Collider2D other, bool entered)
        {
            if (other == null) return;

            MTPlayerHook hook = other.GetComponentInParent<MTPlayerHook>();
            if (hook != null)
            {
                if (state == BossState.Fighting && !windowOpen && contactDamage > 0f) hook.TakeDamage(contactDamage, center);
                return;
            }
            if (!entered) return;

            int id = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(other.attachedRigidbody != null ? (UnityEngine.Object)other.attachedRigidbody : other);
            if (id == lastHitId && Time.time - lastHitTime < 0.15f) return;

            float dmg;
            if (MTDamage.TryReadPlayerShot(other, out dmg))
            {
                lastHitId = id;
                lastHitTime = Time.time;
                TakeDamage(dmg, other.transform.position);
            }
        }

        public void TakeDamage(float amount, Vector2 hitPoint)
        {
            if (state != BossState.Fighting || invulnerable || amount <= 0f) return;

            health = Mathf.Max(0f, health - amount);
            FlashTint(Color.white, 0.08f);
            Trig(pHurt);
            onHurt.Invoke();

            if (health <= 0f) Die();
            else if (form == 1 && health <= maxHealth * form2At) BeginTransform(2);
            else if (form == 2 && health <= maxHealth * form3At) BeginTransform(3);
        }

        void BeginTransform(int newForm)
        {
            if (state != BossState.Fighting) return;
            state = BossState.Transforming;
            invulnerable = true;
            OpenWindow(false);
            if (mainCo != null) StopCoroutine(mainCo);
            ClearHazards();
            mainCo = StartCoroutine(TransformRoutine(newForm));
        }

        IEnumerator TransformRoutine(int newForm)
        {
            form = newForm;
            atkIndex = 0;
            SetInt(pForm, form);
            Trig(pTransform);
            ApplyFormVisuals(form);
            onTransform.Invoke(form);
            FlashTint(Color.white, BeatSec * 1.5f);

            SummonAdds(form);
            DropNote();

            yield return WaitBeats(transformBeats);
            invulnerable = false;
            mainCo = StartCoroutine(FightLoop());
        }

        void Die()
        {
            if (state == BossState.Dead) return;
            state = BossState.Dead;
            invulnerable = true;
            OpenWindow(false);
            if (mainCo != null) StopCoroutine(mainCo);
            ClearHazards();
            ClearAdds();
            Trig(pDie);
            onDefeated.Invoke();
            if (MTLevelController.Instance != null) MTLevelController.Instance.BossDefeated();
            mainCo = StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return WaitBeats(deathBeats);

            Vector3 pos = new Vector3(CenterX, floorY + 1.4f, 0f);
            MTPickupFactory.Create(MTPickup.PickupKind.Fragment, pos, fragmentPrefab, fragmentSprite, new Color(1f, 0.82f, 0.25f), 1.2f);
            if (MTMusicDirector.Instance != null) MTMusicDirector.Instance.FadeOut(6f);

            yield return new WaitForSeconds(2f);
            if (destroyBodyOnDeath && body != null) RemoveBody();
        }

        /// <summary>Chamado pela arena quando o Player morre durante a luta.</summary>
        public void ResetFight()
        {
            StopAllCoroutines();
            ClearHazards();
            ClearAdds();
            RemoveBody();
            if (bar != null) { Destroy(bar.gameObject); bar = null; }

            state = BossState.Idle;
            health = maxHealth;
            form = 1;
            atkIndex = 0;
            windowOpen = false;
            invulnerable = false;
        }

        void RemoveBody()
        {
            if (body == null) return;
            if (existingInstance != null && body.gameObject == existingInstance) body.gameObject.SetActive(false);
            else Destroy(body.gameObject);
            body = null;
        }

        // ---------------------------------------------------------------- corpo (prefab)
        void SpawnBody()
        {
            GameObject go;
            if (existingInstance != null) { go = existingInstance; go.SetActive(true); }
            else if (bossPrefab != null) go = Instantiate(bossPrefab);
            else go = CreatePlaceholder();

            body = go.transform;
            body.position = new Vector3(CenterX, floorY + arenaHeight + 5f, 0f);
            baseScaleX = Mathf.Abs(body.localScale.x);

            bodyRb = go.GetComponent<Rigidbody2D>();
            if (bodyRb == null) bodyRb = go.AddComponent<Rigidbody2D>();
            bodyRb.bodyType = RigidbodyType2D.Kinematic;
            bodyRb.gravityScale = 0f;

            MTBossBody relay = go.GetComponent<MTBossBody>();
            if (relay == null) relay = go.AddComponent<MTBossBody>();
            relay.owner = this;

            bool found;
            Bounds b = MTUtil.RendererBounds(go, out found);
            halfH = Mathf.Max(0.5f, b.size.y * 0.5f);
            halfW = Mathf.Max(0.5f, b.size.x * 0.5f);
            offsetY = b.center.y - body.position.y;

            if (go.GetComponentInChildren<Collider2D>() == null)
            {
                Vector3 ls = go.transform.lossyScale;
                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(b.size.x / Mathf.Max(0.0001f, Mathf.Abs(ls.x)) * 0.7f, b.size.y / Mathf.Max(0.0001f, Mathf.Abs(ls.y)) * 0.8f);
                col.offset = go.transform.InverseTransformPoint(b.center);
            }

            bodyRenderers = go.GetComponentsInChildren<SpriteRenderer>();
            bodyBase = new Color[bodyRenderers.Length];
            for (int i = 0; i < bodyRenderers.Length; i++) bodyBase[i] = bodyRenderers[i].color;

            CacheAnimator();
            SetInt(pForm, 1);
            ApplyFormVisuals(1);

            center = new Vector2(CenterX, body.position.y + offsetY);
            centerVel = Vector2.zero;
            player = MTUtil.FindPlayer();
        }

        GameObject CreatePlaceholder()
        {
            GameObject go = new GameObject("MT_Boss_Placeholder");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Sprite s = placeholderSprite != null ? placeholderSprite : MTUtil.DiscSprite;
            sr.sprite = s;
            sr.sortingOrder = 2;
            sr.color = placeholderSprite != null ? Color.white : new Color(0.8f, 0.05f, 0.15f);
            float k = placeholderHeight / Mathf.Max(0.01f, s.bounds.size.y);
            go.transform.localScale = new Vector3(k, k, 1f);
            return go;
        }

        void CacheAnimator()
        {
            animParams.Clear();
            anim = body != null ? body.GetComponentInChildren<Animator>() : null;
            if (anim == null || anim.runtimeAnimatorController == null) { anim = null; return; }
            AnimatorControllerParameter[] ps = anim.parameters;
            for (int i = 0; i < ps.Length; i++) animParams[ps[i].name] = ps[i].type;
        }

        void Trig(string n)
        {
            AnimatorControllerParameterType t;
            if (anim != null && !string.IsNullOrEmpty(n) && animParams.TryGetValue(n, out t) && t == AnimatorControllerParameterType.Trigger)
                anim.SetTrigger(n);
        }

        void SetInt(string n, int v)
        {
            AnimatorControllerParameterType t;
            if (anim != null && !string.IsNullOrEmpty(n) && animParams.TryGetValue(n, out t) && t == AnimatorControllerParameterType.Int)
                anim.SetInteger(n, v);
        }

        void SetBool(string n, bool v)
        {
            AnimatorControllerParameterType t;
            if (anim != null && !string.IsNullOrEmpty(n) && animParams.TryGetValue(n, out t) && t == AnimatorControllerParameterType.Bool)
                anim.SetBool(n, v);
        }

        void ApplyFormVisuals(int f)
        {
            if (body == null || string.IsNullOrEmpty(formChildPrefix)) return;
            for (int i = 1; i <= 3; i++)
            {
                Transform t = FindDeep(body, formChildPrefix + i);
                if (t != null) t.gameObject.SetActive(i == f);
            }
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

        void FlashTint(Color c, float seconds)
        {
            flashColor = c;
            flashUntil = Time.time + seconds;
        }

        // ---------------------------------------------------------------- movimento
        void Update()
        {
            if (body == null) return;
            if (player == null) player = MTUtil.FindPlayer();

            if (state != BossState.Dead)
            {
                Vector2 desired = DesiredCenter();
                float smooth = state == BossState.Intro ? moveSmooth * 2.5f : moveSmooth;
                center = Vector2.SmoothDamp(center, desired, ref centerVel, Mathf.Max(0.05f, smooth));
                body.position = new Vector3(center.x, center.y - offsetY, body.position.z);
            }

            if (faceThePlayer && player != null)
            {
                float dx = player.position.x - center.x;
                if (Mathf.Abs(dx) > 0.5f)
                {
                    float sign = ((dx > 0f) == spriteFacesRight) ? 1f : -1f;
                    Vector3 s = body.localScale;
                    s.x = baseScaleX * sign;
                    body.localScale = s;
                }
            }

            bool f = Time.time < flashUntil;
            if (f || wasFlashing)
            {
                for (int i = 0; i < bodyRenderers.Length; i++)
                {
                    if (bodyRenderers[i] == null) continue;
                    Color c = f ? Color.Lerp(bodyBase[i], flashColor, 0.6f) : bodyBase[i];
                    c.a = bodyBase[i].a;
                    bodyRenderers[i].color = c;
                }
                wasFlashing = f;
            }
        }

        Vector2 DesiredCenter()
        {
            float cx = CenterX;
            float sway = 0f;
            MTBeatClock clk = MTBeatClock.Instance;
            if (clk != null && clk.Running) sway = Mathf.Sin((float)clk.BeatPosition * Mathf.PI / 4f) * swayAmplitude;

            if (state == BossState.Intro || state == BossState.Transforming)
                return new Vector2(cx, floorY + highBottom + halfH);

            if (windowOpen)
            {
                float px = player != null ? player.position.x : cx;
                float side = px < cx ? 1f : -1f;
                float x = Mathf.Clamp(px + side * (halfW * 0.75f + 1.0f), arenaLeftX + halfW, arenaRightX - halfW);
                return new Vector2(x, floorY + lowBottom + halfH);
            }
            return new Vector2(cx + sway, floorY + highBottom + halfH);
        }

        // ---------------------------------------------------------------- ataques
        IEnumerator RunAttack(Atk a)
        {
            switch (a)
            {
                case Atk.Skates: yield return Atk_Skates(); break;
                case Atk.Spotlights: yield return Atk_Spotlights(); break;
                case Atk.Swarm: yield return Atk_Swarm(); break;
                case Atk.Curtain: yield return Atk_Curtain(); break;
                case Atk.Hands: yield return Atk_Hands(); break;
                case Atk.Masks: yield return Atk_Masks(); break;
            }
        }

        float PlayerX { get { return player != null ? player.position.x : CenterX; } }
        Vector2 PlayerPos { get { return player != null ? (Vector2)player.position : new Vector2(CenterX, floorY + 1f); } }

        void Track(GameObject g)
        {
            if (tracked.Count > 64) tracked.RemoveAll(x => x == null);
            tracked.Add(g);
        }

        void ClearHazards()
        {
            for (int i = 0; i < tracked.Count; i++)
                if (tracked[i] != null) Destroy(tracked[i]);
            tracked.Clear();
        }

        // Patins (Donna): entram pela borda mais distante do Player e rolam pelo chão
        IEnumerator Atk_Skates()
        {
            int count = form == 1 ? 1 : (form == 2 ? 2 : 3);
            for (int i = 0; i < count; i++)
            {
                bool fromLeft = PlayerX > CenterX;
                if (i % 2 == 1) fromLeft = !fromLeft;
                float x = fromLeft ? arenaLeftX + 0.8f : arenaRightX - 0.8f;
                float dir = fromLeft ? 1f : -1f;
                Color tint = skateSprite != null ? Color.white : new Color(1f, 0.35f, 0.75f);
                Track(MTHazardFactory.Shot(new Vector2(x, floorY + 0.4f), new Vector2(dir * 7.5f, 0f), skateSprite,
                    new Vector2(1.4f, 0.8f), 15f, 9f, tint).gameObject);
                yield return WaitBeats(1);
            }
            yield return WaitBeats(1);
        }

        // Holofotes: coluna de luz avisa 1 batida e depois queima (dano forte)
        IEnumerator Atk_Spotlights()
        {
            int count = form == 1 ? 2 : (form == 2 ? 3 : 4);
            float beat = BeatSec;
            if (form < 3)
            {
                float px = PlayerX;
                for (int i = 0; i < count; i++)
                {
                    float off = i == 0 ? 0f : (i % 2 == 1 ? 1f : -1f) * (4f + 3f * (i / 2));
                    SpawnColumn(Mathf.Clamp(px + off, arenaLeftX + 1f, arenaRightX - 1f), beat, beat);
                }
                yield return WaitBeats(3);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    float x = Mathf.Clamp(PlayerX + Random.Range(-1f, 1f), arenaLeftX + 1f, arenaRightX - 1f);
                    SpawnColumn(x, beat, beat * 0.9f);
                    yield return WaitBeats(1);
                }
                yield return WaitBeats(2);
            }
        }

        void SpawnColumn(float x, float tele, float active)
        {
            float h = arenaHeight;
            float cy = floorY + h * 0.5f;
            Track(MTHazardFactory.Timed(
                new Vector2(x, cy), new Vector2(0.3f, h), new Color(1f, 0.95f, 0.5f, 0.35f),
                null, new Color(1f, 0.95f, 0.65f, 0.6f), new Vector2(x, cy), new Vector2(1.6f, h), 7,
                new Vector2(x, cy), new Vector2(1.4f, h), tele, active, 30f, Vector2.zero).gameObject);
        }

        // Enxame (Queen Bee): leque de abelhas que ondulam em direção ao Player
        IEnumerator Atk_Swarm()
        {
            int n = form == 3 ? 7 : 5;
            Vector2 from = center;
            Vector2 aim = PlayerPos - from;
            if (aim.sqrMagnitude < 0.01f) aim = Vector2.down;
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            Color tint = beeSprite != null ? Color.white : new Color(1f, 0.8f, 0.1f);
            for (int i = 0; i < n; i++)
            {
                float t = n > 1 ? (float)i / (n - 1) : 0.5f;
                float ang = (baseAngle + Mathf.Lerp(-40f, 40f, t)) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 4.5f;
                Track(MTHazardFactory.Shot(from, vel, beeSprite, new Vector2(0.55f, 0.55f), 15f, 6f, tint, 0f, 0.6f, 4f).gameObject);
            }
            yield return WaitBeats(2);
        }

        // Cortina: 2 batidas de aviso, depois cai (dano forte). Largura 10 => sempre dá para sair a tempo.
        IEnumerator Atk_Curtain()
        {
            float beat = BeatSec;
            float x1 = Mathf.Clamp(PlayerX, arenaLeftX + 5f, arenaRightX - 5f);
            SpawnCurtain(x1, beat * 2f, beat);
            if (form >= 3)
            {
                float x2 = Mathf.Clamp(2f * CenterX - x1, arenaLeftX + 5f, arenaRightX - 5f);
                if (Mathf.Abs(x2 - x1) > 11f) SpawnCurtain(x2, beat * 2f, beat);
            }
            yield return WaitBeats(4);
        }

        void SpawnCurtain(float x, float tele, float active)
        {
            float h = arenaHeight;
            float w = 10f;
            float cy = floorY + h * 0.5f;
            Color ac = curtainSprite != null ? Color.white : new Color(0.75f, 0.04f, 0.1f, 0.9f);
            Track(MTHazardFactory.Timed(
                new Vector2(x, cy), new Vector2(w, h), new Color(0.9f, 0.05f, 0.15f, 0.28f),
                curtainSprite, ac, new Vector2(x, cy), new Vector2(w, h), 6,
                new Vector2(x, cy), new Vector2(w * 0.9f, h), tele, active, 30f, new Vector2(0f, h + 1f)).gameObject);
        }

        // Mãos (Zombie Jackson): uma por batida, na posição do Player
        IEnumerator Atk_Hands()
        {
            float beat = BeatSec;
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Clamp(PlayerX + Random.Range(-1.2f, 1.2f), arenaLeftX + 1f, arenaRightX - 1f);
                SpawnHand(x, beat, beat * 0.8f);
                yield return WaitBeats(1);
            }
            yield return WaitBeats(1);
        }

        void SpawnHand(float x, float tele, float active)
        {
            float h = 2.8f;
            float w = handSprite != null ? h * (handSprite.bounds.size.x / Mathf.Max(0.001f, handSprite.bounds.size.y)) : 0.9f;
            Color ac = handSprite != null ? Color.white : new Color(0.85f, 0.75f, 0.65f, 1f);
            Track(MTHazardFactory.Timed(
                new Vector2(x, floorY + 0.15f), new Vector2(w * 1.1f, 0.3f), new Color(0.35f, 0.25f, 0.2f, 0.7f),
                handSprite, ac, new Vector2(x, floorY + h * 0.5f), new Vector2(w, h), -1,
                new Vector2(x, floorY + h * 0.5f), new Vector2(w * 0.8f, h * 0.9f), tele, active, 15f, new Vector2(0f, -h)).gameObject);
        }

        // Máscaras (Lady in Red): leque mirado no Player
        IEnumerator Atk_Masks()
        {
            Vector2 from = center;
            Vector2 aim = PlayerPos - from;
            if (aim.sqrMagnitude < 0.01f) aim = Vector2.down;
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            float[] angles = form >= 3 ? new[] { -30f, -15f, 0f, 15f, 30f } : new[] { -20f, 0f, 20f };
            Color tint = maskSprite != null ? Color.white : new Color(0.95f, 0.95f, 0.9f);
            for (int i = 0; i < angles.Length; i++)
            {
                float a = (baseAngle + angles[i]) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 6.5f;
                Track(MTHazardFactory.Shot(from, vel, maskSprite, new Vector2(0.95f, 0.95f), 15f, 6f, tint, 240f).gameObject);
            }
            yield return WaitBeats(2);
        }

        // ---------------------------------------------------------------- extras
        void SummonAdds(int f)
        {
            ClearAdds();
            int n = f == 2 ? 2 : 3;
            float[] xs = { arenaLeftX + 3f, arenaRightX - 3f, CenterX };
            for (int i = 0; i < n; i++)
            {
                Sprite s = summonSprites != null && summonSprites.Length > 0 ? summonSprites[(i + f) % summonSprites.Length] : null;
                MTEnemy e = MTEnemy.Create(new Vector3(xs[i], floorY + 0.3f, 0f), MTEnemyKind.Chaser, s, Color.white, 1.7f,
                    20f, 15f, 3f, 40f, noteSprite, notePrefab);
                e.noteDropChance = 0f;
                adds.Add(e);
            }
        }

        void ClearAdds()
        {
            for (int i = 0; i < adds.Count; i++)
                if (adds[i] != null) Destroy(adds[i].gameObject);
            adds.Clear();
        }

        // Máx. 2 notas de cura por luta (uma em cada transformação)
        void DropNote()
        {
            if (notesDropped >= 2) return;
            notesDropped++;
            float x = CenterX + (notesDropped == 1 ? -5f : 5f);
            MTPickupFactory.Create(MTPickup.PickupKind.Note, new Vector3(x, floorY + 1.2f, 0f), notePrefab, noteSprite, new Color(1f, 0.85f, 0.2f), 0.6f);
        }
    }
}
