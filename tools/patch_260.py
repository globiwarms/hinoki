"""v2.6.0: new actions, center gear polish, playin guards, debounced rebuild, confirm sound."""
import io

# ============ Config: +3 actions, nicer names ============
p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()
s = s.replace('''            "mute_all", "close_window"
        };''',
'''            "mute_all", "close_window", "paste_plain", "clear_clipboard", "restart_explorer"
        };''')
s = s.replace('''            "Тихий режим (микро + звук)",
            "Закрыть активное окно"
        };''',
'''            "Тихий режим (микро + звук)",
            "Закрыть активное окно",
            "Вставить как обычный текст",
            "Очистить буфер обмена",
            "Перезапустить Проводник"
        };''')
s = s.replace('                case "close_window": return "x";',
'''                case "close_window": return "x";
                case "paste_plain": return "clipboard-paste";
                case "clear_clipboard": return "eraser";
                case "restart_explorer": return "rotate-ccw";''')
io.open(p, "w", encoding="utf-8").write(s)
print("config actions ok")

# ============ ActionEngine: impls + confirm sound + guard ============
p2 = "app/src/ActionEngine.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace('''                case "close_window":
                {
                    IntPtr fg = Native.GetForegroundWindow();
                    if (fg != IntPtr.Zero) Native.SendMessageW(fg, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                    break;
                }''',
'''                case "close_window":
                {
                    IntPtr fg = Native.GetForegroundWindow();
                    if (fg != IntPtr.Zero) Native.SendMessageW(fg, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                    break;
                }
                case "paste_plain": PressCombo("Ctrl+Shift+V"); break;
                case "clear_clipboard":
                {
                    RunOnUi(delegate
                    {
                        try { System.Windows.Clipboard.Clear(); } catch { }
                    });
                    Toast.Show("Буфер очищен", "Содержимое буфера обмена удалено", "eraser");
                    break;
                }
                case "restart_explorer": Shell("taskkill", "/f /im explorer.exe & start explorer.exe"); break;''')
# confirm sound when dialog opens
s2 = s2.replace('''            if (confirm)
            {
                var d = Application.Current != null ? Application.Current.Dispatcher : null;
                if (d != null)
                    d.Invoke(new Action(delegate
                    {
                        approved = GlassConfirm.Show(title, question, verb, icon, true);
                    }));
            }''',
'''            if (confirm)
            {
                var d = Application.Current != null ? Application.Current.Dispatcher : null;
                if (d != null)
                    d.Invoke(new Action(delegate
                    {
                        Sounds.Play("confirm");
                        approved = GlassConfirm.Show(title, question, verb, icon, true);
                    }));
            }''')
io.open(p2, "w", encoding="utf-8").write(s2)
print("engine ok")

# ============ Sounds: confirm tone ============
p3 = "app/src/Sounds.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace('''                case "shutter":
                    tones = new List<double[]> { new double[] { 1568, 0.04, 0.6 }, new double[] { 2093, 0.05, 0.5 } };
                    break;''',
'''                case "shutter":
                    tones = new List<double[]> { new double[] { 1568, 0.04, 0.6 }, new double[] { 2093, 0.05, 0.5 } };
                    break;
                case "confirm":
                    tones = new List<double[]> { new double[] { 392, 0.09, 0.45 }, new double[] { 587, 0.11, 0.45 }, new double[] { 784, 0.1, 0.4 } };
                    break;''')
io.open(p3, "w", encoding="utf-8").write(s3)
print("sounds ok")

# ============ FlowerMenu: guards + gear rotate on hover ============
p4 = "app/src/FlowerMenu.cs"
s4 = io.open(p4, encoding="utf-8").read()
s4 = s4.replace("""            if (instant)
            {
                Opacity = 1;
                var cstI = (ScaleTransform)_centerGroup.RenderTransform;
                cstI.ScaleX = 1; cstI.ScaleY = 1;
                _centerGroup.Opacity = 1;
                return;
            }""",
"""            if (instant)
            {
                Opacity = 1;
                if (_centerGroup != null)
                {
                    var cstI = (ScaleTransform)_centerGroup.RenderTransform;
                    cstI.ScaleX = 1; cstI.ScaleY = 1;
                    _centerGroup.Opacity = 1;
                }
                return;
            }""")
# center: gear rotation transform + hover spin
s4 = s4.replace("""            var gearHost = new Viewbox();
            gearHost.Width = centerD * 0.4; gearHost.Height = centerD * 0.4;
            var gearCanvas = new Canvas();
            gearCanvas.Width = 24; gearCanvas.Height = 24;
            var gearPath = new System.Windows.Shapes.Path();
            gearPath.Data = Lucide.Get("settings");
            gearPath.Stroke = Ui.Solid(Colors.White, 0.97);
            gearPath.StrokeThickness = 1.6;
            gearPath.StrokeLineJoin = PenLineJoin.Round;
            gearPath.StrokeStartLineCap = PenLineCap.Round;
            gearPath.StrokeEndLineCap = PenLineCap.Round;
            gearCanvas.Children.Add(gearPath);
            gearHost.Child = gearCanvas;
            _centerCircle.Child = gearHost;""",
"""            var gearHost = new Viewbox();
            gearHost.Width = centerD * 0.4; gearHost.Height = centerD * 0.4;
            var gearCanvas = new Canvas();
            gearCanvas.Width = 24; gearCanvas.Height = 24;
            var gearPath = new System.Windows.Shapes.Path();
            gearPath.Data = Lucide.Get("settings");
            gearPath.Stretch = Stretch.Uniform;
            gearPath.Width = 24; gearPath.Height = 24;
            gearPath.Stroke = Ui.Solid(Colors.White, 0.97);
            gearPath.StrokeThickness = 1.6;
            gearPath.StrokeLineJoin = PenLineJoin.Round;
            gearPath.StrokeStartLineCap = PenLineCap.Round;
            gearPath.StrokeEndLineCap = PenLineCap.Round;
            var gearRot = new RotateTransform(0);
            gearPath.RenderTransform = gearRot;
            gearCanvas.Children.Add(gearPath);
            gearHost.Child = gearCanvas;
            _gearRotate = gearRot;
            _centerCircle.Child = gearHost;""")
s4 = s4.replace("""            _gearRotate = gearRot;
            _centerCircle.Child = gearHost;""",
"""            _gearRotate = gearRot;
            _centerCircle.Child = gearHost;""")
s4 = s4.replace("        double _size;", "        double _size;\n        RotateTransform _gearRotate;")
# hover spin
s4 = s4.replace("""            _centerGroup.MouseEnter += delegate
            {
                AnimateScale(cst, 1.04, 240);
                cg.BeginAnimation(DropShadowEffect.OpacityProperty,
                    new DoubleAnimation(0.7, new Duration(TimeSpan.FromMilliseconds(240))));
            };
            _centerGroup.MouseLeave += delegate
            {
                AnimateScale(cst, 1, 240);
                cg.BeginAnimation(DropShadowEffect.OpacityProperty,
                    new DoubleAnimation(0.18 + 0.5 * (cfg.GlowStrength / 100.0), new Duration(TimeSpan.FromMilliseconds(240))));
            };""",
"""            _centerGroup.MouseEnter += delegate
            {
                AnimateScale(cst, 1.04, 240);
                cg.BeginAnimation(DropShadowEffect.OpacityProperty,
                    new DoubleAnimation(0.7, new Duration(TimeSpan.FromMilliseconds(240))));
                if (_gearRotate != null)
                {
                    var spin = new DoubleAnimation(45, new Duration(TimeSpan.FromMilliseconds(420)));
                    spin.EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut };
                    _gearRotate.BeginAnimation(RotateTransform.AngleProperty, spin);
                }
            };
            _centerGroup.MouseLeave += delegate
            {
                AnimateScale(cst, 1, 240);
                cg.BeginAnimation(DropShadowEffect.OpacityProperty,
                    new DoubleAnimation(0.18 + 0.5 * (cfg.GlowStrength / 100.0), new Duration(TimeSpan.FromMilliseconds(240))));
                if (_gearRotate != null)
                {
                    var spin = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(420)));
                    spin.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    _gearRotate.BeginAnimation(RotateTransform.AngleProperty, spin);
                }
            };""")
io.open(p4, "w", encoding="utf-8").write(s4)
print("flower gear ok")

# ============ SettingsWindow: debounced preview rebuild ============
p5 = "app/src/SettingsWindow.cs"
s5 = io.open(p5, encoding="utf-8").read()
s5 = s5.replace("        bool _expandedIcons;\n        string _expandLabel = \"\";",
"""        bool _expandedIcons;
        string _expandLabel = "";
        System.Windows.Threading.DispatcherTimer _rebuildDebounce;""")
# helper + replace direct rebuild calls in sliders
s5 = s5.replace("""        public void Pulse()""",
"""        void DebouncedRebuild()
        {
            if (_rebuildDebounce == null)
            {
                _rebuildDebounce = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
                _rebuildDebounce.Tick += delegate
                {
                    _rebuildDebounce.Stop();
                    _preview.Rebuild();
                    _preview.SetSelected(_selPetal);
                };
            }
            _rebuildDebounce.Stop();
            _rebuildDebounce.Start();
        }

        public void Pulse()""")
s5 = s5.replace("""                App.Config.PetalOpacity = (int)Math.Round(opSlider.Value);
                opVal.Text = App.Config.PetalOpacity + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();""",
"""                App.Config.PetalOpacity = (int)Math.Round(opSlider.Value);
                opVal.Text = App.Config.PetalOpacity + "%";
                App.SaveConfig();
                DebouncedRebuild();
                Pulse();""")
s5 = s5.replace("""                App.Config.GlowStrength = (int)Math.Round(glowSlider.Value);
                glowVal.Text = App.Config.GlowStrength + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();""",
"""                App.Config.GlowStrength = (int)Math.Round(glowSlider.Value);
                glowVal.Text = App.Config.GlowStrength + "%";
                App.SaveConfig();
                DebouncedRebuild();
                Pulse();""")
s5 = s5.replace("""                App.Config.CenterOpacity = (int)Math.Round(cSlider.Value);
                cVal.Text = App.Config.CenterOpacity + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();""",
"""                App.Config.CenterOpacity = (int)Math.Round(cSlider.Value);
                cVal.Text = App.Config.CenterOpacity + "%";
                App.SaveConfig();
                DebouncedRebuild();
                Pulse();""")
s5 = s5.replace("""                App.Config.PetalRoundness = (int)Math.Round(rSlider.Value);
                rVal.Text = App.Config.PetalRoundness + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();""",
"""                App.Config.PetalRoundness = (int)Math.Round(rSlider.Value);
                rVal.Text = App.Config.PetalRoundness + "%";
                App.SaveConfig();
                DebouncedRebuild();
                Pulse();""")
io.open(p5, "w", encoding="utf-8").write(s5)
print("debounce ok")
