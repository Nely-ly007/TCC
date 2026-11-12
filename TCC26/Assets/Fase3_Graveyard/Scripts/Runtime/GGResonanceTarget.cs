using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Alvo de Ressonância: atinja com a nota musical para "acender". Fica aceso por holdBeats batidas.
    /// Combine vários alvos com um GGResonanceDoor para destravar portas.
    /// </summary>
    public class GGResonanceTarget : MonoBehaviour, IGGDamageable
    {
        [SerializeField] SpriteRenderer sr;
        [SerializeField] Color offColor = new Color(0.45f, 0.45f, 0.55f, 1f);
        [SerializeField] Color onColor = new Color(0.7f, 1f, 0.6f, 1f);
        [SerializeField] float holdBeats = 8f;
        public UnityEvent onLit;

        double litUntil = -1.0;

        public bool IsLit { get { return GGBeatClock.Position < litUntil; } }

        public void SetHoldBeats(float beats) { holdBeats = beats; }

        void Awake()
        {
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        }

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            bool was = IsLit;
            litUntil = GGBeatClock.Position + holdBeats;
            if (!was && onLit != null) onLit.Invoke();
            GGCameraFollow.Shake(0.05f, 0.1f);
        }

        void Update()
        {
            if (sr == null) return;
            if (IsLit)
            {
                float p = Mathf.Pow(1f - GGBeatClock.Phase, 2f);
                sr.color = Color.Lerp(onColor, Color.white, p * 0.5f);
            }
            else sr.color = offColor;
        }
    }
}
