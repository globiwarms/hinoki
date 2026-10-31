"""v1.3.0 core patch: single-frame settings, new config fields, actions, icon set."""
import io

# ============ Config.cs ============
p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()

s = s.replace("        public int CenterOpacity { get; set; }     // 30..100",
"""        public int CenterOpacity { get; set; }     // 30..100
        public int PetalRoundness { get; set; }    // 50..100 (% of capsule radius)
        public int AnimSpeed { get; set; }         // 50..200 (%)
        public string EntranceStyle { get; set; }  // spring | smooth | instant
        public int AutoCloseSeconds { get; set; }  // 0 = off""")

s = s.replace("""                        if (cfg.Version < 4)
                        {
                            cfg.CenterOpacity = 92;
                        }
                        cfg.Version = 4;""",
"""                        if (cfg.Version < 4)
                        {
                            cfg.CenterOpacity = 92;
                        }
                        if (cfg.Version < 5)
                        {
                            cfg.PetalRoundness = 100;
                            cfg.AnimSpeed = 100;
                            cfg.EntranceStyle = "spring";
                            cfg.AutoCloseSeconds = 0;
                        }
                        cfg.Version = 5;""")

s = s.replace("""            c.Version = 4;
            c.InterceptClick = true;""",
"""            c.Version = 5;
            c.InterceptClick = true;""")
s = s.replace("            c.CenterOpacity = 92;",
"""            c.CenterOpacity = 92;
            c.PetalRoundness = 100;
            c.AnimSpeed = 100;
            c.EntranceStyle = "spring";
            c.AutoCloseSeconds = 0;""")
s = s.replace("""            if (CenterOpacity < 30) CenterOpacity = 30;
            if (CenterOpacity > 100) CenterOpacity = 100;""",
"""            if (CenterOpacity < 30) CenterOpacity = 30;
            if (CenterOpacity > 100) CenterOpacity = 100;
            if (PetalRoundness < 50) PetalRoundness = 50;
            if (PetalRoundness > 100) PetalRoundness = 100;
            if (AnimSpeed < 50) AnimSpeed = 50;
            if (AnimSpeed > 200) AnimSpeed = 200;
            if (AutoCloseSeconds < 0) AutoCloseSeconds = 0;
            if (AutoCloseSeconds > 30) AutoCloseSeconds = 30;
            if (EntranceStyle != "spring" && EntranceStyle != "smooth" && EntranceStyle != "instant") EntranceStyle = "spring";""")

# 7 new actions
s = s.replace('            "open_downloads", "open_desktop", "open_documents"',
'''            "open_downloads", "open_desktop", "open_documents",
            "copy", "monitor_off", "clear_recycle_bin", "open_discord", "open_spotify",
            "search_start", "notification_center"''')
s = s.replace('            "Открыть папку Документы"',
'''            "Открыть папку Документы",
            "Копировать выделенное",
            "Выключить монитор",
            "Очистить корзину",
            "Открыть Discord",
            "Открыть Spotify",
            "Поиск Windows",
            "Центр уведомлений"''')
s = s.replace('                case "open_documents": return "E8A5";',
'''                case "open_documents": return "E8A5";
                case "copy": return "E8C8";
                case "monitor_off": return "E7F4";
                case "clear_recycle_bin": return "E74D";
                case "open_discord": return "E8BD";
                case "open_spotify": return "EC4F";
                case "search_start": return "E721";
                case "notification_center": return "E7E7";''')

io.open(p, "w", encoding="utf-8").write(s)
print("config v5 ok")

# ============ ActionEngine: new impls ============
p2 = "app/src/ActionEngine.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace('                case "paste_clipboard": PressCombo("Ctrl+V"); break;',
'''                case "paste_clipboard": PressCombo("Ctrl+V"); break;
                case "copy": PressCombo("Ctrl+C"); break;
                case "monitor_off": MonitorOff(); break;
                case "clear_recycle_bin": Shell("powershell.exe", "-NoProfile -Command Clear-RecycleBin -Force -ErrorAction SilentlyContinue"); break;
                case "open_discord": OpenLink("discord://"); break;
                case "open_spotify": OpenLink("spotify:"); break;
                case "search_start": PressCombo("Win+S"); break;
                case "notification_center": PressCombo("Win+N"); break;''')
s2 = s2.replace("""        static void MinimizeActive()""",
"""        static void MonitorOff()
        {
            Native.SendMessageW(new IntPtr(0xFFFF), 0x0112, new IntPtr(0xF170), new IntPtr(2));
        }

        static void MinimizeActive()""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("engine ok")

# ============ Native: SendMessage ============
p3 = "app/src/Native.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace("""        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);""",
"""        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("native ok")

# ============ SettingsWindow: kill double frames, gradient glass ============
p4 = "app/src/SettingsWindow.cs"
s4 = io.open(p4, encoding="utf-8").read()

# rich gradient instead of flat fill; remove DWM + acrylic
s4 = s4.replace("""            _chrome = new Border();
            _chrome.CornerRadius = new CornerRadius(22);
            _chrome.Background = Ui.Solid(Color.FromRgb(0x10, 0x11, 0x15), 0.88);
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.10);
            _chrome.BorderThickness = new Thickness(1);
            _chrome.ClipToBounds = true;
            _chrome.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 44, ShadowDepth = 10, Opacity = 0.5 };""",
"""            _chrome = new Border();
            _chrome.CornerRadius = new CornerRadius(22);
            var chromeFill = new LinearGradientBrush();
            chromeFill.StartPoint = new Point(0, 0);
            chromeFill.EndPoint = new Point(0.7, 1);
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x18, 0x1A, 0x20), 0));
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x11, 0x12, 0x17), 0.55));
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x0D, 0x0E, 0x12), 1));
            chromeFill.Freeze();
            _chrome.Background = chromeFill;
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.09);
            _chrome.BorderThickness = new Thickness(1);
            _chrome.ClipToBounds = true;
            _chrome.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 44, ShadowDepth = 10, Opacity = 0.5 };""")

s4 = s4.replace("""            Loaded += delegate
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                Native.RoundCorners(hwnd);
                if (App.Config.BlurBackground)
                    Native.ApplyAcrylic(hwnd, 0xDC, 0x0F, 0x10, 0x14);
                Entrance();
            };""",
"""            Loaded += delegate
            {
                // single frame: my own border only — no DWM rounding, no acrylic sheet
                Entrance();
            };""")

# remove the "Размытие окна настроек" toggle (acrylic is gone)
s4 = s4.replace("""            var tgBlur = new GlassToggle(cfgApp.BlurBackground);
            tgBlur.Changed += delegate { App.Config.BlurBackground = tgBlur.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Размытие окна настроек", "Acrylic-фон этого окна. Выключите, если видите артефакты", tgBlur));

""", "")

# version strings
s4 = s4.replace('Glass.Label("v1.2.4", 10, true)', 'Glass.Label("v1.3.0", 10, true)')
s4 = s4.replace('Glass.Label("Версия 1.2.4 · Windows 10/11", 12, true)', 'Glass.Label("Версия 1.3.0 · Windows 10/11", 12, true)')

io.open(p4, "w", encoding="utf-8").write(s4)
print("settings glass ok")
