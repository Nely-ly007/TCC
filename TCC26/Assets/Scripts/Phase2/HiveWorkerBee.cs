using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Abelha operária voadora (20 HP, 15 de dano). Ciclo de 4 batidas:
    /// batida 1 = telegrafa (pisca/treme), batida 3 = mergulha na direção em que o jogador estava,
    /// depois recua. Quando está longe, patrulha em vaivém sincronizado à música.
    /// ABELHAS DA RAINHA (filhas do HiveQueenArena) são mais fáceis: no máximo MinionMax vivas, atacam a cada
    /// MinionAttackEveryBars compassos, a mira trava na batida 2 (dá tempo de desviar), mergulho mais lento,
    /// dano menor, alcance menor e 1,5 s de folga ao nascer. Valores no HiveLevelBootstrap ▸ Abelhas da Rainha.
    /// </summary>
    public class HiveWorkerBee : MonoBehaviour
    {
        private enum State { Patrol, Telegraph, Dash, Recover }

        private const float Aggro = 9f;

        /// <summary>Máximo de abelhas da Rainha vivas ao mesmo tempo (a Rainha pede 2 e, na fase 2, até 4).</summary>
        public static int MinionMax = 2;
        /// <summary>Dano de contato das abelhas da Rainha (abelha comum de fase = 15).</summary>
        public static int MinionDamage = 10;
        /// <summary>Velocidade do mergulho das abelhas da Rainha (abelha comum = 12).</summary>
        public static float MinionDashSpeed = 8f;
        /// <summary>1 = ataca todo compasso; 2 = ataca de 2 em 2 compassos (padrão); 3 = de 3 em 3.</summary>
        public static int MinionAttackEveryBars = 2;
        private const float MinionAggro = 6.5f;
        private const float MinionGrace = 1.5f;

        private HiveDamageable dmg;
        private Transform visual;
        private SpriteRenderer body;
        private Transform wingA, wingB;

        private Vector2 anchor;
        private Vector2 pos;
        private Vector2 lockPoint;
        private Vector2 dashDir = Vector2.right;
        private float range = 6f;
        private float dashLeft;
        private float flip = 1f;
        private float jitterSeed;
        private State state = State.Patrol;

        private bool isMinion;
        private bool aimFrozen;
        private int barCount;
        private float graceUntil;
        private float aggro = Aggro;
        private float dashSpeed = 12f;

        public void Init(float patrolRange, float hp)
        {
            // abelha chamada pela Rainha? (nasce como filha do HiveQueenArena)
            isMinion = transform.parent != null && transform.parent.GetComponent<HiveQueenArena>() != null;
            if (isMinion)
            {
                var alive = transform.parent.GetComponentsInChildren<HiveWorkerBee>();
                if (alive.Length > Mathf.Max(1, MinionMax))
                {
                    Destroy(gameObject);   // já há abelhas demais na arena
                    return;
                }
                aggro = MinionAggro;
                dashSpeed = Mathf.Max(3f, MinionDashSpeed);
                graceUntil = Time.time + MinionGrace;
            }

            range = Mathf.Max(0f, patrolRange);
            anchor = transform.position;
            pos = anchor;
            jitterSeed = Random.value * 10f;

            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.42f;

            dmg = gameObject.AddComponent<HiveDamageable>();
            dmg.Setup(hp > 0f ? hp : 20f);
            dmg.Damaged += OnHurt;
            dmg.Died += OnDied;

            var cd = gameObject.AddComponent<HiveContactDamage>();
            cd.damage = isMinion ? Mathf.Max(1, MinionDamage) : 15;
            cd.knockback = 7f;

            var v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            visual = v.transform;
            body = v.AddComponent<SpriteRenderer>();
            body.sprite = HiveSpriteFactory.Bee();
            body.sortingOrder = 10;

            wingA = MakeWing(0.12f, 0.2f, 11);
            wingB = MakeWing(0.0f, 0.22f, 9);
        }

        private Transform MakeWing(float x, float y, int order)
        {
            var w = new GameObject("Wing");
            w.transform.SetParent(visual, false);
            w.transform.localPosition = new Vector3(x, y, 0f);
            var sr = w.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Wing();
            sr.sortingOrder = order;
            w.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            return w.transform;
        }

        private void OnEnable() { HiveBeatDriver.OnBeat += HandleBeat; }
        private void OnDisable() { HiveBeatDriver.OnBeat -= HandleBeat; }

        private bool PlayerNear(out Vector2 playerPos)
        {
            playerPos = Vector2.zero;
            var p = HiveBridge.Player;
            if (p == null) return false;
            playerPos = p.transform.position;
            return Vector2.Distance(pos, playerPos) < aggro;
        }

        private void HandleBeat(int beat, int inBar)
        {
            if (dmg == null || dmg.IsDead) return;

            if (inBar == 0)
            {
                barCount++;
                if (state == State.Recover || state == State.Dash) state = State.Patrol;
                Vector2 pp;
                bool mayAttack = !isMinion ||
                                 (Time.time >= graceUntil && barCount % Mathf.Max(1, MinionAttackEveryBars) == 0);
                if (mayAttack && state == State.Patrol && PlayerNear(out pp))
                {
                    state = State.Telegraph;
                    aimFrozen = false;
                    lockPoint = pp;
                    HiveSfx.Play(HiveSfxId.Warn, 0.25f, 1.3f);
                }
            }
            else if (inBar == 1 && state == State.Telegraph && isMinion)
            {
                aimFrozen = true;   // a mira para de seguir você: 1 batida inteira para sair da linha
            }
            else if (inBar == 2 && state == State.Telegraph)
            {
                Vector2 d = lockPoint - pos;
                dashDir = d.sqrMagnitude < 0.01f ? Vector2.down : d.normalized;
                dashLeft = (float)HiveBeatDriver.BeatInterval * 0.9f;
                state = State.Dash;
                HiveSfx.Play(HiveSfxId.Shoot, 0.25f, 0.7f);
            }
        }

        private void Update()
        {
            if (dmg == null || dmg.IsDead) return;
            float dt = Time.deltaTime;
            double sb = HiveBeatDriver.Running ? System.Math.Max(0.0, HiveBeatDriver.SongBeat) : Time.timeAsDouble;

            Vector2 patrol = anchor + new Vector2(
                Mathf.Sin((float)(sb * Mathf.PI * 2.0 / 8.0) + jitterSeed) * range * 0.5f,
                Mathf.Sin((float)(sb * Mathf.PI) + jitterSeed) * 0.25f);

            var p = HiveBridge.Player;
            float faceTarget = flip;

            switch (state)
            {
                case State.Patrol:
                    pos = Vector2.MoveTowards(pos, patrol, 12f * dt);
                    break;
                case State.Recover:
                    pos = Vector2.MoveTowards(pos, patrol, 7f * dt);
                    if ((pos - patrol).sqrMagnitude < 0.04f) state = State.Patrol;
                    break;
                case State.Telegraph:
                    if (p != null && !aimFrozen) lockPoint = (Vector2)p.transform.position + Vector2.up * 0.3f;
                    break;
                case State.Dash:
                {
                    Vector2 step = dashDir * dashSpeed * dt;
                    if (!HivePhysics.CircleBlocked(pos, 0.3f, dashDir, step.magnitude + 0.05f)) pos += step;
                    else dashLeft = 0f;
                    dashLeft -= dt;
                    faceTarget = dashDir.x >= 0f ? 1f : -1f;
                    if (dashLeft <= 0f) state = State.Recover;
                    break;
                }
            }

            if (state != State.Dash && p != null)
                faceTarget = p.transform.position.x >= pos.x ? 1f : -1f;
            flip = faceTarget;

            Vector2 shown = pos;
            if (state == State.Telegraph)
                shown += new Vector2(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
            transform.position = shown;

            visual.localScale = new Vector3(flip, 1f, 1f);

            float blink = state == State.Telegraph ? (0.5f + 0.5f * Mathf.Sin(Time.time * 36f)) : 0f;
            // o flash de dano (HiveDamageable, LateUpdate) sobrepõe esta cor durante o hit
            body.color = state == State.Telegraph ? Color.Lerp(Color.white, HivePalette.Pink, blink) : Color.white;

            float flap = Mathf.Sin(Time.time * 70f) * 28f;
            wingA.localRotation = Quaternion.Euler(0f, 0f, 35f + flap);
            wingB.localRotation = Quaternion.Euler(0f, 0f, 55f - flap);
        }

        private void OnHurt(HiveDamageable d, float amount, Vector2 from)
        {
            Vector2 away = pos - from;
            if (away.sqrMagnitude < 0.01f) away = Vector2.up;
            pos += away.normalized * 0.4f;
            if (state == State.Telegraph || state == State.Dash) state = State.Recover;
        }

        private void OnDied(HiveDamageable d)
        {
            HiveSfx.Play(HiveSfxId.EnemyDie, 0.5f, 1.2f);
            HiveFx.Burst(pos, HivePalette.Gold, 14, 5f, 0.6f, 0.2f, 6f);
            HiveFx.Ring(pos, HivePalette.Honey, 1.8f, 0.35f);
            var b = HiveLevelBootstrap.Instance;
            if (b != null) b.OnEnemyKilled(pos, 0.2f, 1);
            Destroy(gameObject);
        }
    }
}