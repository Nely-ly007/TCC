using System.Collections;
using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Rainha Abelha — boss da Fase 2. Todos os ataques saem das batidas/compassos (4/4, 108 BPM).
    ///
    /// FASE 1 (100% → 50%), ciclo de 4 compassos:
    ///   Volley   : leque de 3 ferrões nas batidas 2 e 4.
    ///   SlamPrep : alinha no jogador, pisca na batida 3, mergulha na batida 4.
    ///   Grounded : IMPACTO exatamente no tempo 1 (ondas rasteiras, pule!). Fica atordoada no chão
    ///              (dano x1.5), chama abelhas na batida 3 e sobe na batida 4.
    ///   Rain     : chuva de mel do teto, uma gota por batida (uma mira no jogador na batida 3).
    /// FASE 2 (≤ 50%, "ROYAL RAGE"), ciclo de 5 compassos: Volley+ (5 ferrões), SlamPrep, Grounded+ (onda dupla),
    ///   Rain+ (2 gotas/batida) e Dash (investida horizontal baixa — pule por cima).
    /// Cura: cai uma nota (+15 HP) aos 66% e outra aos 33% da vida da Rainha (máx. 2 por luta).
    /// </summary>
    public class HiveQueenBoss : MonoBehaviour
    {
        private enum Mode { Intro, Hover, Align, Dive, Grounded, Rise, DashPrep, Dash, Dead }
        private enum Pattern { Volley, SlamPrep, Grounded, Rain, Dash }

        private static readonly Pattern[] Cycle1 = { Pattern.Volley, Pattern.SlamPrep, Pattern.Grounded, Pattern.Rain };
        private static readonly Pattern[] Cycle2 = { Pattern.Volley, Pattern.SlamPrep, Pattern.Grounded, Pattern.Rain, Pattern.Dash };

        private const float BodyRadius = 1.45f;

        private HiveQueenArena arena;
        private HiveDamageable dmg;
        private HiveContactDamage contact;
        private CircleCollider2D hurt;
        private Transform visual;
        private SpriteRenderer body;
        private Transform wingA, wingB;
        private float spriteScale = 1f;   // multiplicador do sprite personalizado (HiveLevelBootstrap ▸ Boss Sprite Scale)

        private float minX, maxX, floorY, ceilY, centerX, hoverY, restY;
        private Vector2 pos, vel;
        private Mode mode = Mode.Intro;
        private Pattern pattern = Pattern.Volley;
        private bool armed, phase2, warn, dead;
        private int barCount = -1;
        private int heals;
        private float diveSpeed, dashDir = 1f, dashSpeed, flip = -1f;
        private float squashCur = 1f, squashTarget = 1f;
        private float dashTrail;
        private Vector2 dashStart;

        public void Init(HiveQueenArena a, float x0, float x1, float floor, float ceil, float hp)
        {
            arena = a;
            minX = x0; maxX = x1; floorY = floor; ceilY = ceil;
            centerX = (x0 + x1) * 0.5f;
            hoverY = floorY + Mathf.Min(5.2f, (ceilY - floorY) * 0.55f);
            restY = floorY + 1.55f;
            pos = new Vector2(centerX, ceilY + 3.5f);
            transform.position = pos;

            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            hurt = gameObject.AddComponent<CircleCollider2D>();
            hurt.isTrigger = true;
            hurt.radius = BodyRadius;

            dmg = gameObject.AddComponent<HiveDamageable>();
            dmg.Setup(hp > 0f ? hp : 500f);
            dmg.invulnerable = true;
            dmg.Damaged += OnHurt;
            dmg.Died += OnDied;

            contact = gameObject.AddComponent<HiveContactDamage>();
            contact.damage = 30;
            contact.knockback = 10f;
            contact.active = false;

            var v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            visual = v.transform;
            body = v.AddComponent<SpriteRenderer>();
            body.sprite = HiveSpriteFactory.Queen();
            body.sortingOrder = 10;

            wingA = MakeWing(-0.35f, 0.95f, 9, 2.1f);
            wingB = MakeWing(0.15f, 1.05f, 11, 2.3f);

            // sprite próprio do boss (HiveLevelBootstrap ▸ Boss Sprite): troca só o visual, o comportamento é o mesmo
            var boot = HiveLevelBootstrap.Instance;
            if (boot != null && boot.bossSprite != null)
            {
                body.sprite = boot.bossSprite;
                spriteScale = Mathf.Max(0.05f, boot.bossSpriteScale);
                foreach (var wr in visual.GetComponentsInChildren<SpriteRenderer>())
                    if (wr != body) wr.enabled = false;
            }
        }

        private Transform MakeWing(float x, float y, int order, float scale)
        {
            var w = new GameObject("Wing");
            w.transform.SetParent(visual, false);
            w.transform.localPosition = new Vector3(x, y, 0f);
            w.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = w.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Wing();
            sr.sortingOrder = order;
            return w.transform;
        }

        private void OnEnable() { HiveBeatDriver.OnBeat += HandleBeat; }
        private void OnDisable() { HiveBeatDriver.OnBeat -= HandleBeat; }

        private static Vector2 PlayerPos(Vector2 fallback)
        {
            var p = HiveBridge.Player;
            return p != null ? (Vector2)p.transform.position : fallback;
        }

        // ============================================================ Cronograma (por batida)

        private void HandleBeat(int beat, int inBar)
        {
            if (dead || dmg == null || mode == Mode.Intro) return;

            if (!armed)
            {
                if (inBar != 0) return;
                armed = true;
                barCount = -1;
            }

            if (inBar == 0)
            {
                barCount++;
                Pattern[] cycle = phase2 ? Cycle2 : Cycle1;
                pattern = cycle[barCount % cycle.Length];
            }

            switch (pattern)
            {
                case Pattern.Volley:
                    if (inBar == 0) SetMode(Mode.Hover);
                    if (inBar == 1 || inBar == 3)
                        FireFan(phase2 ? 5 : 3, phase2 ? 14f : 18f, phase2 ? 10f : 8.5f);
                    if (phase2 && inBar == 2) FireFan(1, 0f, 12f);
                    break;

                case Pattern.SlamPrep:
                    if (inBar == 0) { SetMode(Mode.Align); warn = false; }
                    else if (inBar == 2) { warn = true; HiveSfx.Play(HiveSfxId.Warn, 0.5f, 0.8f); }
                    else if (inBar == 3) StartDive();
                    break;

                case Pattern.Grounded:
                    if (inBar == 0) Impact();
                    else if (inBar == 2) CallBees();
                    else if (inBar == 3) { SetMode(Mode.Rise); dmg.damageMultiplier = 1f; }
                    break;

                case Pattern.Rain:
                    if (inBar == 0) SetMode(Mode.Hover);
                    RainDrops(inBar);
                    break;

                case Pattern.Dash:
                    if (inBar == 0) StartDashPrep();
                    else if (inBar == 1) { warn = true; HiveSfx.Play(HiveSfxId.Warn, 0.5f, 0.7f); }
                    else if (inBar == 2) StartDash();
                    break;
            }
        }

        private void SetMode(Mode m)
        {
            mode = m;
            squashTarget = (m == Mode.DashPrep || m == Mode.Dash) ? 0.72f : 1f;
            if (m != Mode.Grounded) dmg.damageMultiplier = 1f;
        }

        // ============================================================ Ataques

        private void FireFan(int count, float spreadDeg, float speed)
        {
            Vector2 origin = pos + new Vector2(flip * 1.2f, -0.2f);
            Vector2 target = PlayerPos(pos + Vector2.down) + Vector2.up * 0.4f;
            float baseAng = Mathf.Atan2(target.y - origin.y, target.x - origin.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float a = baseAng + (i - (count - 1) * 0.5f) * spreadDeg;
                Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                HiveStinger.Create(origin, dir, speed, 15);
            }
            HiveFx.Burst(origin, HivePalette.Pink, 5, 3f, 0.25f, 0.14f);
        }

        private void StartDive()
        {
            warn = false;
            SetMode(Mode.Dive);
            diveSpeed = Mathf.Max(2f, (pos.y - restY) / (float)HiveBeatDriver.BeatInterval);
        }

        private void Impact()
        {
            if (mode != Mode.Dive)
            {
                SetMode(Mode.Hover);
                return;
            }
            pos.y = restY;
            SetMode(Mode.Grounded);
            dmg.damageMultiplier = 1.5f;
            warn = false;

            Vector2 foot = new Vector2(pos.x, floorY);
            HiveBridge.ShakeCamera(0.35f, 0.3f);
            HiveSfx.Play(HiveSfxId.Slam, 1f, 1f);
            HiveFx.Ring(foot, HivePalette.Pink, 4.5f, 0.5f);
            HiveFx.Burst(foot, HivePalette.Gold, 18, 6f, 0.6f, 0.22f, 8f);

            float speed = 6.5f;
            HiveShockwave.Create(foot, 1f, speed, minX - 1f, maxX + 1f, 30);
            HiveShockwave.Create(foot, -1f, speed, minX - 1f, maxX + 1f, 30);
            if (phase2) StartCoroutine(SecondWave(foot));
        }

        private IEnumerator SecondWave(Vector2 foot)
        {
            yield return new WaitForSeconds((float)HiveBeatDriver.BeatInterval * 0.5f);
            if (dead) yield break;
            HiveSfx.Play(HiveSfxId.Slam, 0.6f, 1.2f);
            HiveShockwave.Create(foot, 1f, 8.5f, minX - 1f, maxX + 1f, 30);
            HiveShockwave.Create(foot, -1f, 8.5f, minX - 1f, maxX + 1f, 30);
        }

        private void CallBees()
        {
            int alive = arena.AliveBees();
            int max = phase2 ? 4 : 2;
            int n = Mathf.Min(2, max - alive);
            for (int i = 0; i < n; i++)
            {
                float x = i % 2 == 0 ? minX + 0.5f : maxX - 0.5f;
                Vector2 at = new Vector2(x, floorY + 5f);
                arena.SpawnBee(at);
                HiveFx.Ring(at, HivePalette.Gold, 1.8f, 0.4f);
            }
            if (n > 0) HiveSfx.Play(HiveSfxId.Appear, 0.5f, 0.7f);
        }

        private void RainDrops(int inBar)
        {
            int n = phase2 ? 2 : 1;
            float speed = phase2 ? 9.5f : 8f;
            for (int i = 0; i < n; i++)
            {
                float x = Random.Range(minX + 1f, maxX - 1f);
                if (inBar == 2 && i == 0) x = Mathf.Clamp(PlayerPos(pos).x, minX + 1f, maxX - 1f);
                Vector2 at = new Vector2(x, ceilY - 0.3f);
                HiveHoneyDrop.Create(at, speed, 15);
                HiveFx.Burst(at, HivePalette.Honey, 3, 2f, 0.25f, 0.12f, 0f, HiveSpriteFactory.Dot());
            }
        }

        private void StartDashPrep()
        {
            warn = false;
            SetMode(Mode.DashPrep);
            bool fromRight = PlayerPos(pos).x < centerX;
            dashStart = new Vector2(fromRight ? maxX : minX, floorY + 1.1f);
            dashDir = fromRight ? -1f : 1f;
        }

        private void StartDash()
        {
            warn = false;
            SetMode(Mode.Dash);
            dashSpeed = (maxX - minX) / ((float)HiveBeatDriver.BeatInterval * 1.7f);
            HiveBridge.ShakeCamera(0.2f, 0.25f);
            HiveSfx.Play(HiveSfxId.BossRoar, 0.7f, 1.3f);
        }

        // ============================================================ Eventos de vida

        private void OnHurt(HiveDamageable d, float amount, Vector2 from)
        {
            float n = dmg.Normalized;
            arena.SetBossBar(n);

            if (heals == 0 && n <= 0.66f) { heals = 1; arena.DropHeal(); }
            else if (heals == 1 && n <= 0.33f) { heals = 2; arena.DropHeal(); }

            if (!phase2 && !dead && n <= 0.5f && n > 0f) EnterPhase2();
        }

        private void EnterPhase2()
        {
            phase2 = true;
            armed = false;
            barCount = -1;
            warn = false;
            SetMode(Mode.Hover);
            StartCoroutine(RageRoutine());
        }

        private IEnumerator RageRoutine()
        {
            dmg.invulnerable = true;
            contact.active = false;
            HiveHostile.ClearAll();
            HiveSfx.Play(HiveSfxId.BossRoar, 1f, 0.9f);
            HiveBridge.ShakeCamera(0.4f, 0.8f);
            HiveOverlay.Banner("ROYAL RAGE", "A Rainha enfureceu!", 1.6f);
            HiveOverlay.Flash(HivePalette.Gold, 0.5f, 0.6f);
            HiveFx.Ring(pos, HivePalette.Gold, 7f, 0.8f);
            yield return new WaitForSeconds(1.2f);
            if (dead) yield break;
            arena.SpawnBee(new Vector2(minX + 0.5f, floorY + 5f));
            arena.SpawnBee(new Vector2(maxX - 0.5f, floorY + 5f));
            dmg.invulnerable = false;
            contact.active = true;
        }

        private void OnDied(HiveDamageable d)
        {
            if (dead) return;
            dead = true;
            mode = Mode.Dead;
            contact.active = false;
            warn = false;
            HiveHostile.ClearAll();
            arena.ClearMinions();
            arena.OnBossDefeated();
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            HiveSfx.Play(HiveSfxId.BossRoar, 1f, 0.7f);
            float t = 0f, next = 0f;
            while (t < 2.4f)
            {
                t += Time.deltaTime;
                next -= Time.deltaTime;
                if (next <= 0f)
                {
                    next = 0.13f;
                    Vector2 p = pos + Random.insideUnitCircle * 1.8f;
                    HiveFx.Burst(p, Random.value > 0.5f ? HivePalette.Gold : HivePalette.Pink, 10, 5f, 0.6f, 0.22f, 4f);
                    HiveFx.Ring(p, HivePalette.Honey, 1.8f, 0.35f);
                    HiveSfx.Play(HiveSfxId.EnemyDie, 0.4f, Random.Range(0.7f, 1.3f));
                    HiveBridge.ShakeCamera(0.2f, 0.15f);
                }
                body.color = Color.Lerp(Color.white, HivePalette.Pink, Mathf.PingPong(t * 8f, 1f));
                yield return null;
            }
            HiveFx.Burst(pos, HivePalette.Gold, 60, 11f, 1.1f, 0.3f, 2f);
            HiveFx.Ring(pos, Color.white, 9f, 0.9f);
            HiveSfx.Play(HiveSfxId.Slam, 1f, 0.8f);
            HiveOverlay.Flash(Color.white, 0.8f, 0.7f);
            arena.SpawnFragment(new Vector2(centerX, Mathf.Max(pos.y, floorY + 4f)));
            Destroy(gameObject);
        }

        // ============================================================ Movimento / visual

        private void Update()
        {
            if (dmg == null) return;
            float dt = Time.deltaTime;
            double sb = HiveBeatDriver.Running ? System.Math.Max(0.0, HiveBeatDriver.SongBeat) : Time.timeAsDouble;
            float halfW = (maxX - minX) * 0.5f;
            Vector2 pp = PlayerPos(pos);

            switch (mode)
            {
                case Mode.Intro:
                {
                    pos = Vector2.MoveTowards(pos, new Vector2(centerX, hoverY), 5f * dt);
                    if ((pos - new Vector2(centerX, hoverY)).sqrMagnitude < 0.05f)
                    {
                        mode = Mode.Hover;
                        dmg.invulnerable = false;
                        contact.active = true;
                        HiveFx.Ring(pos, HivePalette.Gold, 6f, 0.6f);
                    }
                    break;
                }
                case Mode.Hover:
                {
                    float tx = centerX + Mathf.Sin((float)(sb * Mathf.PI / 4.0)) * halfW * 0.55f;
                    float ty = hoverY + Mathf.Sin((float)(sb * Mathf.PI)) * 0.3f;
                    pos = Vector2.SmoothDamp(pos, new Vector2(tx, ty), ref vel, 0.28f);
                    break;
                }
                case Mode.Align:
                {
                    float tx = Mathf.Clamp(pp.x, minX, maxX);
                    pos = Vector2.SmoothDamp(pos, new Vector2(tx, hoverY + 0.6f), ref vel, 0.16f);
                    break;
                }
                case Mode.Dive:
                    pos.y = Mathf.Max(restY, pos.y - diveSpeed * dt);
                    break;
                case Mode.Grounded:
                    pos.y = restY;
                    break;
                case Mode.Rise:
                    pos = Vector2.SmoothDamp(pos, new Vector2(pos.x, hoverY), ref vel, 0.3f);
                    break;
                case Mode.DashPrep:
                    pos = Vector2.SmoothDamp(pos, dashStart, ref vel, 0.2f);
                    break;
                case Mode.Dash:
                {
                    pos.x = Mathf.Clamp(pos.x + dashDir * dashSpeed * dt, minX, maxX);
                    dashTrail -= dt;
                    if (dashTrail <= 0f)
                    {
                        dashTrail = 0.04f;
                        HiveFx.Burst(pos, HivePalette.Gold, 2, 1.5f, 0.35f, 0.3f);
                    }
                    break;
                }
            }

            transform.position = pos;

            // orientação
            if (mode == Mode.Dash || mode == Mode.DashPrep) flip = dashDir;
            else if (mode != Mode.Dead) flip = pp.x >= pos.x ? 1f : -1f;

            squashCur = Mathf.Lerp(squashCur, squashTarget, 10f * dt);
            float pulse = 1f + 0.04f * HiveBeatDriver.Pulse;
            visual.localScale = new Vector3(flip * squashCur * pulse * spriteScale, squashCur * pulse * spriteScale, 1f);
            hurt.radius = BodyRadius * squashCur;

            // cor (o flash de dano em HiveDamageable.LateUpdate sobrepõe durante o hit)
            if (mode != Mode.Dead)
            {
                Color c = phase2 ? new Color(1f, 0.88f, 0.82f, 1f) : Color.white;
                if (mode == Mode.Grounded)
                    c = Color.Lerp(c, new Color(0.7f, 0.85f, 1f, 1f), 0.6f + 0.4f * Mathf.Sin(Time.time * 10f));
                if (warn)
                    c = Color.Lerp(c, HivePalette.Pink, 0.5f + 0.5f * Mathf.Sin(Time.time * 34f));
                if (dmg.invulnerable)
                    c.a = 0.55f + 0.25f * Mathf.Sin(Time.time * 20f);
                body.color = c;
            }

            float flap = Mathf.Sin(Time.time * (mode == Mode.Grounded ? 20f : 55f)) * 20f;
            wingA.localRotation = Quaternion.Euler(0f, 0f, 30f + flap);
            wingB.localRotation = Quaternion.Euler(0f, 0f, 55f - flap);
        }
    }
}