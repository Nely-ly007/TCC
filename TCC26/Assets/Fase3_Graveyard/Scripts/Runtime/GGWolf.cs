using System.Collections;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Lobo (inimigo forte, dano 30): patrulha o chão; ao ver o Player uiva (1 batida), agacha (1 batida)
    /// e investe no ritmo por chargeBeats batidas; depois descansa vulnerável. Vida 40 (4 notas).
    /// A raiz fica nos PÉS do lobo (o builder alinha o sprite).
    /// </summary>
    public class GGWolf : GGEnemyBase
    {
        [Header("Lobo")]
        [SerializeField] float patrolSpeed = 1.6f;
        [SerializeField] float patrolRange = 4f;
        [SerializeField] float chargeSpeed = 7.5f;
        [SerializeField] float detectRange = 9f;
        [SerializeField] float detectHeight = 2.5f;
        [SerializeField] float chargeBeats = 2f;
        [SerializeField] float recoverBeats = 2f;
        [SerializeField] float halfWidth = 0.55f;

        float homeX;
        float dir = 1f;

        protected override void Awake()
        {
            base.Awake();
            homeX = transform.position.x;
        }

        void Start()
        {
            StartCoroutine(Brain());
        }

        bool PlayerSpotted()
        {
            Transform p = Player;
            if (p == null) return false;
            Vector3 pos = transform.position;
            return Mathf.Abs(p.position.x - pos.x) < detectRange && Mathf.Abs(p.position.y - pos.y) < detectHeight;
        }

        /// <summary>Anda no chão. Retorna false se bateu na parede ou chegou na borda.</summary>
        bool MoveGrounded(float direction, float speed)
        {
            Vector2 pos = transform.position;
            float step = direction * speed * Time.deltaTime;

            RaycastHit2D wall;
            if (GGUtil.GroundRaycast(pos + Vector2.up * 0.4f, new Vector2(direction, 0f), Mathf.Abs(step) + halfWidth, out wall)
                && wall.normal.y < 0.5f)
                return false;

            RaycastHit2D ground;
            Vector2 ahead = new Vector2(pos.x + direction * halfWidth + step, pos.y + 0.6f);
            if (!GGUtil.GroundRaycast(ahead, Vector2.down, 1.6f, out ground)) return false;

            transform.position = new Vector3(pos.x + step, ground.point.y, transform.position.z);
            return true;
        }

        void PatrolStep()
        {
            float x = transform.position.x;
            if (Mathf.Abs(x - homeX) > patrolRange) dir = Mathf.Sign(homeX - x);
            if (!MoveGrounded(dir, patrolSpeed)) dir = -dir;
            SetFacing(dir > 0f);
        }

        IEnumerator Brain()
        {
            while (!dead)
            {
                // patrulha
                PlayAnim("Run");
                while (!dead && !PlayerSpotted())
                {
                    PatrolStep();
                    yield return null;
                }
                if (dead) yield break;

                // aviso: uiva até a próxima batida, agacha por mais uma
                Transform p = Player;
                dir = (p != null && p.position.x < transform.position.x) ? -1f : 1f;
                SetFacing(dir > 0f);
                PlayAnim("Howl");
                yield return StartCoroutine(GGBeat.WaitNextBeat());
                PlayAnim("Crouch");
                yield return StartCoroutine(GGBeat.Wait(1.0));

                // investida no ritmo
                PlayAnim("Run");
                double end = GGBeatClock.Position + chargeBeats;
                while (!dead && GGBeatClock.Position < end)
                {
                    if (!MoveGrounded(dir, chargeSpeed)) break;
                    yield return null;
                }

                // descanso (vulnerável)
                PlayAnim("Crouch");
                yield return StartCoroutine(GGBeat.Wait(recoverBeats));
                homeX = transform.position.x;
            }
        }
    }
}
