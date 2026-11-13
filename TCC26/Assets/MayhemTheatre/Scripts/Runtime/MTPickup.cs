using UnityEngine;
using UnityEngine.Events;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Nota (cura +15), Vinil (moeda) ou Fragmento do Disco Dourado (fim da fase).</summary>
    public class MTPickup : MonoBehaviour
    {
        public enum PickupKind { Note, Vinyl, Fragment }

        public PickupKind kind = PickupKind.Vinyl;
        public float healAmount = 15f;
        public int value = 1;
        [Tooltip("Vinil/Fragmento: ligue aqui o método do seu GameManager (ex.: AddVinyl / CollectFragment).")]
        public UnityEvent onCollected = new UnityEvent();
        public float bobHeight = 0.12f;
        public float bobSpeed = 2f;

        Vector3 basePos;
        bool taken;

        void Start()
        {
            basePos = transform.position;
        }

        void Update()
        {
            if (bobHeight <= 0f) return;
            transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + basePos.x) * bobHeight);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (taken) return;
            MTPlayerHook hook = other.GetComponentInParent<MTPlayerHook>();
            if (hook == null) return;
            taken = true;

            switch (kind)
            {
                case PickupKind.Note:
                    hook.Heal(healAmount);
                    break;
                case PickupKind.Fragment:
                    if (MTLevelController.Instance != null) MTLevelController.Instance.CompleteLevel();
                    break;
            }

            onCollected.Invoke();
            Destroy(gameObject);
        }
    }
}
