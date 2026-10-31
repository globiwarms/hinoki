// Hinoki — action engine: runs whatever a petal is configured to do
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
                case "app": LaunchApp(p); break;
                case "volume_mute": TapKey(0xAD); break;            // VK_VOLUME_MUTE
                case "mic_toggle":
                {
                    bool muted = Audio.IsMicMuted();
                    Audio.SetMicMuted(!muted);
                    if (!muted) Report("Hinoki", "Микрофон выключен");
                    break;
                }
                case "volume_down":
                {
                    double v = Audio.GetSystemVolume();
                    if (v >= 0) Audio.SetSystemVolume(v - 0.25);
                    break;
                }
                case "volume_up":
                {
                    double v = Audio.GetSystemVolume();
                    if (v >= 0) Audio.SetSystemVolume(v + 0.25);
                    break;
                }
                case "screenshot": ScreenshotToClipboard(); break;
                case "media": TapKey(0xB3); break;                  // VK_MEDIA_PLAY_PAUSE
                case "open_folder": OpenPath(p); break;
                case "run_cmd": RunCmd(p); break;
                case "power_menu": PowerMenuDialog(); break;
                case "show_desktop": PressCombo("Win+D"); break;
                case "url": OpenUrl(p); break;
            }
        }

        static void Report(string title, string text)
        {
            if (App.Config != null && !App.Config.ShowNotifications) return;
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d != null)
                d.BeginInvoke(new Action(delegate { Toast.Show(title, text); }));
        }

        static void RunOnUi(Action a)
        {
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d != null) d.BeginInvoke(a);
            else a();
        }

        static void Shell(string exe, string args)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = exe;
            psi.Arguments = args;
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        static void LaunchApp(PetalConfig p)
        {
            if (string.IsNullOrWhiteSpace(p.Target))
            {
                Report("Hinoki", "Лепесток «" + p.Label + "» не настроен — укажите приложение.");
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
            if (string.IsNullOrWhiteSpace(p.Target))
            {
                Report("Hinoki", "Лепесток «" + p.Label + "» не настроен — укажите ссылку.");
                return;
            }
            string url = p.Target.Trim();
            if (!url.Contains("://")) url = "https://" + url;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public static void OpenLink(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }

        static void OpenPath(PetalConfig p)
        {
            if (string.IsNullOrWhiteSpace(p.Target))
            {
                Report("Hinoki", "Лепесток «" + p.Label + "» не настроен — укажите путь к папке.");
                return;
            }
            string path = Environment.ExpandEnvironmentVariables(p.Target.Trim());
            if (!System.IO.Directory.Exists(path) && !System.IO.File.Exists(path))
            {
                Report("Hinoki", "Путь не найден: " + path);
                return;
            }
            OpenLink(path);
        }

        static void RunCmd(PetalConfig p)
        {
            if (string.IsNullOrWhiteSpace(p.Target))
            {
                Report("Hinoki", "Лепесток «" + p.Label + "» не настроен — укажите команду.");
                return;
            }
            string cmd = Environment.ExpandEnvironmentVariables(p.Target.Trim());
            var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            Process.Start(psi);
        }

        static void ScreenshotToClipboard()
        {
            try
            {
                var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
                using (var bmp = new System.Drawing.Bitmap(bounds.Width, bounds.Height))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bmp.Size);
                    Sounds.Play("shutter");
                    RunOnUi(delegate
                    {
                        try
                        {
                            var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                bmp.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty,
                                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                            source.Freeze();
                            System.Windows.Clipboard.SetImage(source);
                        }
                        catch { }
                    });
                    Toast.Show("Скриншот в буфере", "Вставьте через Ctrl+V", "camera");
                }
            }
            catch (Exception ex) { Report("Hinoki", "Скриншот не получился: " + ex.Message); }
        }

        static void PowerMenuDialog()
        {
            RunOnUi(delegate
            {
                string choice = PowerMenu.Show();
                if (choice == "sleep") Native.SetSuspendState(false, false, false);
                else if (choice == "restart") Shell("shutdown", "/r /t 0");
                else if (choice == "shutdown") Shell("shutdown", "/s /t 0");
            });
        }

        /// <summary>Power actions run on a worker thread — marshal the confirm dialog to the UI thread.</summary>
        static void PowerAction(string icon, string title, string confirmLabel, string question, string verb, Action act)
        {
            bool confirm = App.Config == null || App.Config.ConfirmPower;
            bool approved = true;
            if (confirm)
            {
                var d = Application.Current != null ? Application.Current.Dispatcher : null;
                if (d != null)
                    d.Invoke(new Action(delegate
                    {
                        Sounds.Play("confirm");
                        approved = GlassConfirm.Show(title, question, verb, icon, true);
                    }));
            }
            if (approved) act();
            else Toast.Show("Отменено", "Действие отменено", "info");
        }

        static void MonitorOff()
        {
            Native.SendMessageW(new IntPtr(0xFFFF), 0x0112, new IntPtr(0xF170), new IntPtr(2));
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
                if (short.TryParse(s.Substring(1), out n) && n >= 1 && n <= 24) return (ushort)(0x6F + n);
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
    }
}
