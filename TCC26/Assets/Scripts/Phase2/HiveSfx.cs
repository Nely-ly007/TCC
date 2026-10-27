using System.Collections.Generic;
using UnityEngine;

namespace PopAdventure.Hive
{
    public enum HiveSfxId
    {
        Beat, Vinyl, Heal, Hit, PlayerHurt, EnemyDie, Slam, Warn, Checkpoint,
        Fragment, Victory, Shoot, Appear, Crumble, Splash, BossRoar, Gate
    }

    /// <summary>
    /// Efeitos sonoros sintetizados em código (AudioClip.Create) — placeholders prontos para jogar.
    /// Troque depois por .wav reais: basta preencher o dicionário "overrides" (ver SetOverride).
    /// </summary>
    public class HiveSfx : MonoBehaviour
    {
        private const int SR = 22050;
        private const int Voices = 8;

        private static HiveSfx inst;
        private static readonly Dictionary<HiveSfxId, AudioClip> clips = new Dictionary<HiveSfxId, AudioClip>();
        private static readonly Dictionary<HiveSfxId, AudioClip> overrides = new Dictionary<HiveSfxId, AudioClip>();
        private static readonly System.Random rng = new System.Random(7);

        public static float MasterVolume = 0.8f;

        private AudioSource[] voices;
        private int next;

        /// <summary>Substitui um som sintetizado por um AudioClip seu.</summary>
        public static void SetOverride(HiveSfxId id, AudioClip clip)
        {
            overrides[id] = clip;
        }

        public static void Play(HiveSfxId id, float volume = 1f, float pitch = 1f)
        {
            if (inst == null)
            {
                var go = new GameObject("HiveSfx");
                inst = go.AddComponent<HiveSfx>();
                inst.voices = new AudioSource[Voices];
                for (int i = 0; i < Voices; i++)
                {
                    var s = go.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    s.spatialBlend = 0f;
                    inst.voices[i] = s;
                }
            }

            AudioClip clip;
            if (!overrides.TryGetValue(id, out clip) || clip == null) clip = GetClip(id);
            if (clip == null) return;

            AudioSource v = null;
            for (int i = 0; i < Voices; i++)
            {
                var c = inst.voices[(inst.next + i) % Voices];
                if (!c.isPlaying) { v = c; inst.next = (inst.next + i + 1) % Voices; break; }
            }
            if (v == null)
            {
                v = inst.voices[inst.next];
                inst.next = (inst.next + 1) % Voices;
            }
            v.pitch = pitch;
            v.volume = Mathf.Clamp01(volume * MasterVolume);
            v.clip = clip;
            v.Play();
        }

        // ============================================================ Síntese

        private static AudioClip GetClip(HiveSfxId id)
        {
            AudioClip c;
            if (clips.TryGetValue(id, out c) && c != null) return c;
            float[] data = Build(id);
            c = AudioClip.Create("hive_" + id, data.Length, 1, SR, false);
            c.SetData(data, 0);
            c.hideFlags = HideFlags.DontUnloadUnusedAsset;
            clips[id] = c;
            return c;
        }

        // wave: 0 seno, 1 quadrada, 2 triangular, 3 ruído
        private static float[] Tone(float f0, float f1, float dur, int wave, float vol)
        {
            int n = Mathf.Max(1, (int)(dur * SR));
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2f * Mathf.PI * f / SR;
                float s;
                switch (wave)
                {
                    case 0: s = Mathf.Sin(phase); break;
                    case 1: s = Mathf.Sin(phase) >= 0f ? 0.6f : -0.6f; break;
                    case 2: s = (2f / Mathf.PI) * Mathf.Asin(Mathf.Sin(phase)); break;
                    default: s = (float)(rng.NextDouble() * 2.0 - 1.0); break;
                }
                float attack = Mathf.Min(1f, i / (0.004f * SR));
                float env = attack * (1f - t) * (1f - t);
                d[i] = s * env * vol;
            }
            return d;
        }

        private static float[] Concat(params float[][] parts)
        {
            int n = 0;
            for (int i = 0; i < parts.Length; i++) n += parts[i].Length;
            var r = new float[n];
            int o = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                System.Array.Copy(parts[i], 0, r, o, parts[i].Length);
                o += parts[i].Length;
            }
            return r;
        }

        private static float[] Mix(params float[][] parts)
        {
            int n = 0;
            for (int i = 0; i < parts.Length; i++) n = Mathf.Max(n, parts[i].Length);
            var r = new float[n];
            for (int i = 0; i < parts.Length; i++)
                for (int k = 0; k < parts[i].Length; k++) r[k] += parts[i][k];
            for (int k = 0; k < n; k++) r[k] = Mathf.Clamp(r[k], -1f, 1f);
            return r;
        }

        private static float[] Silence(float dur)
        {
            return new float[Mathf.Max(1, (int)(dur * SR))];
        }

        private static float[] Arp(float[] freqs, float noteDur, int wave, float vol)
        {
            var parts = new float[freqs.Length][];
            for (int i = 0; i < freqs.Length; i++) parts[i] = Tone(freqs[i], freqs[i], noteDur, wave, vol);
            return Concat(parts);
        }

        private static float[] Build(HiveSfxId id)
        {
            switch (id)
            {
                case HiveSfxId.Beat:
                    return Tone(1800f, 1300f, 0.03f, 0, 0.22f);
                case HiveSfxId.Vinyl:
                    return Concat(Tone(988f, 988f, 0.05f, 2, 0.35f), Tone(1319f, 1319f, 0.12f, 2, 0.35f));
                case HiveSfxId.Heal:
                    return Arp(new[] { 523f, 659f, 784f, 1047f }, 0.07f, 2, 0.4f);
                case HiveSfxId.Hit:
                    return Mix(Tone(240f, 90f, 0.09f, 1, 0.45f), Tone(0f, 0f, 0.07f, 3, 0.35f));
                case HiveSfxId.PlayerHurt:
                    return Mix(Tone(330f, 70f, 0.28f, 1, 0.5f), Tone(0f, 0f, 0.2f, 3, 0.3f));
                case HiveSfxId.EnemyDie:
                    return Mix(Tone(700f, 90f, 0.28f, 1, 0.4f), Tone(0f, 0f, 0.12f, 3, 0.25f));
                case HiveSfxId.Slam:
                    return Mix(Tone(130f, 38f, 0.45f, 0, 0.95f), Tone(0f, 0f, 0.25f, 3, 0.55f));
                case HiveSfxId.Warn:
                    return Concat(Tone(1000f, 1000f, 0.07f, 1, 0.28f), Silence(0.04f), Tone(1000f, 1000f, 0.07f, 1, 0.28f));
                case HiveSfxId.Checkpoint:
                    return Mix(Tone(523f, 523f, 0.5f, 2, 0.3f), Tone(659f, 659f, 0.5f, 2, 0.25f),
                               Tone(784f, 784f, 0.5f, 2, 0.25f), Tone(1047f, 1047f, 0.5f, 0, 0.2f));
                case HiveSfxId.Fragment:
                case HiveSfxId.Victory:
                    return Arp(new[] { 523f, 659f, 784f, 1047f, 784f, 1047f, 1319f, 1568f }, 0.11f, 2, 0.4f);
                case HiveSfxId.Shoot:
                    return Tone(950f, 320f, 0.1f, 1, 0.28f);
                case HiveSfxId.Appear:
                    return Tone(420f, 840f, 0.08f, 2, 0.28f);
                case HiveSfxId.Crumble:
                    return Tone(0f, 0f, 0.35f, 3, 0.5f);
                case HiveSfxId.Splash:
                    return Mix(Tone(0f, 0f, 0.1f, 3, 0.3f), Tone(500f, 200f, 0.1f, 0, 0.3f));
                case HiveSfxId.BossRoar:
                    return Mix(Tone(190f, 55f, 0.7f, 1, 0.55f), Tone(95f, 40f, 0.7f, 0, 0.5f));
                case HiveSfxId.Gate:
                    return Mix(Tone(95f, 60f, 0.55f, 1, 0.45f), Tone(0f, 0f, 0.4f, 3, 0.3f));
                default:
                    return Tone(440f, 440f, 0.1f, 0, 0.3f);
            }
        }
    }
}
