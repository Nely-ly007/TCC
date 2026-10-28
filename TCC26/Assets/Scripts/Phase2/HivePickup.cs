using UnityEngine;

namespace PopAdventure.Hive
{
    public enum HivePickupType { Vinyl, Heal, Fragment }

    /// <summary>
    /// Vinil (moeda), nota de cura (+15 HP) e fragmento do Disco Dourado (recompensa do boss).
    /// Flutua, pulsa no beat e é atraído pelo jogador quando perto.
    /// </summary>
    public class HivePickup : MonoBehaviour
    {
        /// <summary>Vinis de marcador contam no total da fase; drops de inimigos são bônus.</summary>
        public bool countsInTotal = true;

        /// <summary>Raio do ímã (unidades). Definidos pelo HiveLevelBootstrap (Vinyl Magnet / Note Magnet).</summary>
        public static float VinylMagnet = 0.9f;
        public static float NoteMagnet = 0.7f;

        private HivePickupType type;
        private int value = 1;
        private bool falls;
        private bool landed;
        private float fallVel;
        private float magnet;
        private float baseScale = 1f;
        private float bobSeed;
        private float born;
        private bool collected;
        private Transform visual;
        private SpriteRenderer sr;
        private SpriteRenderer glow;
        private Vector2 basePos;

        public static HivePickup Create(HivePickupType t, Vector2 pos, int val, bool fall, Transform parent)
        {
            var go = new GameObject(t + "Pickup");
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            var p = go.AddComponent<HivePickup>();
            p.Init(t, val, fall);
            return p;
        }

        private void Init(HivePickupType t, int val, bool fall)
        {
            type = t;
            value = val;
            falls = fall;
            bobSeed = Random.value * 10f;
            born = Time.time;
            basePos = transform.position;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;

            var gl = new GameObject("Glow");
            gl.transform.SetParent(transform, false);
            glow = gl.AddComponent<SpriteRenderer>();
            glow.sprite = HiveSpriteFactory.Soft();
            glow.sortingOrder = 8;

            var v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            visual = v.transform;
            sr = v.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 9;

            switch (t)
            {
                case HivePickupType.Vinyl:
                    sr.sprite = HiveSpriteFactory.Vinyl();
                    baseScale = 0.7f; col.radius = 0.4f; magnet = VinylMagnet;
                    glow.color = new Color(1f, 1f, 1f, 0.22f); glow.transform.localScale = Vector3.one * 1.3f;
                    break;
                case HivePickupType.Heal:
                    sr.sprite = HiveSpriteFactory.Note();
                    baseScale = 0.85f; col.radius = 0.45f; magnet = NoteMagnet;
                    glow.color = new Color(HivePalette.Pink.r, HivePalette.Pink.g, HivePalette.Pink.b, 0.35f);
                    glow.transform.localScale = Vector3.one * 1.8f;
                    break;
                default:
                    sr.sprite = HiveSpriteFactory.Fragment();
                    baseScale = 1.4f; col.radius = 0.8f; magnet = 0f;
                    glow.color = new Color(HivePalette.Gold.r, HivePalette.Gold.g, HivePalette.Gold.b, 0.5f);
                    glow.transform.localScale = Vector3.one * 3.2f;
                    break;
            }
            visual.localScale = Vector3.one * baseScale;
        }

        private void Update()
        {
            if (collected || visual == null) return;
            float dt = Time.deltaTime;

            // queda (notas de cura derrubadas pelo boss)
            if (falls && !landed)
            {
                fallVel += 16f * dt;
                RaycastHit2D h;
                float step = fallVel * dt;
                if (HivePhysics.Ray(transform.position, Vector2.down, step + 0.45f, out h))
                {
                    landed = true;
                    basePos = new Vector2(transform.position.x, h.point.y + 0.55f);
                    transform.position = basePos;
                }
                else
                {
                    transform.position += Vector3.down * step;
                    basePos = transform.position;
                }
            }

            // ímã
            var p = HiveBridge.Player;
            if (p != null && magnet > 0f && Time.time - born > 0.5f && (!falls || landed))
            {
                Vector2 to = (Vector2)p.transform.position - (Vector2)transform.position;
                float d = to.magnitude;
                if (d < magnet)
                {
                    float sp = Mathf.Lerp(2f, 9f, 1f - d / magnet);
                    transform.position += (Vector3)(to.normalized * sp * dt);
                    basePos = transform.position;
                }
            }

            // flutuar + pulsar
            float bob = Mathf.Sin(Time.time * 2.4f + bobSeed) * 0.1f;
            visual.localPosition = new Vector3(0f, bob, 0f);
            float pulse = 1f + 0.12f * HiveBeatDriver.Pulse;
            float sx = 1f;
            if (type == HivePickupType.Vinyl) sx = Mathf.Max(0.18f, Mathf.Abs(Mathf.Cos(Time.time * 3.2f + bobSeed)));
            visual.localScale = new Vector3(baseScale * pulse * sx, baseScale * pulse, 1f);

            if (type == HivePickupType.Fragment)
                visual.Rotate(0f, 0f, 40f * dt);
            glow.transform.localScale = Vector3.one * (type == HivePickupType.Fragment ? 3.2f : 1.5f) *
                                        (1f + 0.15f * HiveBeatDriver.Pulse);
        }

        private void OnTriggerEnter2D(Collider2D o)
        {
            if (collected || !HiveBridge.IsPlayer(o)) return;
            collected = true;

            Vector2 at = transform.position;
            var boot = HiveLevelBootstrap.Instance;
            switch (type)
            {
                case HivePickupType.Vinyl:
                    HiveBridge.AddVinyls(value);
                    if (boot != null) boot.OnVinylCollected(countsInTotal);
                    HiveSfx.Play(HiveSfxId.Vinyl, 0.55f, Random.Range(0.95f, 1.12f));
                    HiveFx.Burst(at, Color.white, 7, 3.2f, 0.35f, 0.13f);
                    break;
                case HivePickupType.Heal:
                    HiveBridge.HealPlayer(value);
                    HiveSfx.Play(HiveSfxId.Heal, 0.6f, 1f);
                    HiveFx.Burst(at, HivePalette.Pink, 12, 4f, 0.5f, 0.17f);
                    HiveFx.Ring(at, HivePalette.Pink, 1.6f, 0.4f);
                    HiveOverlay.Toast("+" + value + " HP", HivePalette.Pink, 1.0f);
                    break;
                default:
                    HiveBridge.CollectFragment(2);
                    HiveSfx.Play(HiveSfxId.Fragment, 0.8f, 1f);
                    HiveFx.Burst(at, HivePalette.Gold, 36, 8f, 1.0f, 0.26f, 1f);
                    HiveFx.Ring(at, HivePalette.Gold, 5f, 0.7f);
                    if (boot != null) boot.OnFragmentCollected();
                    break;
            }
            Destroy(gameObject);
        }
    }
}