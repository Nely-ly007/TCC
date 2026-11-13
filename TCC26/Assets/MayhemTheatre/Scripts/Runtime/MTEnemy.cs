using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.MayhemTheatre
{
    public enum MTEnemyKind { Chaser, Patroller, Hoverer }

    /// <summary>
    /// Inimigo da Fase 4 (marionetes que perseguem e inimigos "reprise" das fases 1-3).
    /// Chaser = persegue e dá um bote no ritmo (tempo 1 e 3); Patroller = anda de um lado a outro; Hoverer = flutua e persegue devagar.
    /// Contato causa dano (15 básico / 30 forte). Leva dano de qualquer objeto com MTDamageSource (ou nome "Projectile").
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MTEnemy : MTBeatListener, IMTDamageable
    {
        [Header("Tipo")]
        public MTEnemyKind kind = MTEnemyKind.Chaser;
        public bool spriteFacesRight = true;

        [Header("Atributos")]
        public float maxHealth = 20f;
        public float contactDamage = 15f;
        public float moveSpeed = 2.5f;
        public float aggroRange = 10f;
        public bool avoidLedges = true;

        [Header("Bote no ritmo (Chaser)")]
        public bool lungeOnBeat = true;
        public float attackRange = 2.6f;
        public float lungeSpeed = 7f;
        public float lungeTime = 0.35f;
        public float lungeCooldown = 1.6f;

        [Header("Hoverer")]
        public float hoverAmplitude = 0.7f;
        public float hoverBeatsPerCycle = 4f;

        [Header("Drop de nota (cura)")]
        [Range(0f, 1f)] public float noteDropChance = 0.3f;
        public GameObject notePrefab;
        public Sprite noteSprite;

        public UnityEvent onDied = new UnityEvent();

        public bool IsDead { get; private set; }
        public float Health { get; private set; }

        Rigidbody2D rb;
        Collider2D solid;
        MTContactDamage contact;
        SpriteRenderer[] renderers;
        Color[] baseColors;
        Transform player;
        float facing = 1f;
        float homeY;
        float knockUntil;
        float flashUntil;
        float nextLunge;
        float lungeEnd;
        float lungeDir;
        bool lunging;
        float nextFindPlayer;
        int lastHitId;
        float lastHitTime = -10f;

        static readonly RaycastHit2D[] hitBuf = new RaycastHit2D[8];

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.gravityScale = kind == MTEnemyKind.Hoverer ? 0f : 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;

            Health = maxHealth;
            homeY = transform.position.y;
            EnsureColliders();
        }

        protected override void Start()
        {
            base.Start();
            FindPlayer();
            IgnorePlayerBody();
        }

        void FindPlayer()
        {
            player = MTUtil.FindPlayer();
            nextFindPlayer = Time.time + 1f;
        }

        void EnsureColliders()
        {
            bool found;
            Bounds b = MTUtil.RendererBounds(gameObject, out found);
            Vector3 ls = transform.lossyScale;
            float sx = Mathf.Max(0.0001f, Mathf.Abs(ls.x));
            float sy = Mathf.Max(0.0001f, Mathf.Abs(ls.y));
            Vector2 localCenter = transform.InverseTransformPoint(b.center);

            Collider2D[] existing = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < existing.Length; i++)
            {
                if (!existing[i].isTrigger) { solid = existing[i]; break; }
            }

            if (solid == null)
            {
                PhysicsMaterial2D mat = new PhysicsMaterial2D("MT_NoFriction");
                mat.friction = 0f;
                mat.bounciness = 0f;

                CapsuleCollider2D cap = gameObject.AddComponent<CapsuleCollider2D>();
                cap.direction = CapsuleDirection2D.Vertical;
                float w = Mathf.Max(0.3f, b.size.x / sx * 0.45f);
                float h = Mathf.Max(0.6f, b.size.y / sy * 0.8f);
                cap.size = new Vector2(w, h);
                cap.offset = new Vector2(localCenter.x, localCenter.y - (b.size.y / sy) * 0.05f);
                cap.sharedMaterial = mat;
                solid = cap;
            }

            GameObject hb = new GameObject("MT_Hurtbox");
            hb.transform.SetParent(transform, false);
            BoxCollider2D box = hb.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(Mathf.Max(0.4f, b.size.x / sx * 0.55f), Mathf.Max(0.6f, b.size.y / sy * 0.85f));
            box.offset = localCenter;
            contact = hb.AddComponent<MTContactDamage>();
            contact.damage = contactDamage;
        }

        void IgnorePlayerBody()
        {
            // O Player não deve ser empurrado/bloqueado pelo corpo do inimigo: o dano vem do Hurtbox (trigger).
            if (player == null || solid == null) return;
            Collider2D[] pc = player.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < pc.Length; i++)
                if (!pc[i].isTrigger) Physics2D.IgnoreCollision(solid, pc[i], true);
        }

        protected override void OnBeat(int beatAbs, int beatInBar, int bar)
        {
            if (IsDead || kind != MTEnemyKind.Chaser || !lungeOnBeat || lunging) return;
            if (player == null || Time.time < nextLunge) return;
            if (beatInBar % 2 != 0) return; // só nos tempos 1 e 3

            float dx = player.position.x - transform.position.x;
            float dy = player.position.y - transform.position.y;
            if (Mathf.Abs(dx) < attackRange && Mathf.Abs(dy) < 2.2f)
            {
                lunging = true;
                lungeEnd = Time.time + lungeTime;
                lungeDir = dx >= 0f ? 1f : -1f;
                facing = lungeDir;
                nextLunge = Time.time + lungeCooldown;
            }
        }

        void FixedUpdate()
        {
            if (IsDead) return;
            if (player == null && Time.time > nextFindPlayer) { FindPlayer(); IgnorePlayerBody(); }
            if (Time.time < knockUntil) return;

            Vector2 v = MTUtil.GetVelocity(rb);
            float dx = player != null ? player.position.x - transform.position.x : 0f;
            float dist = player != null ? Vector2.Distance(player.position, transform.position) : 999f;
            float dirToPlayer = dx >= 0f ? 1f : -1f;

            switch (kind)
            {
                case MTEnemyKind.Chaser:
                    if (lunging)
                    {
                        v.x = lungeDir * lungeSpeed;
                        if (Time.time > lungeEnd) lunging = false;
                    }
                    else if (player != null && dist < aggroRange)
                    {
                        facing = dirToPlayer;
                        v.x = CanAdvance(facing) ? facing * moveSpeed : 0f;
                    }
                    else v.x = 0f;
                    break;

                case MTEnemyKind.Patroller:
                    if (!CanAdvance(facing)) facing = -facing;
                    v.x = facing * moveSpeed;
                    break;

                case MTEnemyKind.Hoverer:
                    {
                        float cycleSeconds = (float)(60.0 / 64.0) * Mathf.Max(1f, hoverBeatsPerCycle);
                        MTBeatClock c = MTBeatClock.Instance;
                        if (c != null && c.Running) cycleSeconds = (float)c.SecondsPerBeat * Mathf.Max(1f, hoverBeatsPerCycle);
                        float targetY = homeY + Mathf.Sin(Time.time * Mathf.PI * 2f / cycleSeconds) * hoverAmplitude;
                        v.y = (targetY - transform.position.y) * 6f;
                        if (player != null && dist < aggroRange)
                        {
                            facing = dirToPlayer;
                            v.x = facing * moveSpeed * 0.6f;
                        }
                        else v.x = 0f;
                        break;
                    }
            }

            MTUtil.SetVelocity(rb, v);
        }

        void Update()
        {
            if (IsDead) return;

            // vira o sprite
            Vector3 s = transform.localScale;
            float mag = Mathf.Abs(s.x);
            float sign = (facing > 0f) == spriteFacesRight ? 1f : -1f;
            if (!Mathf.Approximately(s.x, mag * sign)) { s.x = mag * sign; transform.localScale = s; }

            // flash de dano
            bool flashing = Time.time < flashUntil;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = flashing ? Color.Lerp(baseColors[i], Color.white, 0.85f) : baseColors[i];
                c.a = baseColors[i].a;
                renderers[i].color = c;
            }
        }

        // ---------- terreno ----------
        bool CanAdvance(float dir)
        {
            if (solid == null) return true;
            Bounds b = solid.bounds;
            float frontX = b.center.x + dir * (b.extents.x + 0.1f);

            if (Probe(new Vector2(b.center.x, b.min.y + 0.35f), new Vector2(dir, 0f), b.extents.x + 0.25f)) return false;

            if (avoidLedges && kind != MTEnemyKind.Hoverer)
            {
                bool grounded = Probe(new Vector2(b.center.x, b.min.y + 0.1f), Vector2.down, 0.35f);
                if (grounded && !Probe(new Vector2(frontX, b.min.y + 0.1f), Vector2.down, 0.7f)) return false;
            }
            return true;
        }

        bool Probe(Vector2 origin, Vector2 direction, float distance)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            int n = Physics2D.Raycast(origin, direction, filter, hitBuf, distance);
            for (int i = 0; i < n; i++)
            {
                Collider2D c = hitBuf[i].collider;
                if (c == null) continue;
                if (c.transform.IsChildOf(transform)) continue;
                if (c.GetComponentInParent<MTPlayerHook>() != null) continue;
                if (c.GetComponentInParent<MTEnemy>() != null) continue;
                return true;
            }
            return false;
        }

        // ---------- dano ----------
        void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
        void OnCollisionEnter2D(Collision2D c) { TryHit(c.collider); }

        void TryHit(Collider2D other)
        {
            if (IsDead || other == null) return;
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
            if (IsDead) return;
            Health -= amount;
            flashUntil = Time.time + 0.1f;

            float dir = transform.position.x >= hitPoint.x ? 1f : -1f;
            knockUntil = Time.time + 0.12f;
            lunging = false;
            Vector2 v = MTUtil.GetVelocity(rb);
            v.x = dir * 4f;
            if (kind != MTEnemyKind.Hoverer) v.y = Mathf.Max(v.y, 2f);
            MTUtil.SetVelocity(rb, v);

            if (Health <= 0f) Die();
        }

        void Die()
        {
            IsDead = true;
            if (contact != null) contact.active = false;
            Collider2D[] cs = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < cs.Length; i++) cs[i].enabled = false;
            rb.simulated = false;

            if (Random.value < noteDropChance)
                MTPickupFactory.Create(MTPickup.PickupKind.Note, transform.position + Vector3.up * 0.6f, notePrefab, noteSprite, new Color(1f, 0.85f, 0.2f), 0.5f);

            onDied.Invoke();
            Destroy(gameObject, 0.15f);
        }

        // ---------- fábrica de placeholder ----------
        public static MTEnemy Create(Vector3 position, MTEnemyKind kind, Sprite sprite, Color tint, float spriteHeight,
            float health, float damage, float speed, float aggro, Sprite noteSprite, GameObject notePrefab)
        {
            GameObject go = new GameObject("MT_Enemy_" + kind);
            go.SetActive(false);
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Sprite s = sprite != null ? sprite : MTUtil.DiscSprite;
            sr.sprite = s;
            sr.color = tint;
            sr.sortingOrder = 3;
            float h = Mathf.Max(0.01f, s.bounds.size.y);
            float k = spriteHeight / h;
            go.transform.localScale = new Vector3(k, k, 1f);

            go.AddComponent<Rigidbody2D>();
            MTEnemy e = go.AddComponent<MTEnemy>();
            e.kind = kind;
            e.maxHealth = health;
            e.contactDamage = damage;
            e.moveSpeed = speed;
            e.aggroRange = aggro;
            e.noteSprite = noteSprite;
            e.notePrefab = notePrefab;
            go.SetActive(true);
            return e;
        }
    }
}
