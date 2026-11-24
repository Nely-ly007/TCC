using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Boss da Fase 3 (Graveyard Groove). A LÓGICA é toda por código e no ritmo da música (114 BPM).
    /// A ANIMAÇÃO é AUTOMÁTICA: o GGBossRig (no objeto Visual) move as peças do desenho pelo código.
    /// Se você preferir animar à mão, deixe o campo "Rig" vazio e use um Animator: o script dispara os parâmetros abaixo
    /// (se um parâmetro não existir, é ignorado em silêncio).
    ///
    /// Parâmetros do Animator (só usados quando NÃO há GGBossRig):
    ///   Bool    IsMoving      (andando/deslizando)
    ///   Trigger Intro         (entrada, ~8 batidas)
    ///   Trigger AttackDash    (aviso do deslize; depois ela desliza)
    ///   Trigger AttackHands   (invoca mãos de zumbi)
    ///   Trigger AttackSpin    (giro + ondas de choque no chão)
    ///   Trigger AttackOrbs    (dispara orbs; fase 2+)
    ///   Trigger AttackSummon  (invoca fantasmas; fase 2+)
    ///   Trigger Hurt          (levou dano)
    ///   Trigger PhaseChange   (mudança de fase, ~4 batidas)
    ///   Trigger Die           (derrotado)
    /// </summary>
    public class GGBoss : MonoBehaviour, IGGDamageable
    {
        public enum State { Dormant, Intro, Fighting, Transition, Dead }
        enum Attack { Dash, Hands, Spin, Orbs, Ghosts }

        [Header("Identidade")]
        [SerializeField] string bossName = "Boss";

        [Header("Vida")]
        [SerializeField] int maxHealth = 250;
        [Range(0.05f, 0.95f)] [SerializeField] float phase2At = 0.66f;
        [Range(0.05f, 0.95f)] [SerializeField] float phase3At = 0.33f;
        [Tooltip("Notas de cura que o boss deixa cair por luta (máx. 2).")]
        [SerializeField] int maxHealDropsPerFight = 2;

        [Header("Dano")]
        [SerializeField] int contactDamage = 15;
        [SerializeField] int dashDamage = 30;
        [SerializeField] int handsDamage = 15;
        [SerializeField] int waveDamage = 15;
        [SerializeField] int orbDamage = 15;

        [Header("Arena (a raiz do Boss fica nos PÉS)")]
        [SerializeField] Transform leftLimit;
        [SerializeField] Transform rightLimit;
        [Tooltip("De onde saem os orbs (o X é sempre 'para frente', vire o boss para qualquer lado). Se vazio, usa 1.6 unidades acima dos pés.")]
        [SerializeField] Transform muzzle;
        [SerializeField] float walkSpeed = 2.5f;
        [SerializeField] float preferredDistance = 5f;
        [SerializeField] float dashUnitsPerBeat = 6f;

        [Header("Visual (rig automático ou Animator)")]
        [SerializeField] Transform visual;
        [Tooltip("Animação automática por código. Se atribuído, o Animator abaixo não é necessário.")]
        [SerializeField] GGBossRig rig;
        [SerializeField] Animator animator;
        [Tooltip("Marque se o sprite original olha para a DIREITA. Ignorado quando há GGBossRig (ele já sabe).")]
        [SerializeField] bool spriteFacesRight = true;
        [SerializeField] bool flashOnHit = true;
        [SerializeField] GGWorldHealthBar healthBar;

        [Header("Nomes dos parâmetros do Animator")]
        [SerializeField] string pIsMoving = "IsMoving";
        [SerializeField] string tIntro = "Intro";
        [SerializeField] string tDash = "AttackDash";
        [SerializeField] string tHands = "AttackHands";
        [SerializeField] string tSpin = "AttackSpin";
        [SerializeField] string tOrbs = "AttackOrbs";
        [SerializeField] string tSummon = "AttackSummon";
        [SerializeField] string tHurt = "Hurt";
        [SerializeField] string tPhaseChange = "PhaseChange";
        [SerializeField] string tDie = "Die";
        [SerializeField] bool fireHurtTrigger = true;

        [Header("Prefabs (o construtor preenche)")]
        [SerializeField] GameObject zombieHandPrefab;
        [SerializeField] GameObject shockwavePrefab;
        [SerializeField] GameObject orbPrefab;
        [SerializeField] GameObject ghostPrefab;
        [SerializeField] GameObject healNotePrefab;
        [SerializeField] GameObject fragmentPrefab;

        [Header("Ritmo")]
        [SerializeField] int introBeats = 8;

        [Header("Eventos (ligue ao seu HUD / GameManager)")]
        public UnityEvent onFightStarted;
        [Tooltip("0..1 — ligue ao seu HUD em UI Toolkit.")]
        public GGFloatEvent onHealthNormalized;
        public GGIntEvent onPhaseChanged;
        public UnityEvent onHurt;
        public UnityEvent onDefeated;

        public event System.Action Defeated;

        public State CurrentState { get; private set; }
        public int Phase { get; private set; }
        public string BossName { get { return bossName; } }

        int health;
        int healDrops;
        int currentContact;
        Vector3 startPos;
        Attack lastAttack;
        bool hasLast;
        float flashUntil;
        float facing = -1f;
        float lastHurtTrigger = -10f;
        bool wasFlashing;
        SpriteRenderer[] flashRenderers;
        Color[] baseColors;
        readonly HashSet<int> paramHashes = new HashSet<int>();
        readonly List<GameObject> spawned = new List<GameObject>();
        readonly List<GGGhost> ghosts = new List<GGGhost>();

        // ------------------------------------------------------------------ setup
        void Awake()
        {
            Phase = 1;
            health = maxHealth;
            startPos = transform.position;
            currentContact = contactDamage;
            CurrentState = State.Dormant;

            if (rig == null && visual != null) rig = visual.GetComponent<GGBossRig>();
            if (rig == null) rig = GetComponentInChildren<GGBossRig>();
            if (visual == null && rig != null) visual = rig.transform;
            if (rig != null)
            {
                spriteFacesRight = !rig.ArtFacesLeft;
                rig.SetPhase(1);
            }
            facing = spriteFacesRight ? 1f : -1f;

            if (animator == null && visual != null) animator = visual.GetComponent<Animator>();
            if (visual == null && animator != null) visual = animator.transform;
            if (healthBar == null) healthBar = GetComponentInChildren<GGWorldHealthBar>();

            CacheParameters();

            flashRenderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>() : new SpriteRenderer[0];
            baseColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++) baseColors[i] = flashRenderers[i].color;
        }

        void CacheParameters()
        {
            paramHashes.Clear();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            AnimatorControllerParameter[] ps = animator.parameters;
            for (int i = 0; i < ps.Length; i++) paramHashes.Add(ps[i].nameHash);
        }

        void Trig(string parameter)
        {
            if (animator == null || string.IsNullOrEmpty(parameter)) return;
            int h = Animator.StringToHash(parameter);
            if (paramHashes.Contains(h)) animator.SetTrigger(h);
        }

        void SetBool(string parameter, bool value)
        {
            if (animator == null || string.IsNullOrEmpty(parameter)) return;
            int h = Animator.StringToHash(parameter);
            if (paramHashes.Contains(h)) animator.SetBool(h, value);
        }

        /// <summary>Dispara a animação: no rig automático (se houver) e/ou no Animator (parâmetro opcional).</summary>
        void Anim(string trigger, GGBossAnim anim, float beats, int stage = 0, double startBeat = double.NaN)
        {
            if (rig != null) rig.Play(anim, beats, stage, startBeat);
            Trig(trigger);
        }

        // ------------------------------------------------------------------ helpers
        float X { get { return transform.position.x; } }
        float Left { get { return leftLimit != null ? leftLimit.position.x : startPos.x - 12f; } }
        float Right { get { return rightLimit != null ? rightLimit.position.x : startPos.x + 12f; } }

        /// <summary>Boca dos orbs: o offset do Muzzle é espelhado conforme o lado para onde o boss olha.</summary>
        Vector3 MuzzlePos
        {
            get
            {
                Vector3 local = muzzle != null ? transform.InverseTransformPoint(muzzle.position) : new Vector3(0f, 1.6f, 0f);
                local.x = Mathf.Abs(local.x) * facing;
                return transform.TransformPoint(local);
            }
        }

        void SetX(float x)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(x, Left, Right);
            transform.position = p;
        }

        float DirToPlayer()
        {
            Transform p = GGUtil.Player;
            if (p == null) return -1f;
            return p.position.x >= X ? 1f : -1f;
        }

        void Face(float dir)
        {
            if (Mathf.Abs(dir) < 0.01f) return;
            facing = dir > 0f ? 1f : -1f;
            if (visual == null) return;
            Vector3 s = visual.localScale;
            float sign = ((dir > 0f) == spriteFacesRight) ? 1f : -1f;
            s.x = Mathf.Abs(s.x) * sign;
            visual.localScale = s;
        }

        void Track(GameObject go)
        {
            if (go == null) return;
            if (spawned.Count > 80) spawned.RemoveAll(g => g == null);
            spawned.Add(go);
        }

        void ClearHazards()
        {
            for (int i = 0; i < spawned.Count; i++)
                if (spawned[i] != null) Destroy(spawned[i]);
            spawned.Clear();
            ghosts.Clear();
        }

        int AliveGhosts()
        {
            ghosts.RemoveAll(g => g == null || g.IsDead);
            return ghosts.Count;
        }

        int RestBeats()
        {
            return Phase == 1 ? 4 : (Phase == 2 ? 3 : 2);
        }

        // ------------------------------------------------------------------ luta
        /// <summary>Chamado pela GGBossArena quando o Player entra e os portões fecham.</summary>
        public void BeginFight()
        {
            if (CurrentState != State.Dormant) return;
            CurrentState = State.Intro;
            if (healthBar != null) { healthBar.SetValue(1f); healthBar.SetVisible(true); }
            if (onHealthNormalized != null) onHealthNormalized.Invoke(1f);
            StartCoroutine(IntroThenFight());
        }

        IEnumerator IntroThenFight()
        {
            Face(DirToPlayer());
            Anim(tIntro, GGBossAnim.Intro, introBeats);
            GGCameraFollow.Shake(0.3f, 0.5f);
            yield return StartCoroutine(GGBeat.Wait(introBeats));
            if (onFightStarted != null) onFightStarted.Invoke();
            yield return StartCoroutine(FightLoop());
        }

        IEnumerator FightLoop()
        {
            CurrentState = State.Fighting;
            while (CurrentState == State.Fighting)
            {
                Attack atk = PickAttack();
                lastAttack = atk;
                hasLast = true;
                yield return StartCoroutine(RunAttack(atk));
                yield return StartCoroutine(Reposition(RestBeats()));
            }
        }

        Attack PickAttack()
        {
            List<Attack> pool = new List<Attack> { Attack.Dash, Attack.Hands, Attack.Spin };
            if (Phase >= 2)
            {
                pool.Add(Attack.Orbs);
                if (AliveGhosts() < 2) pool.Add(Attack.Ghosts);
            }
            if (hasLast && pool.Count > 1) pool.Remove(lastAttack);
            return pool[Random.Range(0, pool.Count)];
        }

        IEnumerator RunAttack(Attack atk)
        {
            switch (atk)
            {
                case Attack.Dash: yield return StartCoroutine(DashAttack()); break;
                case Attack.Hands: yield return StartCoroutine(HandsAttack()); break;
                case Attack.Spin: yield return StartCoroutine(SpinAttack()); break;
                case Attack.Orbs: yield return StartCoroutine(OrbsAttack()); break;
                case Attack.Ghosts: yield return StartCoroutine(GhostsAttack()); break;
            }
        }

        // Descanso rítmico: anda para manter distância do Player (vulnerável).
        IEnumerator Reposition(int beats)
        {
            double end = GGBeatClock.Position + beats;
            while (GGBeatClock.Position < end && CurrentState == State.Fighting)
            {
                bool moving = false;
                Transform p = GGUtil.Player;
                if (p != null)
                {
                    float dx = p.position.x - X;
                    Face(dx);
                    float side = dx >= 0f ? 1f : -1f;
                    float want = Mathf.Clamp(p.position.x - side * preferredDistance, Left, Right);
                    if (Mathf.Abs(want - X) > 0.3f)
                    {
                        SetX(Mathf.MoveTowards(X, want, walkSpeed * Time.deltaTime));
                        moving = true;
                    }
                }
                SetBool(pIsMoving, moving);
                yield return null;
            }
            SetBool(pIsMoving, false);
        }

        IEnumerator MoveX(float toX, double beats)
        {
            double start = GGBeatClock.Position;
            float fromX = X;
            while (true)
            {
                float t = Mathf.Clamp01((float)((GGBeatClock.Position - start) / beats));
                SetX(Mathf.Lerp(fromX, toX, Mathf.SmoothStep(0f, 1f, t)));
                if (t >= 1f) break;
                yield return null;
            }
        }

        // ------------------------------------------------------------------ ataques
        // 1) Deslize (moonwalk): aviso de 2 batidas (1 na fase 3) e atravessa a arena. Fase 3: vai e volta.
        IEnumerator DashAttack()
        {
            float firstDir = DirToPlayer();
            Face(firstDir);
            float warn = Phase >= 3 ? 1f : 2f;
            Anim(tDash, GGBossAnim.Dash, warn, 0);                    // agacha e treme (aviso)
            yield return StartCoroutine(GGBeat.Wait(warn));

            int dashes = Phase >= 3 ? 2 : 1;
            for (int i = 0; i < dashes; i++)
            {
                float dir = (i % 2 == 0) ? firstDir : -firstDir;
                float toX = dir > 0f ? Right : Left;
                double beats = Mathf.Max(2f, Mathf.Abs(toX - X) / dashUnitsPerBeat);
                Face(dir);
                currentContact = dashDamage;
                SetBool(pIsMoving, true);
                Anim(null, GGBossAnim.Dash, (float)beats, 1);          // deslize
                yield return StartCoroutine(MoveX(toX, beats));
                SetBool(pIsMoving, false);
                currentContact = contactDamage;
                if (i < dashes - 1)
                {
                    Face(-dir);                                        // vira e avisa de novo (1 batida)
                    Anim(null, GGBossAnim.Dash, 1f, 0);
                    yield return StartCoroutine(GGBeat.Wait(1.0));
                }
            }
            Anim(null, GGBossAnim.Dash, 1f, 2);                        // recuperação
            yield return StartCoroutine(GGBeat.Wait(1.0));
        }

        // 2) Onda de mãos de zumbi: uma mão a cada meia batida; sempre sobra um vão.
        IEnumerator HandsAttack()
        {
            float dir = DirToPlayer();
            Face(dir);

            int count = Phase == 1 ? 5 : (Phase == 2 ? 7 : 9);
            double now = GGBeatClock.Position;
            double startBeat = System.Math.Floor(now + 1.0) + 1.0;     // 1ª batida depois de >= 1 batida de aviso
            double endBeat = startBeat + count * 0.5 + 2.6;
            Anim(tHands, GGBossAnim.Hands, (float)(endBeat - now), 0, now);

            yield return StartCoroutine(GGBeat.WaitUntil(now + 1.0));
            int gap = Random.Range(1, count - 1);
            const float spacing = 2.4f;
            for (int i = 0; i < count; i++)
            {
                if (i == gap) continue;
                float x = X + dir * (2.2f + spacing * i);
                if (x < Left || x > Right) continue;
                SpawnHand(x, startBeat + i * 0.5);
            }
            yield return StartCoroutine(GGBeat.WaitUntil(endBeat));
        }

        // 3) Giro: ondas de choque pelo chão para os dois lados (pule!). Fase 1: 1 onda, fase 2: 2, fase 3: 3.
        //    Cada onda = um giro com pulinho; o pisão (e a onda) cai EXATAMENTE numa batida.
        IEnumerator SpinAttack()
        {
            float spinBeats = Phase == 1 ? 4f : 3f;                    // duração de cada giro
            const float stompBefore = 0.85f;                           // o pisão acontece em (duração - 0.85) — ver GGBossMotion.Spin
            for (int w = 0; w < Phase; w++)
            {
                double land = System.Math.Ceiling(GGBeatClock.Position + (spinBeats - stompBefore));
                double start = land - (spinBeats - stompBefore);
                yield return StartCoroutine(GGBeat.WaitUntil(start));
                Anim(tSpin, GGBossAnim.Spin, spinBeats, 0, start);
                yield return StartCoroutine(GGBeat.WaitUntil(land));
                SpawnShockwave(-1f);
                SpawnShockwave(1f);
                GGCameraFollow.Shake(0.15f, 0.2f);
                yield return StartCoroutine(GGBeat.WaitUntil(start + spinBeats));
            }
            yield return StartCoroutine(GGBeat.Wait(1.0));
        }

        // 4) Orbs mirados (fase 2+). Podem ser destruídos com a nota. Fase 3: leque de 3.
        //    Cada orb sai numa batida, junto com o empurrão das mãos.
        IEnumerator OrbsAttack()
        {
            int volleys = Phase >= 3 ? 4 : 3;
            double now = GGBeatClock.Position;
            double first = System.Math.Floor(now + 1.0) + 1.0;         // >= 1 batida de aviso
            double endBeat = first + (volleys - 1) + 1.5;
            Anim(tOrbs, GGBossAnim.Orbs, (float)(endBeat - now), 0, now);

            for (int v = 0; v < volleys; v++)
            {
                yield return StartCoroutine(GGBeat.WaitUntil(first + v));
                Transform p = GGUtil.Player;
                Vector2 muzzle2 = MuzzlePos;
                Vector2 dir = p != null ? ((Vector2)(p.position + Vector3.up * 0.8f) - muzzle2).normalized : Vector2.left;
                Face(dir.x);
                if (Phase >= 3)
                {
                    SpawnOrb(GGUtil.Rotate(dir, -18f));
                    SpawnOrb(dir);
                    SpawnOrb(GGUtil.Rotate(dir, 18f));
                }
                else SpawnOrb(dir);
            }
            yield return StartCoroutine(GGBeat.WaitUntil(endBeat));
        }

        // 5) Invoca dois fantasmas que perseguem no ritmo (fase 2+). Braços sobem, pisão em t = 2 batidas.
        IEnumerator GhostsAttack()
        {
            Anim(tSummon, GGBossAnim.Summon, 4f);
            yield return StartCoroutine(GGBeat.Wait(2.0));
            float y = transform.position.y + 4f;
            SpawnGhost(new Vector3(Left + 1.5f, y, 0f));
            SpawnGhost(new Vector3(Right - 1.5f, y, 0f));
            yield return StartCoroutine(GGBeat.Wait(2.0));
        }

        // ------------------------------------------------------------------ spawns
        void SpawnHand(float x, double startBeat)
        {
            if (zombieHandPrefab == null) return;
            // prefabs do construtor têm a raiz no CENTRO da célula (0.5 acima do chão)
            GameObject go = Instantiate(zombieHandPrefab, new Vector3(x, transform.position.y + 0.5f, 0f), Quaternion.identity);
            Track(go);
            GGZombieHand hand = go.GetComponent<GGZombieHand>();
            if (hand != null)
            {
                hand.SetDamage(handsDamage);
                hand.StartOneShot(startBeat, 1f, 1f);
            }
        }

        void SpawnShockwave(float dir)
        {
            Vector3 pos = new Vector3(X + dir * 1.2f, transform.position.y + 0.55f, 0f);
            GGBossProjectile bp = null;
            if (shockwavePrefab != null)
            {
                GameObject go = Instantiate(shockwavePrefab, pos, Quaternion.identity);
                bp = go.GetComponent<GGBossProjectile>();
                if (bp == null) { Destroy(go); return; }
            }
            else bp = GGBossProjectile.CreateFallback(pos, new Color(0.5f, 1f, 0.4f, 0.9f), 1.1f, true, false);

            Track(bp.gameObject);
            Vector3 s = bp.transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir > 0f ? 1f : -1f);
            bp.transform.localScale = s;
            float speed = 7f + Phase;
            float life = (Right - Left) / speed + 0.6f;
            bp.Launch(new Vector2(dir * speed, 0f), waveDamage, life);
        }

        void SpawnOrb(Vector2 dir)
        {
            Vector3 pos = MuzzlePos;
            GGBossProjectile bp = null;
            if (orbPrefab != null)
            {
                GameObject go = Instantiate(orbPrefab, pos, Quaternion.identity);
                bp = go.GetComponent<GGBossProjectile>();
                if (bp == null) { Destroy(go); return; }
            }
            else bp = GGBossProjectile.CreateFallback(pos, new Color(1f, 0.35f, 0.8f, 1f), 0.7f, false, true);

            Track(bp.gameObject);
            bp.Launch(dir.normalized * 7f, orbDamage, 4.5f);
        }

        void SpawnGhost(Vector3 pos)
        {
            if (ghostPrefab == null) return;
            GameObject go = Instantiate(ghostPrefab, pos, Quaternion.identity);
            Track(go);
            GGGhost g = go.GetComponent<GGGhost>();
            if (g != null)
            {
                g.SetDropChance(0f);
                g.SetAlwaysChase();
                ghosts.Add(g);
            }
        }

        void TryDropHealNote()
        {
            if (healNotePrefab == null || healDrops >= maxHealDropsPerFight) return;
            healDrops++;
            float x = Mathf.Lerp(Left + 3f, Right - 3f, Random.value);
            Track(Instantiate(healNotePrefab, new Vector3(x, transform.position.y + 1f, 0f), Quaternion.identity));
        }

        // ------------------------------------------------------------------ dano / fases / morte
        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (CurrentState != State.Fighting || amount <= 0) return;

            health = Mathf.Max(0, health - amount);
            float n = (float)health / maxHealth;
            if (healthBar != null) healthBar.SetValue(n);
            if (onHealthNormalized != null) onHealthNormalized.Invoke(n);
            flashUntil = Time.time + 0.1f;
            if (rig != null) rig.Hurt();
            if (fireHurtTrigger && Time.time - lastHurtTrigger > 0.8f)
            {
                Trig(tHurt);
                lastHurtTrigger = Time.time;
            }
            if (onHurt != null) onHurt.Invoke();

            if (health <= 0) { Die(); return; }

            int newPhase = n <= phase3At ? 3 : (n <= phase2At ? 2 : 1);
            if (newPhase > Phase)
            {
                StopAllCoroutines();
                StartCoroutine(PhaseTransition(newPhase));
            }
        }

        IEnumerator PhaseTransition(int newPhase)
        {
            CurrentState = State.Transition;
            Phase = newPhase;
            currentContact = contactDamage;
            SetBool(pIsMoving, false);
            ClearHazards();
            if (rig != null) rig.SetPhase(Phase);
            Anim(tPhaseChange, GGBossAnim.PhaseChange, 4f);
            if (onPhaseChanged != null) onPhaseChanged.Invoke(Phase);
            GGCameraFollow.Shake(0.5f, 0.7f);
            TryDropHealNote();
            yield return StartCoroutine(GGBeat.Wait(4.0));
            yield return StartCoroutine(FightLoop());
        }

        void Die()
        {
            if (CurrentState == State.Dead) return;
            CurrentState = State.Dead;
            StopAllCoroutines();
            ClearHazards();
            SetBool(pIsMoving, false);
            Anim(tDie, GGBossAnim.Die, 0f);
            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            if (healthBar != null) healthBar.SetVisible(false);
            if (onHealthNormalized != null) onHealthNormalized.Invoke(0f);
            GGCameraFollow.Shake(0.6f, 1.2f);
            if (onDefeated != null) onDefeated.Invoke();
            if (Defeated != null) Defeated();
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(1.8f);
            if (fragmentPrefab != null)
                Instantiate(fragmentPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            else
                Debug.LogWarning("GGBoss: 'Fragment Prefab' não atribuído — o fragmento do Disco Dourado não será criado.", this);
        }

        /// <summary>Volta o boss ao estado inicial (usado quando o Player morre durante a luta).</summary>
        public void ResetFight()
        {
            StopAllCoroutines();
            ClearHazards();
            health = maxHealth;
            Phase = 1;
            healDrops = 0;
            hasLast = false;
            currentContact = contactDamage;
            transform.position = startPos;
            CurrentState = State.Dormant;

            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = true;
            if (healthBar != null) { healthBar.SetValue(1f); healthBar.SetVisible(false); }
            if (onHealthNormalized != null) onHealthNormalized.Invoke(1f);
            if (rig != null) { rig.ResetToDormant(); rig.SetPhase(1); }
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (CurrentState == State.Dormant || CurrentState == State.Intro || CurrentState == State.Dead) return;
            GGPlayerHealth h = other.GetComponentInParent<GGPlayerHealth>();
            if (h != null) h.TakeDamage(currentContact, transform.position);
        }

        void LateUpdate()
        {
            if (!flashOnHit || flashRenderers == null) return;
            bool flash = Time.time < flashUntil;
            if (flash)
            {
                for (int i = 0; i < flashRenderers.Length; i++)
                    if (flashRenderers[i] != null)
                        flashRenderers[i].color = Color.Lerp(baseColors[i], new Color(1f, 0.6f, 0.6f, baseColors[i].a), 0.8f);
                wasFlashing = true;
            }
            else if (wasFlashing)
            {
                for (int i = 0; i < flashRenderers.Length; i++)
                    if (flashRenderers[i] != null) flashRenderers[i].color = baseColors[i];
                wasFlashing = false;
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 p = Application.isPlaying ? startPos : transform.position;
            float l = leftLimit != null ? leftLimit.position.x : p.x - 12f;
            float r = rightLimit != null ? rightLimit.position.x : p.x + 12f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(l, p.y, 0f), new Vector3(r, p.y, 0f));
            Gizmos.DrawLine(new Vector3(l, p.y, 0f), new Vector3(l, p.y + 3f, 0f));
            Gizmos.DrawLine(new Vector3(r, p.y, 0f), new Vector3(r, p.y + 3f, 0f));
        }
    }
}
