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
        public static AudioClip PryScreech => Get(SfxFolder, "sfx_pry_screech", 1.4f, false, PryScreechSynth);
        public static AudioClip FlickerBuzz => Get(SfxFolder, "sfx_flicker_buzz", 0.6f, false, FlickerSynth);

        public static AudioClip Message => Get(SfxFolder, "sfx_ventnet_message", 0.35f, false, MessageSynth);
        public static AudioClip PaChime => Get(SfxFolder, "sfx_pa_chime", 1.8f, false, ChimeSynth);
        public static AudioClip PlazaCrowd => Get(AmbienceFolder, "amb_plaza_crowd", 10f, true, CrowdSynth);
        public static AudioClip PlazaMusic => Get(AmbienceFolder, "amb_plaza_music", 8f, true, MusicSynth);

        public static AudioClip DroneHum => Get(AmbienceFolder, "amb_drone_hum", 2f, true, DroneHumSynth);
        public static AudioClip DroneChirp => Get(SfxFolder, "sfx_drone_chirp", 0.45f, false, DroneChirpSynth);
        public static AudioClip DroneAlarm => Get(SfxFolder, "sfx_drone_alarm", 0.9f, false, DroneAlarmSynth);

        // ------------------------------------------------------------------ Synths (buffer, rng)

        /// <summary>Four little rotors: a whiny tone with a fast flutter, plus airy noise.</summary>
        private static void DroneHumSynth(float[] b, System.Random r)
        {
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                lp += 0.2f * (White(r) - lp);
                float flutter = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 36f * t);
                float whine = Mathf.Sin(2f * Mathf.PI * 230f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 460f * t) * 0.25f
                              + Mathf.Sin(2f * Mathf.PI * 690f * t + 0.4f) * 0.12f;
                b[i] = (whine * 0.6f + lp * 0.8f) * flutter;
            }
        }

        /// <summary>"Huh?" — two rising beeps.</summary>
        private static void DroneChirpSynth(float[] b, System.Random r)
        {
            float phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float u = t < 0.18f ? t / 0.18f : (t - 0.22f) / 0.18f;
                bool on = t < 0.18f || (t > 0.22f && t < 0.4f);
                float f = Mathf.Lerp(t < 0.2f ? 820f : 1000f, t < 0.2f ? 1180f : 1500f, Mathf.Clamp01(u));
                phase += 2f * Mathf.PI * f / Rate;
                float env = on ? Mathf.Clamp01(u * 20f) * Mathf.Clamp01((1f - u) * 12f) : 0f;
                b[i] = (Mathf.Sin(phase) + 0.25f * Mathf.Sin(phase * 2f)) * env;
            }
        }

        /// <summary>Two-tone pursuit alarm.</summary>
        private static void DroneAlarmSynth(float[] b, System.Random r)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float f = t < 0.42f ? 880f : 660f;
                float local = t < 0.42f ? t : t - 0.45f;
                bool on = t < 0.42f || (t > 0.45f && t < 0.87f);
                float env = on ? Mathf.Clamp01(local / 0.01f) * Mathf.Clamp01((0.42f - local) / 0.03f) : 0f;
                float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * 0.35f + Mathf.Sin(2f * Mathf.PI * f * t) * 0.65f;
                b[i] = square * env;
            }
        }

        /// <summary>Burner pager: two quick rising chirps.</summary>
        private static void MessageSynth(float[] b, System.Random r)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                if (t < 0.08f)
                {
                    s = Beep(t, 1320f) * Mathf.Clamp01(t / 0.004f) * Mathf.Clamp01((0.08f - t) / 0.01f);
                }
                else if (t > 0.12f && t < 0.2f)
                {
                    float u = t - 0.12f;
                    s = Beep(u, 1760f) * Mathf.Clamp01(u / 0.004f) * Mathf.Clamp01((0.08f - u) / 0.01f);
                }
                b[i] = s;
            }
        }

        private static float Beep(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 3f * f * t);

        /// <summary>Station PA "ding-dong" (two bell tones).</summary>
        private static void ChimeSynth(float[] b, System.Random r)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                b[i] = Bell(t, 659.3f) + (t > 0.55f ? Bell(t - 0.55f, 523.3f) : 0f);
            }
        }

        private static float Bell(float t, float f)
        {
            float attack = Mathf.Clamp01(t / 0.003f);
            return attack * (Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t / 0.7f)
                             + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t / 0.3f)
                             + 0.2f * Mathf.Sin(2f * Mathf.PI * f * 5.4f * t) * Mathf.Exp(-t / 0.12f));
        }

        /// <summary>
        /// Muffled crowd walla: ten "voices", each a buzzy source pushed through two formant resonators and chopped
        /// into syllables with pauses, then low-passed as if heard through a wall.
        /// </summary>
        private static void CrowdSynth(float[] b, System.Random r)
        {
            const int voices = 10;
            var f0 = new float[voices];
            var gain = new float[voices];
            var sylStart = new float[voices];
            var sylLen = new float[voices];
            var silent = new bool[voices];
            var w1 = new float[voices];
            var w2 = new float[voices];
            var y = new float[voices, 4]; // two resonators × two states
            var phase = new float[voices];
            for (int v = 0; v < voices; v++)
            {
                f0[v] = 95f + (float)r.NextDouble() * 140f;
                gain[v] = 0.4f + (float)r.NextDouble() * 0.6f;
                sylStart[v] = -(float)r.NextDouble();
                sylLen[v] = 0.01f;
            }

            const float rad = 0.985f;
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float mix = 0f;
                for (int v = 0; v < voices; v++)
                {
                    if (t - sylStart[v] >= sylLen[v])
                    {
                        sylStart[v] = t;
                        silent[v] = r.NextDouble() < 0.22;
                        sylLen[v] = silent[v] ? 0.2f + (float)r.NextDouble() * 0.5f : 0.08f + (float)r.NextDouble() * 0.16f;
                        w1[v] = 2f * Mathf.PI * (300f + (float)r.NextDouble() * 500f) / Rate;
                        w2[v] = 2f * Mathf.PI * (900f + (float)r.NextDouble() * 1400f) / Rate;
                    }
                    if (silent[v])
                    {
                        continue;
                    }
                    phase[v] += f0[v] / Rate;
                    phase[v] -= Mathf.Floor(phase[v]);
                    float excitation = (2f * phase[v] - 1f) * 0.6f + White(r) * 0.35f;
                    float r1 = (1f - rad) * excitation + 2f * rad * Mathf.Cos(w1[v]) * y[v, 0] - rad * rad * y[v, 1];
                    y[v, 1] = y[v, 0];
                    y[v, 0] = r1;
                    float r2 = (1f - rad) * excitation + 2f * rad * Mathf.Cos(w2[v]) * y[v, 2] - rad * rad * y[v, 3];
                    y[v, 3] = y[v, 2];
                    y[v, 2] = r2;
                    float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - sylStart[v]) / sylLen[v]));
                    mix += (r1 + r2 * 0.6f) * env * gain[v];
                }
                lp += 0.18f * (mix - lp);
                b[i] = lp;
            }
        }

        /// <summary>Lounge-y loop bleeding out of a stall: Am7–Fmaj7–Cmaj7–G6 pads, a soft bass and hats, 120 bpm, 4 bars.</summary>
        private static void MusicSynth(float[] b, System.Random r)
        {
            int[][] chords = { new[] { 57, 60, 64, 67 }, new[] { 53, 57, 60, 64 }, new[] { 48, 52, 55, 59 }, new[] { 55, 59, 62, 64 } };
            int[] bass = { 45, 41, 36, 43 };
            const float bar = 2f, beat = 0.5f;
            float hp = 0f, lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                int bi = Mathf.Min(3, (int)(t / bar));
                float tb = t - bi * bar;
                float padEnv = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * tb / bar);
                float pad = 0f;
                foreach (int n in chords[bi])
                {
                    float f = Midi(n);
                    pad += Mathf.Sin(2f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * 2f * f * t + 0.5f);
                }
                pad *= padEnv * 0.18f;

                float tBeat = tb % (2f * beat); // bass on beats 1 and 3
                float bassNote = Mathf.Sin(2f * Mathf.PI * Midi(bass[bi]) * t) * Mathf.Exp(-tBeat / 0.35f) * 0.5f;

                float tOff = (t + beat * 0.5f) % beat; // hats on the off-beats
                float w = White(r);
                hp += 0.3f * (w - hp);
                float hat = (w - hp) * Mathf.Exp(-tOff / 0.02f) * 0.12f;

                float mix = pad + bassNote + hat;
                lp += 0.22f * (mix - lp); // heard from across the plaza
                b[i] = lp;
            }
        }

        private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

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

        /// <summary>Metal grinding against rusted metal (rising, wobbling squeal over grit), ending in a hard clank as it gives.</summary>
        private static void PryScreechSynth(float[] b, System.Random r)
        {
            float lp = 0f, phase = 0f;
            float end = b.Length / (float)Rate;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                lp += 0.15f * (White(r) - lp);
                float f = 520f + 380f * (t / end) + 60f * Mathf.Sin(2f * Mathf.PI * 7f * t);
                phase += 2f * Mathf.PI * f / Rate;
                float squeal = (Mathf.Sin(phase) + 0.4f * Mathf.Sin(phase * 2.01f)) * 0.35f;
                float grind = lp * 0.9f * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 23f * t));
                float body = Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((end - 0.25f - t) / 0.1f);
                float release = t > end - 0.25f ? Mathf.Exp(-(t - (end - 0.25f)) / 0.05f) : 0f;
                float clank = release * (Mathf.Sin(2f * Mathf.PI * 760f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 95f * t));
                b[i] = (squeal + grind) * body + clank;
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
