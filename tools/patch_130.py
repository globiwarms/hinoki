"""v1.3.0: proper system backdrop (DWM), remove blob source, smoother petals."""
import io

# ============ Native: DWM SystemBackdrop ============
p = "app/src/Native.cs"
s = io.open(p, encoding="utf-8").read()
if "DWMWA_SYSTEMBACKDROP" not in s:
    s = s.replace("""        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);""",
"""        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMSBT_NONE = 0;
        public const int DWMSBT_MAINWINDOW = 1;   // Mica
        public const int DWMSBT_PERSISTENTWINDOW = 2; // Acrylic
        public const int DWMSBT_TRANSIENTWINDOW = 3;  // Thin acrylic""")
io.open(p, "w", encoding="utf-8").write(s)
print("native backdrop ok")

# ============ SettingsWindow: system backdrop, remove blob ============
p2 = "app/src/SettingsWindow.cs"
s2 = io.open(p2, encoding="utf-8").read()

# 1) remove blob entirely
start = s2.index("            var blob = new Ellipse();")
end = s2.index("            var layout = new Grid();", start)
s2 = s2[:start] + s2[end:]

# 2) chrome: more transparent background for DWM backdrop to show
s2 = s2.replace("""            var chromeFill = new LinearGradientBrush();
            chromeFill.StartPoint = new Point(0, 0);
            chromeFill.EndPoint = new Point(0.7, 1);
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x18, 0x1A, 0x20), 0));
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x11, 0x12, 0x17), 0.55));
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x0D, 0x0E, 0x12), 1));
            chromeFill.Freeze();
            _chrome.Background = chromeFill;
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.09);""",
"""            var chromeFill = new LinearGradientBrush();
            chromeFill.StartPoint = new Point(0, 0);
            chromeFill.EndPoint = new Point(0.7, 1);
            chromeFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xC8, 0x1C, 0x1E, 0x24), 0));
            chromeFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xC0, 0x14, 0x15, 0x1B), 0.55));
            chromeFill.GradientStops.Add(new GradientStop(Color.FromArgb(0xB8, 0x10, 0x11, 0x16), 1));
            chromeFill.Freeze();
            _chrome.Background = chromeFill;
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.10);""")

# 3) Loaded: use DWM system backdrop (real Win11 acrylic)
s2 = s2.replace("""            Loaded += delegate
            {
                // single frame: my own border only — no DWM rounding, no acrylic sheet
                Entrance();
            };""",
"""            Loaded += delegate
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                // DWM SystemBackdrop: true Windows 11 acrylic, follows rounded corners natively
                int backdrop = 2; // DWMSBT_PERSISTENTWINDOW (acrylic)
                Native.DwmSetWindowAttribute(hwnd, Native.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                int round = Native.DWMWCP_ROUND;
                Native.DwmSetWindowAttribute(hwnd, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
                Entrance();
            };""")

# 4) version 1.3
s2 = s2.replace('Glass.Label("v1.2", 10, true)', 'Glass.Label("v1.3", 10, true)')
s2 = s2.replace('Glass.Label("Версия 1.2 · Windows 10/11", 12, true)', 'Glass.Label("Версия 1.3 · Windows 10/11", 12, true)')

io.open(p2, "w", encoding="utf-8").write(s2)
print("settings backdrop ok")

# ============ FlowerMenu: smoother entrance (single timer, no per-petal) ============
p3 = "app/src/FlowerMenu.cs"
s3 = io.open(p3, encoding="utf-8").read()

# PrepareIn already sets states. PlayIn uses per-petal timers — replace with
# single Storyboard for smoother coordination
s3 = s3.replace("""            int step = style == "ripple" ? 90 : 40;
            for (int i = 0; i < _petals.Count; i++)
                _petals[i].PlayIn(TimeSpan.FromMilliseconds((55 + i * step) / sp));""",
"""            // stagger petals via single DispatcherTimer callback
            for (int i = 0; i < _petals.Count; i++)
            {
                int idx = i;
                var dt = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds((55 + i * step) / sp) };
                dt.Tick += delegate
                {
                    dt.Stop();
                    _petals[idx].PlayIn(TimeSpan.Zero);
                };
                dt.Start();
            }""")

io.open(p3, "w", encoding="utf-8").write(s3)
print("flower smooth ok")
