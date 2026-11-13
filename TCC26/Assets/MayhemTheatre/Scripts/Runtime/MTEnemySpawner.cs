using UnityEngine;

namespace PopAdventure.MayhemTheatre
{
    /// <summary>
    /// Ponto de spawn de inimigo. Cria o inimigo quando o Player chega perto (Activation Distance).
    /// Prefab preenchido = usa o prefab do inimigo da sua fase (reprise das fases 1-3). Vazio = placeholder com o sprite abaixo.
    /// </summary>
    public class MTEnemySpawner : MonoBehaviour
    {
        [Header("Seu prefab (opcional)")]
        public GameObject prefab;

        [Header("Placeholder (usado quando Prefab está vazio)")]
        public MTEnemyKind kind = MTEnemyKind.Chaser;
        public Sprite sprite;
        public Color tint = Color.white;
        public float spriteHeight = 1.7f;
        public float health = 20f;
        public float damage = 15f;
        public float speed = 2.5f;
        public float aggroRange = 10f;
        public Sprite noteSprite;
        public GameObject notePrefab;

        public float activationDistance = 24f;

        bool done;
        GameObject spawned;
        public GameObject Spawned { get { return spawned; } }

        void Update()
        {
            if (done) return;
            Transform p = MTUtil.FindPlayer();
            if (p == null) return;
            if (Mathf.Abs(p.position.x - transform.position.x) < activationDistance) Spawn();
        }

        void Spawn()
        {
            done = true;
            if (prefab != null)
            {
                spawned = Instantiate(prefab, transform.position, Quaternion.identity);
                return;
            }
            MTEnemy e = MTEnemy.Create(transform.position, kind, sprite, tint, spriteHeight, health, damage, speed, aggroRange, noteSprite, notePrefab);
            spawned = e.gameObject;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.5f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.4f);
        }
    }
}
