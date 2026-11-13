using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    public enum Sfx
    {
        Click = 0, Coin = 1, Cook = 2, Chop = 3, Bell = 4, Serve = 5,
        Pay = 6, Error = 7, LevelUp = 8, Clean = 9, Spoil = 10, Trash = 11, Bubble = 12
    }

    /// <summary>
    /// Semua suara dibuat secara prosedural (sintesis) saat runtime, jadi project
    /// tidak bergantung pada file audio eksternal dan tetap 100% legal dipakai.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int SampleRate = 22050;

        AudioSource musicSrc;
        AudioSource ambienceSrc;
        AudioSource rainSrc;
        AudioSource sfxSrc;

        Dictionary<Sfx, AudioClip> sfx = new Dictionary<Sfx, AudioClip>();
        AudioClip musicClip;
        AudioClip ambienceClip;
        AudioClip rainClip;

        SettingsSave settings;
        int musicDensity = 0;

        public void Init()
        {
            // --- sumber audio ---
            musicSrc = gameObject.AddComponent<AudioSource>();
            musicSrc.loop = true; musicSrc.playOnAwake = false; musicSrc.spatialBlend = 0f;

            ambienceSrc = gameObject.AddComponent<AudioSource>();
            ambienceSrc.loop = true; ambienceSrc.playOnAwake = false; ambienceSrc.spatialBlend = 0f;

            rainSrc = gameObject.AddComponent<AudioSource>();
            rainSrc.loop = true; rainSrc.playOnAwake = false; rainSrc.spatialBlend = 0f;

            sfxSrc = gameObject.AddComponent<AudioSource>();
            sfxSrc.loop = false; sfxSrc.playOnAwake = false; sfxSrc.spatialBlend = 0f;

            BuildSfx();
            musicClip = BuildMusic();
            ambienceClip = BuildAmbience();
            rainClip = BuildRain();

            musicSrc.clip = musicClip;
            ambienceSrc.clip = ambienceClip;
            rainSrc.clip = rainClip;

            settings = new SettingsSave();
            ApplySettings(settings);
            musicSrc.Play();
            ambienceSrc.Play();
        }

        public void ApplySettings(SettingsSave s)
        {
            if (s == null) return;
            settings = s;
            if (musicSrc != null) musicSrc.volume = 0.30f * Mathf.Clamp01(s.musicVolume);
            if (ambienceSrc != null) ambienceSrc.volume = 0.22f * Mathf.Clamp01(s.musicVolume);
            if (rainSrc != null) rainSrc.volume = 0.30f * Mathf.Clamp01(s.musicVolume);
            if (sfxSrc != null) sfxSrc.volume = Mathf.Clamp01(s.sfxVolume);
        }

        public void PlaySfx(Sfx kind, float volumeScale = 1f)
        {
            if (sfxSrc == null) return;
            AudioClip clip;
            if (sfx.TryGetValue(kind, out clip) && clip != null)
                sfxSrc.PlayOneShot(clip, Mathf.Clamp01(settings.sfxVolume * volumeScale));
        }

        public void PlayAmbient(Weather weather)
        {
            if (rainSrc == null) return;
            if (weather == Weather.Hujan) { if (!rainSrc.isPlaying) rainSrc.Play(); }
            else { if (rainSrc.isPlaying) rainSrc.Stop(); }
        }

        /// <summary>Musik jadi lebih ramai saat warteg ramai (opsional, dipanggil HUD).</summary>
        public void SetIntensity(int customerCount)
        {
            if (musicSrc == null) return;
            float target = Mathf.Lerp(0.26f, 0.42f, Mathf.Clamp01(customerCount / 6f));
            musicSrc.volume = target * Mathf.Clamp01(settings.musicVolume) / Mathf.Max(0.01f, 0.30f) * 0.30f / 0.30f;
            musicSrc.volume = target * Mathf.Clamp01(settings.musicVolume);
        }

        // =====================================================================
        //  SINTESIS
        // =====================================================================
        static float[] Buffer(float seconds) { return new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate))]; }

        static AudioClip MakeClip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void AddTone(float[] buf, float startSec, float durSec, float freq, float amp, int wave, float attack = 0.01f)
        {
            int start = Mathf.RoundToInt(startSec * SampleRate);
            int len = Mathf.RoundToInt(durSec * SampleRate);
            int attackSamples = Mathf.Max(1, Mathf.RoundToInt(attack * SampleRate));
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                if (idx < 0 || idx >= buf.Length) continue;
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, i / (float)attackSamples);
                float decay = 1f - (i / (float)len);
                env *= decay * decay;
                float phase = 2f * Mathf.PI * freq * t;
                float s;
                switch (wave)
                {
                    case 1: s = Mathf.Sign(Mathf.Sin(phase)); break;                     // square
                    case 2: s = 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(0.5f + phase / (2f * Mathf.PI))); break; // saw
                    case 3: s = Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 2f) * 0.25f + Mathf.Sin(phase * 3f) * 0.15f; break; // organ
                    default: s = Mathf.Sin(phase); break;
                }
                buf[idx] += s * amp * env;
            }
        }

        static void AddNoise(float[] buf, float startSec, float durSec, float amp, float lowpass, System.Random rng)
        {
            int start = Mathf.RoundToInt(startSec * SampleRate);
            int len = Mathf.RoundToInt(durSec * SampleRate);
            float prev = 0f;
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                if (idx < 0 || idx >= buf.Length) continue;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(prev, white, lowpass);
                float env = 1f - (i / (float)len);
                buf[idx] += prev * amp * env;
            }
        }

        void BuildSfx()
        {
            System.Random rng = new System.Random(4242);

            // klik UI
            float[] b = Buffer(0.06f);
            AddTone(b, 0f, 0.05f, 880f, 0.25f, 0, 0.002f);
            sfx[Sfx.Click] = MakeClip("click", b);

            // koin / uang masuk
            b = Buffer(0.34f);
            AddTone(b, 0f, 0.09f, 1320f, 0.30f, 0);
            AddTone(b, 0.07f, 0.12f, 1760f, 0.28f, 0);
            AddTone(b, 0.15f, 0.18f, 2200f, 0.22f, 0);
            sfx[Sfx.Coin] = MakeClip("coin", b);

            // suara masak (minyak mendesis)
            b = Buffer(0.9f);
            AddNoise(b, 0f, 0.9f, 0.20f, 0.22f, rng);
            sfx[Sfx.Cook] = MakeClip("cook", b);

            // memotong / menyiapkan
            b = Buffer(0.18f);
            AddNoise(b, 0f, 0.10f, 0.35f, 0.85f, rng);
            AddTone(b, 0.02f, 0.10f, 220f, 0.10f, 2, 0.002f);
            sfx[Sfx.Chop] = MakeClip("chop", b);

            // bel pelanggan datang
            b = Buffer(0.7f);
            AddTone(b, 0f, 0.7f, 1046f, 0.16f, 0);
            AddTone(b, 0f, 0.7f, 1568f, 0.12f, 0);
            AddTone(b, 0.28f, 0.5f, 1318f, 0.10f, 0);
            sfx[Sfx.Bell] = MakeClip("bell", b);

            // menyajikan makanan
            b = Buffer(0.25f);
            AddTone(b, 0f, 0.12f, 660f, 0.18f, 3);
            AddTone(b, 0.10f, 0.14f, 990f, 0.14f, 3);
            sfx[Sfx.Serve] = MakeClip("serve", b);

            // pembayaran (kasir)
            b = Buffer(0.45f);
            AddTone(b, 0f, 0.10f, 520f, 0.22f, 1, 0.003f);
            AddTone(b, 0.09f, 0.30f, 1040f, 0.18f, 0);
            AddNoise(b, 0.20f, 0.18f, 0.10f, 0.5f, rng);
            sfx[Sfx.Pay] = MakeClip("pay", b);

            // error / tidak bisa
            b = Buffer(0.24f);
            AddTone(b, 0f, 0.10f, 300f, 0.22f, 1);
            AddTone(b, 0.11f, 0.12f, 220f, 0.20f, 1);
            sfx[Sfx.Error] = MakeClip("error", b);

            // naik level
            b = Buffer(1.1f);
            float[] notes = { 523f, 659f, 784f, 1046f };
            for (int i = 0; i < notes.Length; i++) AddTone(b, i * 0.13f, 0.45f, notes[i], 0.18f, 3);
            sfx[Sfx.LevelUp] = MakeClip("levelup", b);

            // bersih-bersih (gesekan sapu)
            b = Buffer(0.5f);
            AddNoise(b, 0f, 0.5f, 0.20f, 0.35f, rng);
            sfx[Sfx.Clean] = MakeClip("clean", b);

            // makanan basi
            b = Buffer(0.4f);
            AddTone(b, 0f, 0.35f, 180f, 0.20f, 2);
            sfx[Sfx.Spoil] = MakeClip("spoil", b);

            // buang sampah
            b = Buffer(0.3f);
            AddNoise(b, 0f, 0.25f, 0.25f, 0.4f, rng);
            sfx[Sfx.Trash] = MakeClip("trash", b);

            // bubble pesanan baru
            b = Buffer(0.18f);
            AddTone(b, 0f, 0.10f, 1200f, 0.16f, 0, 0.004f);
            AddTone(b, 0.06f, 0.10f, 1500f, 0.13f, 0, 0.004f);
            sfx[Sfx.Bubble] = MakeClip("bubble", b);
        }

        AudioClip BuildMusic()
        {
            // Musik latar santai: tangga nada pentatonik, tempo lambat, loop 16 detik.
            float dur = 16f;
            float[] buf = Buffer(dur);
            float beat = 0.8f;
            int[] scale = { 0, 2, 4, 7, 9, 12, 14, 16 };       // pentatonik
            float root = 261.63f;                              // C4
            System.Random rng = new System.Random(777);

            // bass lembut tiap 2 beat
            for (int i = 0; i < 10; i++)
            {
                float f = root / 4f * Mathf.Pow(2f, scale[(i * 3) % 4] / 12f);
                AddTone(buf, i * beat * 2f, beat * 1.9f, f, 0.13f, 0, 0.05f);
            }

            // melodi utama
            int step = 0;
            for (float t = 0f; t < dur - 1f; t += beat)
            {
                if (rng.NextDouble() < 0.72)
                {
                    int note = scale[(step * 3 + rng.Next(0, 3)) % scale.Length];
                    float f = root * Mathf.Pow(2f, note / 12f);
                    AddTone(buf, t, beat * 1.4f, f, 0.10f, 3, 0.02f);
                }
                if (rng.NextDouble() < 0.30)
                {
                    int note = scale[(step + 2) % scale.Length];
                    float f = root * Mathf.Pow(2f, (note + 12) / 12f);
                    AddTone(buf, t + beat * 0.5f, beat, f, 0.045f, 0, 0.01f);
                }
                step++;
            }

            // gema sederhana
            EchoEffect(buf, 0.22f, 0.33f);

            Normalize(buf, 0.85f);
            return MakeClip("warteg_music", buf);
        }

        AudioClip BuildAmbience()
        {
            // Suara lingkungan warteg: dengung rendah + sendok/gerabak samar
            float dur = 8f;
            float[] buf = Buffer(dur);
            System.Random rng = new System.Random(2024);

            AddNoise(buf, 0f, dur, 0.055f, 0.04f, rng);     // dengung ruangan
            AddNoise(buf, 0f, dur, 0.020f, 0.35f, rng);     // murmur percakapan samar

            for (int i = 0; i < 5; i++)
            {
                float t = (float)rng.NextDouble() * (dur - 0.3f);
                AddNoise(buf, t, 0.12f, 0.05f, 0.8f, rng);  // piring beradu
                AddTone(buf, t, 0.10f, 1600f + (float)rng.NextDouble() * 900f, 0.02f, 0);
            }
            Normalize(buf, 0.6f);
            return MakeClip("warteg_ambience", buf);
        }

        AudioClip BuildRain()
        {
            float dur = 6f;
            float[] buf = Buffer(dur);
            System.Random rng = new System.Random(99);
            AddNoise(buf, 0f, dur, 0.18f, 0.30f, rng);
            AddNoise(buf, 0f, dur, 0.06f, 0.75f, rng);
            // tetesan
            for (int i = 0; i < 40; i++)
                AddNoise(buf, (float)rng.NextDouble() * (dur - 0.1f), 0.05f, 0.05f, 0.9f, rng);
            Normalize(buf, 0.8f);
            return MakeClip("rain", buf);
        }

        static void EchoEffect(float[] buf, float delaySec, float feedback)
        {
            int delay = Mathf.RoundToInt(delaySec * SampleRate);
            for (int i = delay; i < buf.Length; i++)
                buf[i] += buf[i - delay] * feedback;
        }

        static void Normalize(float[] buf, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < buf.Length; i++) max = Mathf.Max(max, Mathf.Abs(buf[i]));
            float k = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] = Mathf.Clamp(buf[i] * k, -1f, 1f);
        }
    }
}
