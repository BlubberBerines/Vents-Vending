using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// Placeholder ambience and SFX synthesised in code (GDD §16 hideout/vent sounds) until real recordings exist.
    /// Deterministic: the same seed always produces the same WAV. Loops crossfade their tail into their head so they
    /// repeat seamlessly, and every periodic component uses a whole number of cycles per loop.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int Rate = 44100;
        private const string AmbienceFolder = "Assets/_Project/Audio/Ambience/Generated";
        private const string SfxFolder = "Assets/_Project/Audio/SFX/Generated";
        private const int Version = 1; // bump to regenerate everything

        public static AudioClip RoomHum => Get(AmbienceFolder, "amb_room_hum", 8f, true, RoomHumSynth);
        public static AudioClip DuctAir => Get(AmbienceFolder, "amb_duct_air", 6f, true, DuctAirSynth);
        public static AudioClip SmallFan => Get(AmbienceFolder, "amb_small_fan", 4f, true, SmallFanSynth);
        public static AudioClip BigFan => Get(AmbienceFolder, "amb_big_fan", 4f, true, BigFanSynth);
        public static AudioClip Buzz => Get(AmbienceFolder, "amb_electric_buzz", 2f, true, BuzzSynth);
        public static AudioClip Drip => Get(SfxFolder, "sfx_drip", 0.45f, false, (b, r) => DripSynth(b, r, 1500f));
        public static AudioClip DripLow => Get(SfxFolder, "sfx_drip_low", 0.45f, false, (b, r) => DripSynth(b, r, 1050f));
        public static AudioClip KnockA => Get(SfxFolder, "sfx_pipe_knock_a", 0.9f, false, (b, r) => KnockSynth(b, r, 170f, 0.9f));
        public static AudioClip KnockB => Get(SfxFolder, "sfx_pipe_knock_b", 0.9f, false, (b, r) => KnockSynth(b, r, 260f, 0.7f));
        public static AudioClip Tick => Get(SfxFolder, "sfx_pipe_tick", 0.25f, false, TickSynth);
        public static AudioClip Clunk => Get(SfxFolder, "sfx_clunk", 0.45f, false, ClunkSynth);
        public static AudioClip FlickerBuzz => Get(SfxFolder, "sfx_flicker_buzz", 0.6f, false, FlickerSynth);

        // ------------------------------------------------------------------ Synths (buffer, rng)

        private static void RoomHumSynth(float[] b, System.Random r)
        {
            float brown = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                brown = (brown + 0.02f * White(r)) / 1.02f;
                float wobble = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                b[i] = (brown * 3.5f + 0.16f * Mathf.Sin(2f * Mathf.PI * 55f * t) + 0.06f * Mathf.Sin(2f * Mathf.PI * 110f * t)
                        + 0.025f * Mathf.Sin(2f * Mathf.PI * 165f * t)) * wobble;
            }
        }

        private static void DuctAirSynth(float[] b, System.Random r)
        {
            float lo = 0f, hi = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float w = White(r);
                hi += 0.12f * (w - hi);   // ~900 Hz low-pass
                lo += 0.025f * (w - lo);  // ~180 Hz low-pass
                float gust = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * (1f / 3f) * t) * Mathf.Sin(2f * Mathf.PI * 0.5f * t + 0.7f);
                b[i] = (hi - lo * 0.7f) * gust;
            }
        }

        private static void SmallFanSynth(float[] b, System.Random r)
        {
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                lp += 0.1f * (White(r) - lp);
                float blade = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 24f * t);
                b[i] = lp * blade + 0.07f * Mathf.Sin(2f * Mathf.PI * 120f * t) + 0.035f * Mathf.Sin(2f * Mathf.PI * 240f * t)
                       + 0.015f * Mathf.Sin(2f * Mathf.PI * 600f * t);
            }
        }

        private static void BigFanSynth(float[] b, System.Random r)
        {
            float brown = 0f, lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float w = White(r);
                brown = (brown + 0.02f * w) / 1.02f;
                lp += 0.05f * (w - lp);
                float thump = 0.45f + 0.55f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 5f * t), 3f); // three blades ≈ 5 Hz
                b[i] = (brown * 4f + lp * 0.8f) * thump + 0.18f * Mathf.Sin(2f * Mathf.PI * 40f * t) + 0.06f * Mathf.Sin(2f * Mathf.PI * 80f * t);
            }
        }

        private static void BuzzSynth(float[] b, System.Random r)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int n = 1; n <= 14; n++)
                {
                    s += Mathf.Sin(2f * Mathf.PI * 100f * n * t) / n;
                }
                float swell = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                b[i] = (s * 0.22f + White(r) * 0.03f) * swell;
            }
        }

        private static void DripSynth(float[] b, System.Random r, float startHz)
        {
            float phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float f = startHz * Mathf.Lerp(0.45f, 1f, Mathf.Exp(-t / 0.018f)); // quick downward plink
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Exp(-t / 0.055f) * Mathf.Clamp01(t / 0.0015f);
                float splash = White(r) * Mathf.Exp(-t / 0.006f) * 0.25f;
                b[i] = Mathf.Sin(phase) * env + splash;
            }
        }

        private static void KnockSynth(float[] b, System.Random r, float baseHz, float decay)
        {
            float[] ratios = { 1f, 2.56f, 4.49f, 6.83f, 9.1f };
            float[] amps = { 1f, 0.6f, 0.35f, 0.2f, 0.12f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int p = 0; p < ratios.Length; p++)
                {
                    s += amps[p] * Mathf.Sin(2f * Mathf.PI * baseHz * ratios[p] * t) * Mathf.Exp(-t / (decay * 0.35f / (1f + p * 0.8f)));
                }
                float click = White(r) * Mathf.Exp(-t / 0.004f) * 0.5f;
                b[i] = s * Mathf.Clamp01(t / 0.001f) + click;
            }
        }

        private static void TickSynth(float[] b, System.Random r)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                b[i] = (Mathf.Sin(2f * Mathf.PI * 2300f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 3700f * t) * 0.3f) * Mathf.Exp(-t / 0.018f)
                       + White(r) * Mathf.Exp(-t / 0.002f) * 0.4f;
            }
        }

        private static void ClunkSynth(float[] b, System.Random r)
        {
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                lp += 0.08f * (White(r) - lp);
                float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(70f, 130f, Mathf.Exp(-t / 0.02f)) * t) * Mathf.Exp(-t / 0.08f);
                float rattle = lp * Mathf.Exp(-t / 0.05f) * 1.5f;
                float clank = Mathf.Sin(2f * Mathf.PI * 840f * t) * Mathf.Exp(-t / 0.03f) * 0.25f;
                b[i] = thump + rattle + clank;
            }
        }

        private static void FlickerSynth(float[] b, System.Random r)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int n = 1; n <= 10; n++)
                {
                    s += Mathf.Sin(2f * Mathf.PI * 100f * n * t) / n;
                }
                float gate = (Mathf.Sin(2f * Mathf.PI * 17f * t) + Mathf.Sin(2f * Mathf.PI * 11f * t)) > 0.2f ? 1f : 0.15f;
                float env = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((0.6f - t) / 0.1f);
                b[i] = s * gate * env * 0.5f + White(r) * gate * env * 0.08f;
            }
        }

        // ------------------------------------------------------------------ Plumbing

        /// <summary>FNV-1a: string.GetHashCode is not guaranteed stable across runtimes.</summary>
        private static int StableHash(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s)
                {
                    h = (h ^ c) * 16777619;
                }
                return (int)h;
            }
        }

        private static float White(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

        private static AudioClip Get(string folder, string name, float seconds, bool loop, Action<float[], System.Random> synth)
        {
            string path = $"{folder}/{name}_v{Version}.wav";
            var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (existing != null)
            {
                return existing;
            }

            int length = Mathf.RoundToInt(seconds * Rate);
            int fade = loop ? Rate / 2 : 0;
            var raw = new float[length + fade];
            synth(raw, new System.Random(StableHash(name)));

            var samples = new float[length];
            Array.Copy(raw, samples, length);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                samples[i] = raw[i] * k + raw[length + i] * (1f - k); // tail flows into head: seamless wrap
            }
            Normalise(samples, loop ? 0.55f : 0.85f);

            Directory.CreateDirectory(folder);
            WriteWav(path, samples);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void Normalise(float[] s, float peak)
        {
            float max = 1e-6f;
            foreach (float v in s)
            {
                max = Mathf.Max(max, Mathf.Abs(v));
            }
            float k = peak / max;
            for (int i = 0; i < s.Length; i++)
            {
                s[i] *= k;
            }
        }

        private static void WriteWav(string assetPath, float[] samples)
        {
            string full = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, assetPath);
            using var stream = new FileStream(full, FileMode.Create);
            using var w = new BinaryWriter(stream);
            int dataBytes = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataBytes);
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);      // PCM
            w.Write((short)1);      // mono
            w.Write(Rate);
            w.Write(Rate * 2);      // byte rate
            w.Write((short)2);      // block align
            w.Write((short)16);     // bits
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataBytes);
            foreach (float v in samples)
            {
                w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), short.MinValue, short.MaxValue));
            }
        }
    }
}
