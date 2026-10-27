using UnityEngine;

namespace PopAdventure.Hive
{
    /// <summary>
    /// Adicionado automaticamente ao Player pelo HiveLevelBootstrap.
    /// Aplica efeitos da fase SEM tocar no PlayerController: mel (limita velocidade).
    /// Roda DEPOIS do PlayerController (ExecutionOrder alto) e usa "clamp" (idempotente),
    /// então funciona mesmo que o controller defina a velocidade em Update ou FixedUpdate.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class HivePlayerStatus : MonoBehaviour
    {
        private Rigidbody2D rb;
        private float honeyUntil;
        private float honeyMaxX = 2.4f;
        private float honeyMaxUp = 9f;

        public bool InHoney { get { return Time.time <= honeyUntil; } }

        public static HivePlayerStatus For(GameObject player)
        {
            if (player == null) return null;
            var s = player.GetComponent<HivePlayerStatus>();
            if (s == null) s = player.AddComponent<HivePlayerStatus>();
            return s;
        }

        /// <summary>Chamado a cada frame de física enquanto o jogador está no mel.</summary>
        public void Honey(float maxSpeedX, float maxUpSpeed)
        {
            honeyUntil = Time.time + 0.12f;
            honeyMaxX = maxSpeedX;
            honeyMaxUp = maxUpSpeed;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = GetComponentInParent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (rb == null || !InHoney) return;
            Vector2 v = HiveBridge.GetVelocity(rb);
            v.x = Mathf.Clamp(v.x, -honeyMaxX, honeyMaxX);
            if (v.y > honeyMaxUp) v.y = honeyMaxUp;
            HiveBridge.SetVelocity(rb, v);
        }
    }
}
