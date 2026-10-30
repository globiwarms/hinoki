// Hinoki — tiny in-memory sound synthesizer: soft glass "pops" with no external files
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Hinoki
{
    internal static class Sounds
    {
        private const int SND_MEMORY = 0x4;
        private const int SND_ASYNC = 0x1;
        private const int SND_NODEFAULT = 0x2;

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern bool PlaySound(byte[] pszSound, IntPtr hmod, int fdwSound);

        private static readonly Dictionary<string, byte[]> Cache = new Dictionary<string, byte[]>();

        public static void Play(string name)
        {
            var cfg = App.Config;
            if (cfg == null || !cfg.SoundEnabled) return;

            bool allow = true;
            if (name == "open") allow = cfg.SoundOpen;
            if (name == "close") allow = cfg.SoundClose;
            if (name == "click") allow = cfg.SoundClick;
            if (!allow) return;

            try
            {
                if (!Cache.ContainsKey(name))
                    Cache[name] = Render(name);
                PlaySound(Cache[name], IntPtr.Zero, SND_MEMORY | SND_ASYNC | SND_NODEFAULT);
            }
            catch { }
        }

        // ---------- synthesis ----------

        private static byte[] Render(string name)
        {
            const int rate = 44100;
            double vol = (App.Config != null ? App.Config.SoundVolume : 55) / 100.0;
            vol = 0.12 + 0.75 * vol;

            List<double[]> tones;
            switch (name)
            {
                case "click":
                    tones = new List<double[]> { new double[] { 2093, 0.05, 0.55 }, new double[] { 3136, 0.035, 0.3 } };
                    break;
                case "close":
                    tones = new List<double[]> { new double[] { 659, 0.11, 0.5 }, new double[] { 494, 0.10, 0.4 } };
                    break;
                case "error":
                    tones = new List<double[]> { new double[] { 220, 0.16, 0.55 } };
                    break;
                default: // open — soft two-note bloom
                    tones = new List<double[]> { new double[] { 784, 0.10, 0.45 }, new double[] { 1175, 0.14, 0.5 } };
                    break;
            }

            double total = 0;
            foreach (var t in tones) total = Math.Max(total, t[1] + 0.06);
            int n = (int)(rate * total);
            double[] mix = new double[n];

            double offset = 0;
            foreach (var t in tones)
            {
                double freq = t[0], dur = t[1], amp = t[2];
                int len = (int)(rate * dur);
                for (int i = 0; i < len; i++)
                {
                    double p = (double)i / len;                       // 0..1
                    double env = Math.Sin(Math.Min(p * 6.0, 1.0) * Math.PI / 2); // fast attack
                    env *= Math.Exp(-p * 5.2);                        // exponential decay
                    double s = Math.Sin(2 * Math.PI * freq * i / rate);
                    s += 0.28 * Math.Sin(2 * Math.PI * freq * 2 * i / rate);   // a bit of 2nd harmonic
                    int idx = i + (int)(offset * rate);
                    if (idx < n) mix[idx] += amp * env * s;
                }
                offset += dur * 0.55;
            }

            // normalize softly
            double peak = 0;
            foreach (double v in mix) peak = Math.Max(peak, Math.Abs(v));
            double k = peak > 0 ? vol / peak : 0;

            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms);
                int dataLen = n * 2;
                w.Write(0x46464952);          // RIFF
                w.Write(36 + dataLen);
                w.Write(0x45564157);          // WAVE
                w.Write(0x20746D66);          // fmt
                w.Write(16);
                w.Write((short)1);            // PCM
                w.Write((short)1);            // mono
                w.Write(rate);
                w.Write(rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(0x61746164);          // data
                w.Write(dataLen);
                for (int i = 0; i < n; i++)
                {
                    double v = Math.Max(-1, Math.Min(1, mix[i] * k));
                    // gentle tanh for warmth
                    v = Math.Tanh(v * 1.4) / Math.Tanh(1.4);
                    w.Write((short)(v * 32760));
                }
                w.Flush();
                return ms.ToArray();
            }
        }
    }
}
