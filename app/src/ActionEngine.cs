// Hinoki — action engine: runs whatever a petal is configured to do
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
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
                case "mic_toggle":
                {
                    bool muted = Audio.IsMicMuted();
                    Audio.SetMicMuted(!muted);
                    if (!muted) Report("Hinoki", "Микрофон выключен");
                    break;
                }
                case "volume_mute": TapKey(0xAD); break;            // VK_VOLUME_MUTE
                case "volume_up": TapKey(0xAF); break;
                case "volume_down": TapKey(0xAE); break;
                case "media": TapKey(0xB3); break;                  // VK_MEDIA_PLAY_PAUSE
                case "media_next": TapKey(0xB0); break;
                case "media_prev": TapKey(0xB1); break;
                case "app": LaunchApp(p); break;
                case "url": OpenUrl(p); break;
                case "hotkey": PressCombo(p.Target); break;
                case "screenshot_file": ScreenshotToFile(false); break;
                case "screenshot_window": ScreenshotToFile(true); break;
                case "screenshot_buffer": PressCombo("Win+Shift+S"); break;
                case "show_desktop": PressCombo("Win+D"); break;
                case "minimize_window": MinimizeActive(); break;
                case "topmost_window": ToggleTopmost(); break;
                case "lock_pc": Native.LockWorkStation(); break;
                case "sleep_pc": Native.SetSuspendState(false, false, false); break;
                case "restart_pc": Shell("shutdown", "/r /t 0"); break;
                case "shutdown_pc": Shell("shutdown", "/s /t 0"); break;
                case "task_manager": Shell("taskmgr", ""); break;
                case "windows_settings": OpenLink("ms-settings:"); break;
                case "calculator": Shell("calc", ""); break;
                case "emoji_panel": PressCombo("Win+."); break;
                case "paste_clipboard": PressCombo("Ctrl+V"); break;
                case "copy": PressCombo("Ctrl+C"); break;
                case "monitor_off": MonitorOff(); break;
                case "clear_recycle_bin": Shell("powershell.exe", "-NoProfile -Command Clear-RecycleBin -Force -ErrorAction SilentlyContinue"); break;
                case "open_discord": OpenLink("discord://"); break;
                case "open_spotify": OpenLink("spotify:"); break;
                case "search_start": PressCombo("Win+S"); break;
                case "notification_center": PressCombo("Win+N"); break;
                case "open_steam": OpenLink("steam://open/games"); break;
                case "open_config": OpenLink(AppConfig.Dir); break;
                case "open_downloads": Shell("explorer.exe", "shell:Downloads"); break;
                case "open_desktop": OpenLink(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); break;
                case "open_documents": OpenLink(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); break;
                case "open_settings": RunOnUi(delegate { App.OpenSettings(); }); break;
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

        // ---- extra actions ----

        static void Shell(string exe, string args)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = exe;
            psi.Arguments = args;
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        static void RunOnUi(Action a)
        {
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d != null) d.BeginInvoke(a);
            else a();
        }

        static void ScreenshotToFile(bool activeWindowOnly)
        {
            try
            {
                int x, y, w, h;
                if (activeWindowOnly)
                {
                    IntPtr fg = Native.GetForegroundWindow();
                    if (fg == IntPtr.Zero) { Report("Hinoki", "Нет активного окна."); return; }
                    Native.RECT r;
                    if (!Native.GetWindowRect(fg, out r)) { Report("Hinoki", "Не удалось измерить окно."); return; }
                    x = r.Left; y = r.Top; w = r.Right - r.Left; h = r.Bottom - r.Top;
                }
                else
                {
                    var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
                    x = bounds.X; y = bounds.Y; w = bounds.Width; h = bounds.Height;
                }
                if (w <= 0 || h <= 0) { Report("Hinoki", "Пустая область для снимка."); return; }
                using (var bmp = new System.Drawing.Bitmap(w, h))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                        g.CopyFromScreen(x, y, 0, 0, bmp.Size);
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    string file = System.IO.Path.Combine(dir, "Hinoki_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                    Report("Hinoki", "Скриншот сохранён: " + file);
                    if (App.Config != null && App.Config.OpenShotFolder)
                    {
                        var psi = new ProcessStartInfo("explorer.exe", "/select,\"" + file + "\"");
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                }
            }
            catch (Exception ex) { Report("Hinoki", "Скриншот не получился: " + ex.Message); }
        }

        static void MonitorOff()
        {
            Native.SendMessageW(new IntPtr(0xFFFF), 0x0112, new IntPtr(0xF170), new IntPtr(2));
        }

        static void MinimizeActive()
        {
            IntPtr fg = Native.GetForegroundWindow();
            if (fg != IntPtr.Zero) Native.ShowWindow(fg, 6); // SW_MINIMIZE
        }

        static void ToggleTopmost()
        {
            IntPtr fg = Native.GetForegroundWindow();
            if (fg == IntPtr.Zero) return;
            var now = new System.Collections.Generic.List<byte[]>();
            uint style = GetWindowLong(fg, -20); // GWL_EXSTYLE
            bool isTopmost = (style & 0x8) != 0; // WS_EX_TOPMOST
            Native.SetWindowPos(fg, isTopmost ? Native.HWND_NOTOPMOST : Native.HWND_TOPMOST,
                0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

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

        /// <summary>Parse "Ctrl+Shift+M" into Win32 RegisterHotKey parts.</summary>
        public static bool ParseHotkeyParts(string combo, out uint mods, out uint vk)
        {
            mods = 0; vk = 0;
            if (string.IsNullOrWhiteSpace(combo)) return false;
            string[] parts = combo.Split('+');
            foreach (string raw in parts)
            {
                string t = raw.Trim().ToLowerInvariant();
                if (t == "ctrl" || t == "control") mods |= 0x2;
                else if (t == "shift") mods |= 0x4;
                else if (t == "alt") mods |= 0x1;
                else if (t == "win" || t == "meta") mods |= 0x8;
                else vk = VkFromName(t);
            }
            return mods != 0 && vk != 0;
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

        public static ushort VkFromName(string s)
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
            if (App.Config != null && !App.Config.ShowNotifications) return;
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d != null)
                d.BeginInvoke(new Action(delegate { Tray.Balloon(title, text); }));
        }
    }
}
