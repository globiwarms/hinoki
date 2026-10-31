"""v1.2.4 source patch: overlay square fix, new actions, center opacity."""
import io

# 1) OverlayWindow: remove DWM rounding — it paints a visible square outline on the transparent window
p = "app/src/OverlayWindow.cs"
s = io.open(p, encoding="utf-8").read()
old = """        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int pref = Native.DWMWCP_ROUND;
            Native.DwmSetWindowAttribute(hwnd, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
        }"""
if old in s:
    s = s.replace(old, "")
    s = s.replace("using System.Windows.Interop;\n", "")
io.open(p, "w", encoding="utf-8").write(s)
print("overlay: dwm square removed")

# 2) Config: CenterOpacity + version 4
p2 = "app/src/Config.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("        public int GlowStrength { get; set; }      // 0..100",
                "        public int GlowStrength { get; set; }      // 0..100\n        public int CenterOpacity { get; set; }     // 30..100")
s2 = s2.replace("""                        if (cfg.Version < 3)
                        {
                            cfg.AnimationsEnabled = true;
                            cfg.CloseOnOutsideClick = true;
                        }
                        cfg.Version = 3;""",
"""                        if (cfg.Version < 3)
                        {
                            cfg.AnimationsEnabled = true;
                            cfg.CloseOnOutsideClick = true;
                        }
                        if (cfg.Version < 4)
                        {
                            cfg.CenterOpacity = 92;
                        }
                        cfg.Version = 4;""")
s2 = s2.replace("""            c.Version = 3;
            c.InterceptClick = true;""",
"""            c.Version = 4;
            c.InterceptClick = true;""")
s2 = s2.replace("            c.GlowStrength = 40;",
                "            c.GlowStrength = 40;\n            c.CenterOpacity = 92;")
s2 = s2.replace("""            if (GlowStrength < 0) GlowStrength = 0;
            if (GlowStrength > 100) GlowStrength = 100;""",
"""            if (GlowStrength < 0) GlowStrength = 0;
            if (GlowStrength > 100) GlowStrength = 100;
            if (CenterOpacity < 30) CenterOpacity = 30;
            if (CenterOpacity > 100) CenterOpacity = 100;""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("config: center opacity + v4")

# 3) Actions: 4 new
p3 = "app/src/Config.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace('            "screenshot_file", "screenshot_buffer",',
                '            "screenshot_file", "screenshot_buffer", "screenshot_window",')
s3 = s3.replace('            "open_steam", "open_config", "open_settings"',
                '            "open_steam", "open_config", "open_settings",\n'
                '            "open_downloads", "open_desktop", "open_documents"')
s3 = s3.replace('            "Скриншот экрана → в Изображения",',
                '            "Скриншот экрана → в Изображения",\n'
                '            "Скриншот активного окна → в Изображения",')
s3 = s3.replace('            "Открыть настройки Hinoki"',
                '            "Открыть настройки Hinoki",\n'
                '            "Открыть папку Загрузки",\n'
                '            "Открыть папку Рабочий стол",\n'
                '            "Открыть папку Документы"')
s3 = s3.replace('                case "screenshot_file": return "E722";',
                '                case "screenshot_file": return "E722";\n'
                '                case "screenshot_window": return "E7A5";')
s3 = s3.replace('                case "open_settings": return "E713";',
                '                case "open_settings": return "E713";\n'
                '                case "open_downloads": return "E896";\n'
                '                case "open_desktop": return "E9F9";\n'
                '                case "open_documents": return "E8A5";')
io.open(p3, "w", encoding="utf-8").write(s3)
print("actions: 33 total")

# 4) ActionEngine: new impls
p4 = "app/src/ActionEngine.cs"
s4 = io.open(p4, encoding="utf-8").read()
s4 = s4.replace('                case "screenshot_file": ScreenshotToFile(); break;',
                '                case "screenshot_file": ScreenshotToFile(false); break;\n'
                '                case "screenshot_window": ScreenshotToFile(true); break;')
s4 = s4.replace('                case "open_config": OpenLink(AppConfig.Dir); break;',
                '                case "open_config": OpenLink(AppConfig.Dir); break;\n'
                '                case "open_downloads": Shell("explorer.exe", "shell:Downloads"); break;\n'
                '                case "open_desktop": OpenLink(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)); break;\n'
                '                case "open_documents": OpenLink(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); break;')

old_shot = s4[s4.index("        static void ScreenshotToFile()"):s4.index("        static void MinimizeActive()")]
new_shot = (
"        static void ScreenshotToFile(bool activeWindowOnly)\n"
"        {\n"
"            try\n"
"            {\n"
"                int x, y, w, h;\n"
"                if (activeWindowOnly)\n"
"                {\n"
"                    IntPtr fg = Native.GetForegroundWindow();\n"
"                    if (fg == IntPtr.Zero) { Report(\"Hinoki\", \"Нет активного окна.\"); return; }\n"
"                    Native.RECT r;\n"
"                    if (!Native.GetWindowRect(fg, out r)) { Report(\"Hinoki\", \"Не удалось измерить окно.\"); return; }\n"
"                    x = r.Left; y = r.Top; w = r.Right - r.Left; h = r.Bottom - r.Top;\n"
"                }\n"
"                else\n"
"                {\n"
"                    var bounds = System.Windows.Forms.SystemInformation.VirtualScreen;\n"
"                    x = bounds.X; y = bounds.Y; w = bounds.Width; h = bounds.Height;\n"
"                }\n"
"                if (w <= 0 || h <= 0) { Report(\"Hinoki\", \"Пустая область для снимка.\"); return; }\n"
"                using (var bmp = new System.Drawing.Bitmap(w, h))\n"
"                {\n"
"                    using (var g = System.Drawing.Graphics.FromImage(bmp))\n"
"                        g.CopyFromScreen(x, y, 0, 0, bmp.Size);\n"
"                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);\n"
"                    string file = System.IO.Path.Combine(dir, \"Hinoki_\" + DateTime.Now.ToString(\"yyyyMMdd_HHmmss\") + \".png\");\n"
"                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);\n"
"                    Report(\"Hinoki\", \"Скриншот сохранён: \" + file);\n"
"                }\n"
"            }\n"
"            catch (Exception ex) { Report(\"Hinoki\", \"Скриншот не получился: \" + ex.Message); }\n"
"        }\n\n"
)
s4 = s4.replace(old_shot, new_shot)
io.open(p4, "w", encoding="utf-8").write(s4)
print("engine ok")

# 5) Native: GetWindowRect
p5 = "app/src/Native.cs"
s5 = io.open(p5, encoding="utf-8").read()
s5 = s5.replace("""        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();""",
"""        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);""")
io.open(p5, "w", encoding="utf-8").write(s5)
print("native ok")

# 6) FlowerMenu: center opacity
p6 = "app/src/FlowerMenu.cs"
s6 = io.open(p6, encoding="utf-8").read()
s6 = s6.replace("""            _centerCircle.Background = light ? Ui.GlassFill(0.80 * k, 0.55 * k) : Ui.GlassFill(0.30 * k, 0.16 * k);
            _centerCircle.BorderBrush = Ui.Solid(Colors.White, light ? 0.7 : 0.5);""",
"""            double ck = cfg.CenterOpacity / 100.0;
            _centerCircle.Background = light ? Ui.GlassFill(0.80 * ck, 0.55 * ck) : Ui.GlassFill(0.30 * ck, 0.16 * ck);
            _centerCircle.BorderBrush = Ui.Solid(Colors.White, (light ? 0.7 : 0.5) * ck);""")
io.open(p6, "w", encoding="utf-8").write(s6)
print("flower ok")
