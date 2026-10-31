"""v3.0.0 patch C: PowerMenu glass dialog + per-petal hotkey manager."""
import io

# ============ Toast.cs: append PowerMenu dialog ============
p = "app/src/Toast.cs"
s = io.open(p, encoding="utf-8").read()
s += """
    public static class PowerMenu
    {
        /// <summary>Glass power dialog: sleep / restart / shutdown. Returns choice or "".</summary>
        public static string Show()
        {
            string result = "";
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d == null) return result;
            if (d.CheckAccess()) { BuildAndShow(ref result); return result; }
            d.Invoke(new Action(delegate { BuildAndShow(ref result); }));
            return result;
        }

        class Choice { public string Value; }

        static void BuildAndShow(ref string result)
        {
            try
            {
                var win = new Window();
                win.Width = 400;
                win.SizeToContent = SizeToContent.WidthAndHeight;
                win.WindowStyle = WindowStyle.None;
                win.AllowsTransparency = true;
                win.Background = Brushes.Transparent;
                win.ShowInTaskbar = false;
                win.Topmost = true;
                win.ResizeMode = ResizeMode.NoResize;
                win.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                var chrome = new Border();
                chrome.CornerRadius = new CornerRadius(20);
                var fill = new LinearGradientBrush();
                fill.StartPoint = new Point(0, 0); fill.EndPoint = new Point(0.6, 1);
                fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x20, 0x22, 0x29), 0));
                fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x13, 0x14, 0x19), 1));
                fill.Freeze();
                chrome.Background = fill;
                chrome.BorderBrush = Ui.Solid(Colors.White, 0.13);
                chrome.BorderThickness = new Thickness(1);
                chrome.Padding = new Thickness(14);

                var stack = new StackPanel();

                var t = Glass.Label("Питание", 16, false, true);
                t.Margin = new Thickness(8, 4, 0, 10);
                stack.Children.Add(t);

                Choice holder = new Choice();
                var rows = new (string icon, string label, string key, bool danger)[]
                {
                    ("moon", "Спящий режим", "sleep", false),
                    ("rotate-cw", "Перезагрузка", "restart", false),
                    ("power", "Выключение", "shutdown", true)
                };
                foreach (var r in rows)
                {
                    var rowDef = r;
                    var row = new Border();
                    row.CornerRadius = new CornerRadius(12);
                    row.Padding = new Thickness(12, 9, 12, 10);
                    row.Margin = new Thickness(0, 2, 0, 2);
                    row.Cursor = Cursors.Hand;
                    row.Background = Ui.Solid(Colors.White, 0.04);

                    var g = new Grid();
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var ic = LucideHost.Build(rowDef.icon, 18, false);
                    var icHost = new ContentControl();
                    icHost.Content = ic;
                    icHost.Width = 20; icHost.Height = 20;
                    icHost.VerticalAlignment = VerticalAlignment.Center;
                    if (rowDef.danger) ((System.Windows.Shapes.Path)ic).Stroke = Ui.Solid(Ui.Danger, 1);
                    Grid.SetColumn(icHost, 0);
                    g.Children.Add(icHost);
                    var t = Glass.Label(rowDef.label, 13.5, false, true);
                    t.Margin = new Thickness(12, 0, 0, 0);
                    if (rowDef.danger) t.Foreground = Ui.Solid(Ui.Danger, 1);
                    t.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(t, 1);
                    g.Children.Add(t);
                    row.Child = g;

                    row.MouseEnter += delegate { row.Background = Ui.Solid(Colors.White, 0.08); };
                    row.MouseLeave += delegate { row.Background = Ui.Solid(Colors.White, 0.04); };
                    row.MouseLeftButtonDown += delegate
                    {
                        holder.Value = rowDef.key;
                        win.Close();
                    };
                    stack.Children.Add(row);
                }

                var cancel = new PillButton("Отмена", false);
                cancel.Margin = new Thickness(0, 10, 0, 0);
                cancel.HorizontalAlignment = HorizontalAlignment.Center;
                cancel.Clicked += delegate { win.Close(); };
                stack.Children.Add(cancel);

                chrome.Child = stack;
                win.Content = chrome;
                win.ShowDialog();
                result = holder.Value;
            }
            catch { }
        }
    }
"""
io.open(p, "w", encoding="utf-8").write(s)
print("power menu ok")

# ============ App.cs: per-petal hotkey manager ============
p2 = "app/src/App.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("""        public static void ApplyGlobalHotkey()""",
"""        const int PETAL_HOTKEY_BASE = 0xB200;

        public static void ApplyPetalHotkeys()
        {
            try
            {
                if (_hotkeySrc == null || Config == null) return;
                for (int i = 0; i < 8; i++)
                    Native.UnregisterHotKey(_hotkeySrc.Handle, PETAL_HOTKEY_BASE + i);
                for (int i = 0; i < Config.Petals.Count && i < 8; i++)
                {
                    uint mods, vk;
                    if (ActionEngine.ParseHotkeyParts(Config.Petals[i].Hotkey, out mods, out vk))
                        Native.RegisterHotKey(_hotkeySrc.Handle, PETAL_HOTKEY_BASE + i, mods | 0x4000, vk);
                }
            }
            catch { }
        }

        public static void ApplyGlobalHotkey()""")
s2 = s2.replace("""            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (OverlayWindow.Current != null) { OverlayWindow.Current.SafeClose(); return; }
                    var p = WF.Cursor.Position;
                    OnMiddleDown(p.X, p.Y);
                }));
                handled = true;
            }
            return IntPtr.Zero;""",
"""            int id = wParam.ToInt32();
            if (msg == WM_HOTKEY && id == HOTKEY_ID)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (OverlayWindow.Current != null) { OverlayWindow.Current.SafeClose(); return; }
                    var p = WF.Cursor.Position;
                    OnMiddleDown(p.X, p.Y);
                }));
                handled = true;
            }
            else if (msg == WM_HOTKEY && id >= PETAL_HOTKEY_BASE && id < PETAL_HOTKEY_BASE + 8)
            {
                int idx = id - PETAL_HOTKEY_BASE;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (Config != null && idx < Config.Petals.Count)
                    {
                        var petal = Config.Petals[idx];
                        if (petal.Action == "mic_toggle" || petal.Action == "volume_mute")
                            Sounds.Play(petal.Action == "mic_toggle" ? (Audio.IsMicMuted() ? "toggle_on" : "toggle_off") : "click");
                        ActionEngine.Run(petal);
                    }
                }));
                handled = true;
            }
            return IntPtr.Zero;""")
# re-register on every config save
s2 = s2.replace("""        public static void SaveConfig()
        {
            if (Config != null) Config.Save();
            if (Tray.HasIcon) Tray.Sync();
        }""",
"""        public static void SaveConfig()
        {
            if (Config != null) Config.Save();
            ApplyPetalHotkeys();
            if (Tray.HasIcon) Tray.Sync();
        }""")
# also apply after startup
s2 = s2.replace("""                    SetupGlobalHotkey();""",
"""                    SetupGlobalHotkey();
                    Dispatcher.BeginInvoke(new Action(delegate { ApplyPetalHotkeys(); }),
                        System.Windows.Threading.DispatcherPriority.ApplicationIdle);""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("hotkeys ok")

# ============ SaveConfig hook already calls ApplyPetalHotkeys ============
io.open(p2, "w", encoding="utf-8").write(s2)
print("app ok")
