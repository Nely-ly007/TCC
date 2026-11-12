using System;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>Pulsa escala e brilho no beat (lanternas, chamas, janelas, bandeiras…).</summary>
    public class GGBeatPulse : MonoBehaviour
    {
        [SerializeField] float scaleAmount = 0.06f;
        [SerializeField, Range(0f, 1f)] float brightness = 0.35f;
        [Tooltip("1 = toda batida, 2 = a cada 2 batidas…")]
        [SerializeField] int everyBeats = 1;
        [SerializeField] float sharpness = 3f;

        SpriteRenderer[] renderers;
        Color[] baseColors;
        Vector3 baseScale;

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
            baseScale = transform.localScale;
        }

        void Update()
        {
            double pos = GGBeatClock.Position / Mathf.Max(1, everyBeats);
            float ph = (float)(pos - Math.Floor(pos));
            float pulse = Mathf.Pow(1f - ph, sharpness);
            transform.localScale = baseScale * (1f + scaleAmount * pulse);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].color = Color.Lerp(baseColors[i], Color.white, brightness * pulse);
        }
    }
}
