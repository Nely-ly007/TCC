using System;
using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Animação de sprites por código (usada nos inimigos: corvo, lobo). O boss NÃO usa isto:
    /// o boss é animado por você no Animator do Unity.
    /// </summary>
    public class GGSpriteAnimator : MonoBehaviour
    {
        [Serializable]
        public class Clip
        {
            public string name;
            public Sprite[] frames;
            public float fps = 8f;
            [Tooltip("Se > 0, o ciclo completo dura esta quantidade de batidas (frames travados na música).")]
            public float beatsPerLoop = 0f;
            public bool loop = true;
        }

        [SerializeField] SpriteRenderer target;
        [SerializeField] Clip[] clips;

        Clip current;
        float time;

        public string CurrentName { get { return current != null ? current.name : null; } }

        public void Setup(SpriteRenderer renderer, Clip[] newClips)
        {
            target = renderer;
            clips = newClips;
        }

        void Awake()
        {
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            if (current == null && clips != null && clips.Length > 0) Play(clips[0].name, true);
        }

        public void Play(string clipName, bool restart = false)
        {
            if (clips == null) return;
            if (!restart && current != null && current.name == clipName) return;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].name == clipName)
                {
                    current = clips[i];
                    time = 0f;
                    Apply();
                    return;
                }
            }
        }

        void Update()
        {
            if (current == null) return;
            time += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (current == null || current.frames == null || current.frames.Length == 0 || target == null) return;
            int n = current.frames.Length;
            int idx;
            if (current.loop && current.beatsPerLoop > 0f)
            {
                double ph = GGBeatClock.Position / current.beatsPerLoop;
                float frac = (float)(ph - Math.Floor(ph));
                idx = (int)(frac * n);
            }
            else
            {
                float f = time * current.fps;
                idx = current.loop ? ((int)f) % n : Mathf.Min((int)f, n - 1);
            }
            target.sprite = current.frames[Mathf.Clamp(idx, 0, n - 1)];
        }
    }
}
