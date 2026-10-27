using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Vespa-guarda terrestre (60 HP, 30 de dano — inimigo forte). Patrulha, e quando o jogador entra no alcance:
    /// batida 1 = para e telegrafa (vermelho), batida 3 = investe. Para em parede/beirada e fica exposta.
    /// </summary>
    public class HiveHornetGuard : MonoBehaviour
    {
        private enum State { Patrol, Telegraph, Charge, Recover }

        private const float Aggro = 8f;

        private Rigidbody2D rb;
        private HiveDamageable dmg;
        private Transform visual;
        private SpriteRenderer body;
        private Transform wingA;

        private float anchorX;
        private float range = 6f;
        private float dir = 1f;
        private float knockT;
        private State state = State.Patrol;

        public void Init(float patrolRange, float hp)
        {
            range = Mathf.Max(1f, patrolRange);
            anchorX = transform.position.x;

            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var cap = gameObject.AddComponent<CapsuleCollider2D>();
            cap.size = new Vector2(1.2f, 0.95f);
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.sharedMaterial = new PhysicsMaterial2D("HiveSlip") { friction = 0f, bounciness = 0f };

            dmg = gameObject.AddComponent<HiveDamageable>();
            dmg.Setup(hp > 0f ? hp : 60f);
            dmg.Damaged += OnHurt;
            dmg.Died += OnDied;

            var cd = gameObject.AddComponent<HiveContactDamage>();
            cd.damage = 30;
            cd.knockback = 9f;

            var v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            visual = v.transform;
            body = v.AddComponent<SpriteRenderer>();
            body.sprite = HiveSpriteFactory.Hornet();
            body.sortingOrder = 10;

            var w = new GameObject("Wing");
            w.transform.SetParent(visual, false);
            w.transform.localPosition = new Vector3(0.05f, 0.3f, 0f);
            var wsr = w.AddComponent<SpriteRenderer>();
            wsr.sprite = HiveSpriteFactory.Wing();
            wsr.sortingOrder = 11;
            wingA = w.transform;
        }

        private void OnEnable() { HiveBeatDriver.OnBeat += HandleBeat; }
        private void OnDisable() { HiveBeatDriver.OnBeat -= HandleBeat; }

        private bool PlayerInRange(out Vector2 pp)
        {
            pp = Vector2.zero;
            var p = HiveBridge.Player;
            if (p == null) return false;
            pp = p.transform.position;
            Vector2 me = transform.position;
            return Mathf.Abs(pp.x - me.x) < Aggro && Mathf.Abs(pp.y - me.y) < 2.2f;
        }

        private void HandleBeat(int beat, int inBar)
        {
            if (dmg == null || dmg.IsDead) return;
            Vector2 pp;

            if (inBar == 0)
            {
                if (state == State.Charge || state == State.Recover) state = State.Patrol;
                if (state == State.Patrol && PlayerInRange(out pp))
                {
                    state = State.Telegraph;
                    dir = pp.x >= transform.position.x ? 1f : -1f;
                    HiveSfx.Play(HiveSfxId.Warn, 0.3f, 0.9f);
                }
            }
            else if (inBar == 2 && state == State.Telegraph)
            {
                if (PlayerInRange(out pp)) dir = pp.x >= transform.position.x ? 1f : -1f;
                state = State.Charge;
                HiveSfx.Play(HiveSfxId.Shoot, 0.3f, 0.6f);
            }
        }

        private bool WallAhead()
        {
            RaycastHit2D h;
            Vector2 o = (Vector2)transform.position + Vector2.up * 0.05f;
            return HivePhysics.Ray(o, new Vector2(dir, 0f), 0.85f, out h);
        }

        private bool LedgeAhead()
        {
            RaycastHit2D h;
            Vector2 o = (Vector2)transform.position + new Vector2(dir * 0.8f, 0f);
            return !HivePhysics.Ray(o, Vector2.down, 1.4f, out h);
        }

        private void FixedUpdate()
        {
            if (dmg == null || dmg.IsDead || rb == null) return;
            if (knockT > 0f) { knockT -= Time.fixedDeltaTime; return; }

            float vx = 0f;
            switch (state)
            {
                case State.Patrol:
                    vx = dir * 2.2f;
                    if (WallAhead() || LedgeAhead()) dir = -dir;
                    else if (transform.position.x - anchorX > range * 0.5f && dir > 0f) dir = -1f;
                    else if (transform.position.x - anchorX < -range * 0.5f && dir < 0f) dir = 1f;
                    break;
                case State.Charge:
                    vx = dir * 8.5f;
                    if (WallAhead() || LedgeAhead())
                    {
                        state = State.Recover;
                        vx = 0f;
                        HiveFx.Burst(transform.position, HivePalette.Amber, 8, 3f, 0.4f, 0.16f, 8f);
                    }
                    break;
                default:
                    vx = 0f;
                    break;
            }

            Vector2 v = HiveBridge.GetVelocity(rb);
            v.x = vx;
            HiveBridge.SetVelocity(rb, v);
        }

        private void Update()
        {
            if (dmg == null || dmg.IsDead) return;
            visual.localScale = new Vector3(dir >= 0f ? 1f : -1f, 1f, 1f);

            Vector3 lp = Vector3.zero;
            if (state == State.Telegraph)
            {
                lp = new Vector3(Random.Range(-0.04f, 0.04f), 0f, 0f);
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 34f);
                body.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.3f, 1f), blink);
            }
            else if (state == State.Recover)
            {
                body.color = new Color(0.75f, 0.75f, 1f, 1f);
            }
            else
            {
                body.color = Color.white;
            }
            visual.localPosition = lp;

            wingA.localRotation = Quaternion.Euler(0f, 0f, 40f + Mathf.Sin(Time.time * 60f) * 22f);
        }

        private void OnHurt(HiveDamageable d, float amount, Vector2 from)
        {
            if (rb == null) return;
            float side = transform.position.x >= from.x ? 1f : -1f;
            knockT = 0.15f;
            HiveBridge.SetVelocity(rb, new Vector2(side * 4f, 3f));
            if (state == State.Telegraph) state = State.Recover;
        }

        private void OnDied(HiveDamageable d)
        {
            HiveSfx.Play(HiveSfxId.EnemyDie, 0.6f, 0.8f);
            HiveFx.Burst(transform.position, HivePalette.Honey, 20, 6f, 0.7f, 0.24f, 6f);
            HiveFx.Ring(transform.position, HivePalette.Danger, 2.4f, 0.4f);
            var b = HiveLevelBootstrap.Instance;
            if (b != null) b.OnEnemyKilled(transform.position, 0.5f, 3);
            Destroy(gameObject);
        }
    }
}
