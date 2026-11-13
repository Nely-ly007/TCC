using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Hazard que sobe do chão no ritmo (mãos, spikes). Pattern: '1' = para cima (causa dano), '0' = escondido.
    /// A batida antes de subir ele "avisa" (sobe um pouco e treme). Coloque o sprite com pivô embaixo, na altura do chão.
    /// </summary>
    public class MTBeatHazard : MTBeatListener
    {
        public string pattern = "0010";
        public int beatOffset = 0;
        public float riseDistance = 1.6f;
        public float riseSpeed = 12f;
        [Range(0f, 0.6f)] public float telegraphFraction = 0.25f;
        public float damage = 15f;

        Vector3 shownLocal;
        float current;
        float target;
        bool telegraph;
        MTContactDamage contact;

        void Awake()
        {
            shownLocal = transform.localPosition;
            contact = GetComponent<MTContactDamage>();
            if (contact == null) contact = gameObject.AddComponent<MTContactDamage>();
            contact.damage = damage;
            contact.active = false;
        }

        protected override void Start()
        {
            base.Start();
            MTBeatClock c = MTBeatClock.Instance;
            Apply(c != null && c.Running ? Mathf.Max(0, c.CurrentBeat) : 0);
            current = target;
        }

        protected override void OnBeat(int beatAbs, int beatInBar, int bar)
        {
            Apply(beatAbs);
        }

        void Apply(int beat)
        {
            int n = pattern != null ? pattern.Length : 0;
            if (n == 0) return;
            int i = MTUtil.Mod(beat + beatOffset, n);
            int next = (i + 1) % n;
            bool up = pattern[i] == '1';
            telegraph = !up && pattern[next] == '1';
            target = up ? 1f : (telegraph ? telegraphFraction : 0f);
        }

        void Update()
        {
            current = Mathf.MoveTowards(current, target, riseSpeed / Mathf.Max(0.1f, riseDistance) * Time.deltaTime);
            Vector3 p = shownLocal + Vector3.down * (riseDistance * (1f - current));
            if (telegraph) p.x += Mathf.Sin(Time.time * 70f) * 0.04f;
            transform.localPosition = p;
            contact.active = current > 0.8f;
        }
    }
}
