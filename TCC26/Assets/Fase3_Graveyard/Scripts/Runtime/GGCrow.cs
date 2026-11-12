using System.Collections;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Corvo: fica pousado (ou pairando). Quando o Player chega perto, grasna, e MERGULHA no beat seguinte;
    /// depois voa de volta ao poleiro. Vida 20 (2 notas), dano 15.
    /// </summary>
    public class GGCrow : GGEnemyBase
    {
        [Header("Corvo")]
        [SerializeField] float detectRange = 9f;
        [Tooltip("Marcado: paira batendo asas. Desmarcado: fica pousado.")]
        [SerializeField] bool hoverMode = false;
        [SerializeField] float diveBeats = 1f;
        [SerializeField] float retreatBeats = 2f;
        [SerializeField] float cooldownBeats = 2f;
        [SerializeField] float arcHeight = 1.6f;

        Vector3 home;

        public void SetHover(bool value) { hoverMode = value; }

        protected override void Awake()
        {
            base.Awake();
            home = transform.position;
        }

        void Start()
        {
            StartCoroutine(Brain());
        }

        bool PlayerInRange(float mult)
        {
            Transform p = Player;
            return p != null && Vector2.Distance(p.position, transform.position) < detectRange * mult;
        }

        void IdleStep()
        {
            if (hoverMode)
            {
                PlayAnim("Fly");
                transform.position = home + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.25f);
            }
            else PlayAnim("Idle");
            if (PlayerInRange(1.5f)) FacePlayer();
        }

        IEnumerator Brain()
        {
            while (!dead)
            {
                // 1) espera o Player chegar perto
                while (!dead && !PlayerInRange(1f))
                {
                    IdleStep();
                    yield return null;
                }
                if (dead) yield break;

                // 2) aviso: grasna, alinha na batida e espera mais uma batida
                PlayAnim("Caw");
                FacePlayer();
                yield return StartCoroutine(GGBeat.WaitNextBeat());
                FacePlayer();
                yield return StartCoroutine(GGBeat.Wait(1.0));

                // 3) mergulho (dura diveBeats batidas)
                Vector3 start = transform.position;
                Vector3 target = Player != null ? Player.position + Vector3.up * 0.6f : start;
                SetFacing(target.x >= start.x);
                PlayAnim("Dive");
                double t0 = GGBeatClock.Position;
                while (!dead)
                {
                    float t = (float)((GGBeatClock.Position - t0) / diveBeats);
                    if (t >= 1f) break;
                    float x = Mathf.Lerp(start.x, target.x, t);
                    float y = Mathf.Lerp(start.y, target.y, t * t);
                    transform.position = new Vector3(x, y, start.z);
                    yield return null;
                }
                if (dead) yield break;

                // 4) volta ao poleiro em arco
                Vector3 from = transform.position;
                SetFacing(home.x >= from.x);
                PlayAnim("Fly");
                double t1 = GGBeatClock.Position;
                while (!dead)
                {
                    float t = (float)((GGBeatClock.Position - t1) / retreatBeats);
                    if (t >= 1f) break;
                    float k = Mathf.SmoothStep(0f, 1f, t);
                    Vector3 p = Vector3.Lerp(from, home, k);
                    p.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                    transform.position = p;
                    yield return null;
                }
                transform.position = home;

                // 5) descanso rítmico
                double end = GGBeatClock.Position + cooldownBeats;
                while (!dead && GGBeatClock.Position < end)
                {
                    IdleStep();
                    yield return null;
                }
            }
        }
    }
}
