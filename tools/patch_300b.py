"""v3.0.0 patch B (rewritten): new action switch, helpers."""
import io

p = "app/src/ActionEngine.cs"
s = io.open(p, encoding="utf-8").read()

start = s.index("            switch (p.Action)\n            {")
marker = s.index("        // ---- extra actions ----", start)
brace = s.rindex("            }", start, marker)
end = brace + len("            }\n")

new_switch = """            switch (p.Action)
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
"""

s = s[:start] + new_switch + s[marker:]

# helpers: add before PowerAction doc
anchor = "        /// <summary>Power actions run on a worker thread"
add = """        static void ScreenshotToClipboard()
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

"""
s = s.replace(anchor, add + anchor, 1)

# confirm sound
s = s.replace("""                if (d != null)
                    d.Invoke(new Action(delegate
                    {
                        approved = GlassConfirm.Show(title, question, verb, icon, true);
                    }));""",
"""                if (d != null)
                    d.Invoke(new Action(delegate
                    {
                        Sounds.Play("confirm");
                        approved = GlassConfirm.Show(title, question, verb, icon, true);
                    }));""")

io.open(p, "w", encoding="utf-8").write(s)
print("engine v3 ok")
