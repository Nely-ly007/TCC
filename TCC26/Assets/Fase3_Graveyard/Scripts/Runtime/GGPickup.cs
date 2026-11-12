using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>Coletáveis: Vinil (moeda), Nota (cura +15 HP), Coração (cura maior) e Fragmento do Disco Dourado.</summary>
    public class GGPickup : MonoBehaviour
    {
        public enum Kind { Vinyl, HealNote, Heart, Fragment }

        [SerializeField] Kind kind = Kind.Vinyl;
        [Tooltip("Vinil: quantidade. Nota/Coração: HP curado.")]
        [SerializeField] int amount = 1;
        [SerializeField] Transform visual;
        [SerializeField] AudioClip collectSfx;
        [SerializeField] float bobHeight = 0.12f;
        [SerializeField] float pulseAmount = 0.12f;

        public Kind PickupKind { get { return kind; } }

        bool collected;
        Vector3 visualBase;
        Vector3 scaleBase;
        float seed;

        public void Configure(Kind newKind, int newAmount)
        {
            kind = newKind;
            amount = newAmount;
        }

        void Start()
        {
            if (visual == null) visual = transform;
            visualBase = visual.localPosition;
            scaleBase = visual.localScale;
            seed = Random.value * 10f;
        }

        void Update()
        {
            if (visual == null || collected) return;
            float p = Mathf.Pow(1f - GGBeatClock.Phase, 3f);
            visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * 2.4f + seed) * bobHeight);
            visual.localScale = scaleBase * (1f + pulseAmount * p);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            GGPlayerHealth h = other.GetComponentInParent<GGPlayerHealth>();
            if (h == null) return;
            collected = true;

            switch (kind)
            {
                case Kind.Vinyl:
                    if (GGLevelManager.Instance != null) GGLevelManager.Instance.AddVinyls(amount);
                    break;
                case Kind.HealNote:
                case Kind.Heart:
                    h.Heal(amount);
                    break;
                case Kind.Fragment:
                    if (GGLevelManager.Instance != null) GGLevelManager.Instance.CollectFragment();
                    break;
            }

            if (collectSfx != null) AudioSource.PlayClipAtPoint(collectSfx, transform.position);
            Destroy(gameObject);
        }
    }
}
