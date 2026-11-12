using System;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Mão de zumbi saindo do chão no ritmo. Ciclo: monte de terra (aviso, 1 batida) → sobe → mão ativa → afunda.
    /// Modo "loop" (obstáculo fixo) ou "one-shot" (invocada pelo boss).
    /// </summary>
    public class GGZombieHand : GGHazard
    {
        [SerializeField] SpriteRenderer sr;
        [SerializeField] Sprite moundSprite;
        [SerializeField] Sprite risingSprite;
        [SerializeField] Sprite upSprite;
        [SerializeField] float periodBeats = 6f;
        [SerializeField] float offsetBeats = 0f;
        [SerializeField] float telegraphBeats = 1f;
        [SerializeField] float upBeats = 1f;
        [Tooltip("Só roda o ciclo quando o Player está perto.")]
        [SerializeField] bool proximityOnly = false;
        [SerializeField] float activationRange = 7f;

        bool oneShot;
        double oneShotStart;
        bool danger;

        public override bool IsDangerous { get { return danger; } }

        public void Configure(float newPeriod, float newOffset, bool newProximityOnly)
        {
            periodBeats = newPeriod;
            offsetBeats = newOffset;
            proximityOnly = newProximityOnly;
        }

        /// <summary>Invocação única: começa a avisar em startBeat (batida absoluta) e some sozinha no fim.</summary>
        public void StartOneShot(double startBeat, float telegraph, float up)
        {
            oneShot = true;
            oneShotStart = startBeat;
            telegraphBeats = telegraph;
            upBeats = up;
        }

        void Awake()
        {
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (sr == null) return;
            float riseEnd = telegraphBeats + 0.25f;
            float upEnd = riseEnd + upBeats;
            float sinkEnd = upEnd + 0.25f;
            float c;

            if (oneShot)
            {
                c = (float)(GGBeatClock.Position - oneShotStart);
                if (c > sinkEnd) { Destroy(gameObject); return; }
            }
            else
            {
                if (proximityOnly && !PlayerNear()) { Hide(); return; }
                c = (float)GGBeatClock.Mod(GGBeatClock.Position - offsetBeats, periodBeats);
            }

            danger = false;
            if (c < 0f) { Hide(); return; }

            sr.enabled = true;
            Vector3 lp = sr.transform.localPosition;
            lp.x = 0f;
            if (c < telegraphBeats)
            {
                sr.sprite = moundSprite;
                lp.x = Mathf.Sin(Time.time * 50f) * 0.03f;
            }
            else if (c < riseEnd) sr.sprite = risingSprite;
            else if (c < upEnd) { sr.sprite = upSprite; danger = true; }
            else if (c < sinkEnd) sr.sprite = risingSprite;
            else Hide();
            sr.transform.localPosition = lp;
        }

        bool PlayerNear()
        {
            Transform p = GGUtil.Player;
            return p != null && Mathf.Abs(p.position.x - transform.position.x) < activationRange;
        }

        void Hide()
        {
            danger = false;
            if (sr != null) sr.enabled = false;
        }
    }
}
