// Hinoki — action engine: runs whatever a petal is configured to do
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Hinoki
{
    public static class ActionEngine
    {
        public static void Run(PetalConfig p)
        {
            if (p == null || p.Action == "none") return;
            Task.Factory.StartNew(delegate { SafeRun(p); });
        }

        static void SafeRun(PetalConfig p)
        {
            try { Execute(p); }
            catch (Exception ex) { Report("Hinoki", "Не получилось: " + ex.Message); }
        }

        static void Execute(PetalConfig p)
        {
            switch (p.Action)
            {
                case "discord_mute": SendToDiscord(App.Config.DiscordMuteKeys, "Мут микрофона"); break;
                case "discord_deafen": SendToDiscord(App.Config.DiscordDeafenKeys, "Мут звука"); break;
                case "volume_mute": TapKey(0xAD); break;            // VK_VOLUME_MUTE
                case "media": TapKey(0xB3); break;                  // VK_MEDIA_PLAY_PAUSE
                case "app": LaunchApp(p); break;
                case "url": OpenUrl(p); break;
                case "hotkey": PressCombo(p.Target); break;
            }
        }

        // ---- launchers ----

        public static void OpenLink(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }

        static void LaunchApp(PetalConfig p)
        {
            if (string.IsNullOrWhiteSpace(p.Target))
            {
                Report("Hinoki", "Лепесток «" + p.Label + "» не настроен — укажите приложение в настройках.");
                return;
            }
            var psi = new ProcessStartInfo();
            psi.FileName = Environment.ExpandEnvironmentVariables(p.Target.Trim());
            if (!string.IsNullOrWhiteSpace(p.Args)) psi.Arguments = Environment.ExpandEnvironmentVariables(p.Args);
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        static void OpenUrl(PetalConfig p)
        {
            if (string.IsNullOrWhiteSpace(p.Target)) { Report("Hinoki", "Лепесток «" + p.Label + "» не настроен — укажите ссылку."); return; }
            string url = p.Target.Trim();
            if (!url.Contains("://")) url = "https://" + url;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        // ---- discord ----

        static IntPtr FindDiscordWindow()
        {
            IntPtr found = IntPtr.Zero;
            foreach (Process proc in Process.GetProcessesByName("Discord"))
            {
                if (proc.MainWindowHandle != IntPtr.Zero) { found = proc.MainWindowHandle; proc.Dispose(); break; }
                proc.Dispose();
            }
            if (found == IntPtr.Zero)
            {
                foreach (Process proc in Process.GetProcessesByName("DiscordCanary"))
                {
                    if (proc.MainWindowHandle != IntPtr.Zero) { found = proc.MainWindowHandle; proc.Dispose(); break; }
                    proc.Dispose();
                }
            }
            if (found != IntPtr.Zero) return found;
            // fallback: any visible window whose title mentions Discord
            Native.EnumWindows(delegate(IntPtr hwnd, IntPtr lp)
            {
                if (!Native.IsWindowVisible(hwnd)) return true;
                var sb = new StringBuilder(256);
                Native.GetWindowText(hwnd, sb, 256);
                string t = sb.ToString();
                if (t.Contains("Discord") || t.Contains(" discord ")) { found = hwnd; return false; }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        static void SendToDiscord(string combo, string what)
        {
            IntPtr hwnd = FindDiscordWindow();
            if (hwnd == IntPtr.Zero)
            {
                Report("Hinoki", "Discord не запущен.");
                return;
            }
            IntPtr prev = Native.GetForegroundWindow();
            Native.ForceForeground(hwnd);
            System.Threading.Thread.Sleep(160);
            PressCombo(combo);
            System.Threading.Thread.Sleep(90);
            if (prev != IntPtr.Zero && prev != hwnd) Native.SetForegroundWindow(prev);
        }

        // ---- keystrokes ----

        public static void TapKey(ushort vk)
        {
            var input = new Native.INPUT[2];
            input[0].type = Native.INPUT_KEYBOARD;
            input[0].u.ki.wVk = vk;
            input[1].type = Native.INPUT_KEYBOARD;
            input[1].u.ki.wVk = vk;
            input[1].u.ki.dwFlags = Native.KEYEVENTF_KEYUP;
            Native.SendInput(2, input, Marshal.SizeOf(typeof(Native.INPUT)));
        }

        /// <summary>Parse "Ctrl+Shift+M" style combos and press them.</summary>
        public static void PressCombo(string combo)
        {
            if (string.IsNullOrWhiteSpace(combo)) return;
            ushort mods = 0;
            ushort main = 0;
            string[] parts = combo.Split('+');
            for (int i = 0; i < parts.Length; i++)
            {
                string s = parts[i].Trim().ToLowerInvariant();
                if (s == "ctrl" || s == "control") mods |= 0x11;
                else if (s == "shift") mods |= 0x10;
                else if (s == "alt") mods |= 0x12;
                else if (s == "win" || s == "meta") mods |= 0x5B;
                else main = VkFromName(s);
            }
            if (main == 0 && mods == 0) return;

            var seq = new System.Collections.Generic.List<ushort>();
            // modifiers down
            ushort[] modKeys = { 0x11, 0x10, 0x12, 0x5B };
            foreach (ushort mk in modKeys)
                if ((mods & mk) != 0) seq.Add(mk);
            if (main != 0) seq.Add(main);
            var inputs = new Native.INPUT[seq.Count * 2];
            for (int i = 0; i < seq.Count; i++)
            {
                inputs[i].type = Native.INPUT_KEYBOARD;
                inputs[i].u.ki.wVk = seq[i];
            }
            for (int i = 0; i < seq.Count; i++)
            {
                int j = inputs.Length - 1 - i;
                inputs[j].type = Native.INPUT_KEYBOARD;
                inputs[j].u.ki.wVk = seq[i];
                inputs[j].u.ki.dwFlags = Native.KEYEVENTF_KEYUP;
            }
            Native.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Native.INPUT)));
        }

        static ushort VkFromName(string s)
        {
            if (s.Length == 1)
            {
                char c = char.ToUpperInvariant(s[0]);
                if (c >= '0' && c <= '9') return (ushort)c;
                if (c >= 'A' && c <= 'Z') return (ushort)c;
            }
            if (s.StartsWith("f", StringComparison.Ordinal) && s.Length <= 3)
            {
                short n;
                if (short.TryParse(s.Substring(1), out n) && n >= 1 && n <= 24) return (ushort)(0x6F + n); // F1=0x70
            }
            switch (s)
            {
                case "space": return 0x20;
                case "enter": return 0x0D;
                case "tab": return 0x09;
                case "esc": return 0x1B;
                case "up": return 0x26;
                case "down": return 0x28;
                case "left": return 0x25;
                case "right": return 0x27;
                case "volumeup": return 0xAF;
                case "volumedown": return 0xAE;
                case "volumemute": return 0xAD;
                case "next": case "medianext": return 0xB0;
                case "prev": case "mediaprev": return 0xB1;
                case "stop": return 0xB2;
                case "playpause": return 0xB3;
            }
            return 0;
        }

        static void Report(string title, string text)
        {
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d != null)
                d.BeginInvoke(new Action(delegate { Tray.Balloon(title, text); }));
        }
    }
}
