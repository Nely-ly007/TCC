using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Câmera 2D simples da Fase 3: segue o Player (tag "Player"), com antecipação, limites e tremor.
    /// Se preferir o Cinemachine da fase 2, remova este componente — o resto da fase continua funcionando
    /// (GGCameraFollow.Shake/LockBounds apenas não fazem nada sem ele).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GGCameraFollow : MonoBehaviour
    {
        static GGCameraFollow instance;

        [SerializeField] Transform target;
        [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
        [SerializeField] float smoothTime = 0.18f;
        [SerializeField] float lookAhead = 1.5f;
        [SerializeField] bool useLevelBounds = true;
        [Tooltip("x, y, largura, altura em unidades do mundo.")]
        [SerializeField] Rect levelBounds = new Rect(-8f, -14f, 270f, 40f);

        Camera cam;
        Vector3 basePos;
        Vector3 velocity;
        float lastTargetX;
        float aheadCurrent;
        bool initialized;
        bool locked;
        Rect lockedBounds;
        float shakeTime;
        float shakeDuration;
        float shakeAmplitude;

        void Awake()
        {
            instance = this;
            cam = GetComponent<Camera>();
            basePos = transform.position;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public void SetLevelBounds(Rect bounds) { levelBounds = bounds; }

        public static void Shake(float amplitude, float duration)
        {
            if (instance == null) return;
            instance.shakeAmplitude = Mathf.Max(instance.shakeAmplitude * (instance.shakeTime > 0f ? 1f : 0f), amplitude);
            instance.shakeDuration = Mathf.Max(0.05f, duration);
            instance.shakeTime = duration;
        }

        public static void LockBounds(Rect bounds)
        {
            if (instance == null) return;
            instance.locked = true;
            instance.lockedBounds = bounds;
        }

        public static void UnlockBounds()
        {
            if (instance == null) return;
            instance.locked = false;
        }

        Vector3 Clamp(Vector3 p, Rect b)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float minX = b.xMin + halfW, maxX = b.xMax - halfW;
            float minY = b.yMin + halfH, maxY = b.yMax - halfH;
            p.x = minX > maxX ? b.center.x : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? b.center.y : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                Transform p = GGUtil.Player;
                if (p == null) return;
                target = p;
                lastTargetX = p.position.x;
                initialized = false;
            }

            float dx = target.position.x - lastTargetX;
            lastTargetX = target.position.x;
            float aheadTarget = Mathf.Abs(dx) > 0.0005f ? Mathf.Sign(dx) * lookAhead : aheadCurrent;
            aheadCurrent = Mathf.Lerp(aheadCurrent, aheadTarget, 1f - Mathf.Exp(-3f * Time.deltaTime));

            Vector3 desired = new Vector3(
                target.position.x + offset.x + (locked ? 0f : aheadCurrent),
                target.position.y + offset.y,
                basePos.z);

            if (locked) desired = Clamp(desired, lockedBounds);
            else if (useLevelBounds) desired = Clamp(desired, levelBounds);

            if (!initialized)
            {
                basePos = desired;
                initialized = true;
            }
            else
            {
                basePos = Vector3.SmoothDamp(basePos, desired, ref velocity, locked ? smoothTime * 1.6f : smoothTime);
            }

            Vector3 shake = Vector3.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                float k = Mathf.Clamp01(shakeTime / shakeDuration);
                Vector2 r = Random.insideUnitCircle * shakeAmplitude * k;
                shake = new Vector3(r.x, r.y, 0f);
            }
            transform.position = basePos + shake;
        }
    }
}
