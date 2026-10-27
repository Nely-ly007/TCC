using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Treme quando o jogador pisa, cai após N batidas e volta depois de M compassos.
    /// Contagem em batidas (não em segundos) para ficar no ritmo da música.
    /// </summary>
    public class HiveCrumblePlatform : MonoBehaviour
    {
        private enum State { Idle, Shaking, Fallen }

        private BoxCollider2D col;
        private SpriteRenderer sr;
        private Transform visual;
        private State state = State.Idle;
        private int shakeBeats = 2;
        private int respawnBars = 2;
        private int beatsLeft;
        private int barsLeft;
        private float stateTime;
        private float pop;
        private Vector2 size;

        public void Init(Vector2 platformSize, int beatsBeforeFall, int barsToRespawn)
        {
            size = platformSize;
            shakeBeats = Mathf.Max(1, beatsBeforeFall);
            respawnBars = Mathf.Max(1, barsToRespawn);

            col = gameObject.AddComponent<BoxCollider2D>();
            col.size = size;
            HivePhysics.MakeOneWay(gameObject, col);

            var v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            visual = v.transform;
            sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSpriteFactory.Block(size.x, size.y, HiveBlockStyle.Ground);
            sr.color = new Color(1f, 0.85f, 0.6f, 1f);
            sr.sortingOrder = 2;
        }

        private void OnEnable()
        {
            HiveBeatDriver.OnBeat += HandleBeat;
            HiveBeatDriver.OnBar += HandleBar;
        }

        private void OnDisable()
        {
            HiveBeatDriver.OnBeat -= HandleBeat;
            HiveBeatDriver.OnBar -= HandleBar;
        }

        private void OnCollisionEnter2D(Collision2D c) { TryStart(c); }
        private void OnCollisionStay2D(Collision2D c) { TryStart(c); }

        private void TryStart(Collision2D c)
        {
            if (state != State.Idle || col == null) return;
            if (!HiveBridge.IsPlayer(c.collider)) return;
            if (c.collider.bounds.min.y < col.bounds.max.y - 0.25f) return;
            state = State.Shaking;
            stateTime = 0f;
            beatsLeft = shakeBeats;
            HiveSfx.Play(HiveSfxId.Crumble, 0.35f, 1.3f);
        }

        private void HandleBeat(int beat, int inBar)
        {
            if (state != State.Shaking) return;
            beatsLeft--;
            if (beatsLeft <= 0) Fall();
        }

        private void HandleBar(int bar)
        {
            if (state != State.Fallen) return;
            barsLeft--;
            if (barsLeft <= 0) Respawn();
        }

        private void Fall()
        {
            state = State.Fallen;
            stateTime = 0f;
            barsLeft = respawnBars;
            col.enabled = false;
            HiveSfx.Play(HiveSfxId.Crumble, 0.5f, 0.8f);
            HiveFx.Burst(transform.position, HivePalette.Amber, 14, 3.5f, 0.6f, 0.2f, 9f);
        }

        private void Respawn()
        {
            state = State.Idle;
            col.enabled = true;
            visual.localPosition = Vector3.zero;
            visual.gameObject.SetActive(true);
            sr.color = new Color(1f, 0.85f, 0.6f, 1f);
            pop = 1f;
            HiveSfx.Play(HiveSfxId.Appear, 0.3f, 0.8f);
        }

        private void Update()
        {
            if (visual == null) return;
            stateTime += Time.deltaTime;
            pop = Mathf.MoveTowards(pop, 0f, Time.deltaTime * 6f);

            switch (state)
            {
                case State.Idle:
                    visual.localPosition = Vector3.zero;
                    break;
                case State.Shaking:
                {
                    float amp = 0.03f + 0.03f * Mathf.Min(stateTime, 1.5f);
                    visual.localPosition = new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp), 0f);
                    float blink = Mathf.PingPong(stateTime * 8f, 1f);
                    sr.color = Color.Lerp(new Color(1f, 0.85f, 0.6f, 1f), HivePalette.Pink, blink * 0.5f);
                    break;
                }
                case State.Fallen:
                {
                    float t = stateTime;
                    visual.localPosition = new Vector3(0f, -t * t * 18f, 0f);
                    sr.color = new Color(1f, 0.85f, 0.6f, Mathf.Clamp01(1f - t * 1.6f));
                    if (t > 0.7f) visual.gameObject.SetActive(false);
                    break;
                }
            }

            if (state == State.Idle && pop > 0f)
            {
                float s = 1f + 0.14f * pop;
                visual.localScale = new Vector3(s, s, 1f);
            }
            else visual.localScale = Vector3.one;
        }
    }
}
