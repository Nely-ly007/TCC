using System;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>Gira no ritmo: a cada batida dá um "tranco" suave de degreesPerBeat graus.</summary>
    public class GGSpinner : MonoBehaviour
    {
        [SerializeField] float degreesPerBeat = 180f;
        [Tooltip("Marcado: gira em pulsos a cada batida. Desmarcado: gira contínuo.")]
        [SerializeField] bool pulsed = true;

        void Update()
        {
            double pos = GGBeatClock.Position;
            double fl = Math.Floor(pos);
            float fr = (float)(pos - fl);
            double steps = pulsed ? fl + Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fr * 1.6f)) : pos;
            double angle = GGBeatClock.Mod(degreesPerBeat * steps, 360.0);
            transform.localRotation = Quaternion.Euler(0f, 0f, (float)angle);
        }
    }
}
