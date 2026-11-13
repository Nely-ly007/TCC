using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>Projétil de ataque do boss (patins, abelhas, máscaras). Move-se por código e causa dano ao Player.</summary>
    public class MTShot : MonoBehaviour
    {
        public Vector2 velocity;
        public float damage = 15f;
        public float lifetime = 6f;
        public float spinDegrees = 0f;
        public float sineAmplitude = 0f;
        public float sineFrequency = 3f;
        public bool destroyOnHit = true;

        Vector3 basePos;
        Vector2 perp;
        float born;

        void Start()
        {
            born = Time.time;
            basePos = transform.position;
            Vector2 n = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Vector2.right;
            perp = new Vector2(-n.y, n.x);
        }

        void Update()
        {
            basePos += (Vector3)(velocity * Time.deltaTime);
            Vector3 p = basePos;
            if (sineAmplitude > 0f) p += (Vector3)(perp * (Mathf.Sin((Time.time - born) * sineFrequency) * sineAmplitude));
            transform.position = p;

            if (!Mathf.Approximately(spinDegrees, 0f)) transform.Rotate(0f, 0f, spinDegrees * Time.deltaTime);
            if (Time.time - born > lifetime) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            MTPlayerHook hook = other.GetComponentInParent<MTPlayerHook>();
            if (hook == null) return;
            hook.TakeDamage(damage, transform.position);
            if (destroyOnHit) Destroy(gameObject);
        }
    }
}
