"""v1.2.0: fix screenshot clipboard, hotkey, remove notes, clean tiles, slider center size, more settings."""
import io

# ============ ActionEngine: fix screenshot (create source before dispose) ============
p = "app/src/ActionEngine.cs"
s = io.open(p, encoding="utf-8").read()

old = s[s.index("        static void ScreenshotToClipboard()"):s.index("        static void PowerMenuDialog()")]
new = """        static void ScreenshotToClipboard()
        {
            try
            {
                var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
                using (var bmp = new System.Drawing.Bitmap(bounds.Width, bounds.Height))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bmp.Size);
                    Sounds.Play("shutter");
                    var hBitmap = bmp.GetHbitmap();
                    try
                    {
                        var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();
                        Native.DeleteObject(hBitmap);
                        RunOnUi(delegate
                        {
                            try { System.Windows.Clipboard.SetImage(source); } catch { }
                        });
                        Toast.Show("Скриншот в буфере", "Вставьте через Ctrl+V", "camera");
                    }
                    catch { Native.DeleteObject(hBitmap); }
                }
            }
            catch (Exception ex) { Report("Hinoki", "Скриншот не получился: " + ex.Message); }
        }

"""
s = s.replace(old, new)

# remove dead code references to old actions
s = s.replace('                case "open_folder": OpenPath(p); break;\n', '')
s = s.replace('                case "run_cmd": RunCmd(p); break;\n', '')
s = s.replace('''        static void OpenPath(PetalConfig p)
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

''', '')
s = s.replace('''        static void PowerMenuDialog()
        {
            RunOnUi(delegate
            {
                string choice = PowerMenu.Show();
                if (choice == "sleep") Native.SetSuspendState(false, false, false);
                else if (choice == "restart") Shell("shutdown", "/r /t 0");
                else if (choice == "shutdown") Shell("shutdown", "/s /t 0");
            });
        }

''', '')
io.open(p, "w", encoding="utf-8").write(s)
print("engine screenshot ok")

# ============ Config: clean actions ============
p2 = "app/src/Config.cs"
s2 = io.open(p2, encoding="utf-8").read()

# Replace Keys and Names with exactly the user's 13
start_k = s2.index("        public static readonly string[] Keys = new string[]")
end_k = s2.index("        };", start_k) + len("        };")
keys = """        public static readonly string[] Keys = new string[]
        {
            "none", "app", "volume_mute", "mic_toggle", "volume_down", "volume_up",
            "screenshot", "media", "open_folder", "run_cmd", "power_menu",
            "show_desktop", "url"
        };"""
s2 = s2[:start_k] + keys + s2[end_k:]

start_n = s2.index("        public static readonly string[] Names = new string[]")
end_n = s2.index("        };", start_n) + len("        };")
names = """        public static readonly string[] Names = new string[]
        {
            "Ничего",
            "Открыть приложение",
            "Звук выкл / вкл",
            "Микрофон выкл / вкл",
            "Тише (25%)",
            "Громче (25%)",
            "Скриншот → в буфер",
            "Пауза / вкл музыки",
            "Открыть выбранную папку",
            "Команда в CMD",
            "Питание: сон / выкл",
            "Свернуть все окна",
            "Открыть ссылку"
        };"""
s2 = s2[:start_n] + names + s2[end_n:]

# IconOf: clean mapping
start_i = s2.index("        public static string IconOf(string key)")
end_i = s2.index("        }", s2.index('                case "url"', start_i)) + len("        }")
iconof = """        public static string IconOf(string key)
        {
            switch (key)
            {
                case "app": return "app-window";
                case "volume_mute": return "volume-x";
                case "mic_toggle": return "mic";
                case "volume_down": return "volume-1";
                case "volume_up": return "volume-2";
                case "screenshot": return "camera";
                case "media": return "play";
                case "open_folder": return "folder";
                case "run_cmd": return "terminal";
                case "power_menu": return "power";
                case "show_desktop": return "monitor";
                case "url": return "link-2";
                default: return "settings";
            }
        }"""
s2 = s2[:start_i] + iconof + s2[end_i:]

# migration: map removed keys
s2 = s2.replace('''                                { "app", "app" }, { "volume_mute", "volume_mute" },''',
'''                                { "app", "app" }, { "volume_mute", "volume_mute" },
                                { "mute_all", "none" }, { "close_window", "none" },
                                { "paste_plain", "none" }, { "clear_clipboard", "none" },
                                { "restart_explorer", "none" }, { "open_discord", "none" },
                                { "open_spotify", "none" }, { "search_start", "none" },
                                { "notification_center", "none" }, { "copy", "none" },
                                { "monitor_off", "none" }, { "clear_recycle_bin", "none" },
                                { "hotkey", "none" }, { "open_steam", "none" },
                                { "open_config", "none" }, { "open_settings", "none" },
                                { "open_downloads", "none" }, { "open_desktop", "none" },
                                { "open_documents", "none" },''')

io.open(p2, "w", encoding="utf-8").write(s2)
print("config actions ok")

# ============ Native: DeleteObject ============
p3 = "app/src/Native.cs"
s3 = io.open(p3, encoding="utf-8").read()
if "DeleteObject" not in s3:
    s3 = s3.replace("""        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();""",
"""        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("native ok")

# ============ UiControls: fix dropdown search + icon tiles fill width ============
p4 = "app/src/UiControls.cs"
s4 = io.open(p4, encoding="utf-8").read()

# GlassTextBox: add Focusable override to avoid issues in popup
s4 = s4.replace("""            Box = new TextBox();
            Box.Text = text;""",
"""            Box = new TextBox();
            Box.Text = text;
            Box.Background = Brushes.Transparent;
            Box.BorderThickness = new Thickness(0);""")

# dropdown search box styled like GlassTextBox
s4 = s4.replace("""            var searchBox = new TextBox();
            searchBox.FontFamily = Ui.Text;
            searchBox.FontSize = 13;
            searchBox.Foreground = Ui.Solid(Ui.Ink, 0.97);
            searchBox.CaretBrush = Ui.Solid(Ui.Accent, 1);
            searchBox.Background = Ui.Solid(Colors.White, 0.06);
            searchBox.BorderBrush = Ui.Solid(Colors.White, 0.10);
            searchBox.BorderThickness = new Thickness(1);
            searchBox.Padding = new Thickness(9, 6, 9, 7);
            searchBox.Margin = new Thickness(6, 6, 6, 4);""",
"""            var searchBox = new TextBox();
            searchBox.FontFamily = Ui.Text;
            searchBox.FontSize = 13;
            searchBox.Foreground = Ui.Solid(Ui.Ink, 0.97);
            searchBox.CaretBrush = Ui.Solid(Ui.Accent, 1);
            searchBox.Background = Ui.Solid(Colors.White, 0.05);
            searchBox.BorderBrush = Ui.Solid(Colors.White, 0.08);
            searchBox.BorderThickness = new Thickness(1);
            searchBox.Padding = new Thickness(10, 7, 10, 8);
            searchBox.Margin = new Thickness(6, 6, 6, 4);
            searchBox.HorizontalAlignment = HorizontalAlignment.Stretch;""")

io.open(p4, "w", encoding="utf-8").write(s4)
print("controls ok")
