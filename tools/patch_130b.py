"""v1.3.0 part 2: petal roundness + entrance styles + anim speed + autoclose + icon search."""
import io

# ============ Ui helper ============
p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()
s = s.replace("""        public static char Glyph(string hex)""",
"""        public static double AnimFactor()
        {
            var cfg = App.Config;
            return cfg != null ? cfg.AnimSpeed / 100.0 : 1.0;
        }

        public static char Glyph(string hex)""")
io.open(p, "w", encoding="utf-8").write(s)
print("ui animfactor ok")

# ============ PetalView: roundness + entrance styles ============
p2 = "app/src/FlowerMenu.cs"
s2 = io.open(p2, encoding="utf-8").read()

s2 = s2.replace("            double r = h * 0.48;",
"""            double round = cfgApp != null ? cfgApp.PetalRoundness / 100.0 : 1.0;
            double r = Math.Min(h * 0.48, h * 0.48 * round);""")

s2 = s2.replace("""        public void PlayIn(TimeSpan delay)
        {
            if (App.Config != null && !App.Config.AnimationsEnabled)
            {
                Scale.ScaleX = 1; Scale.ScaleY = 1;
                Opacity = 1;
                return;
            }
            Scale.ScaleX = 0.35; Scale.ScaleY = 0.35;
            Opacity = 0;
            var t = new System.Windows.Threading.DispatcherTimer { Interval = delay };
            t.Tick += delegate
            {
                t.Stop();
                var e = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };
                var gx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(460))) { EasingFunction = e };
                var gy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(460))) { EasingFunction = e };
                Scale.BeginAnimation(ScaleTransform.ScaleXProperty, gx);
                Scale.BeginAnimation(ScaleTransform.ScaleYProperty, gy);
                var go = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(220)));
                BeginAnimation(UIElement.OpacityProperty, go);
            };
            t.Start();
        }""",
"""        public void PlayIn(TimeSpan delay)
        {
            var cfgApp = App.Config;
            string style = cfgApp != null ? cfgApp.EntranceStyle : "spring";
            if (cfgApp != null && (!cfgApp.AnimationsEnabled || style == "instant"))
            {
                Scale.ScaleX = 1; Scale.ScaleY = 1;
                Opacity = 1;
                return;
            }
            double sp = cfgApp != null ? cfgApp.AnimSpeed / 100.0 : 1.0;
            Scale.ScaleX = 0.35; Scale.ScaleY = 0.35;
            Opacity = 0;
            var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delay.TotalMilliseconds / sp) };
            t.Tick += delegate
            {
                t.Stop();
                IEasingFunction e;
                if (style == "smooth") e = new CubicEase { EasingMode = EasingMode.EaseOut };
                else e = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };
                var gx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(460 / sp))) { EasingFunction = e };
                var gy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(460 / sp))) { EasingFunction = e };
                Scale.BeginAnimation(ScaleTransform.ScaleXProperty, gx);
                Scale.BeginAnimation(ScaleTransform.ScaleYProperty, gy);
                var go = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(220 / sp)));
                BeginAnimation(UIElement.OpacityProperty, go);
            };
            t.Start();
        }""")

# FlowerMenu.PlayIn: stagger + center scale respect speed and entrance style
s2 = s2.replace("""        public void PlayIn()
        {
            Opacity = 0;
            var fade = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(200)));
            fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            BeginAnimation(OpacityProperty, fade);

            var cst = (ScaleTransform)_centerGroup.RenderTransform;
            _centerGroup.Opacity = 0;
            var cgo = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(240)));
            _centerGroup.BeginAnimation(OpacityProperty, cgo);
            AnimateScale(cst, 1, 420, 0.55);

            for (int i = 0; i < _petals.Count; i++)
                _petals[i].PlayIn(TimeSpan.FromMilliseconds(40 + i * 40));
        }""",
"""        public void PlayIn()
        {
            var cfgApp = App.Config;
            string style = cfgApp != null ? cfgApp.EntranceStyle : "spring";
            double sp = cfgApp != null ? cfgApp.AnimSpeed / 100.0 : 1.0;
            bool instant = cfgApp != null && (!cfgApp.AnimationsEnabled || style == "instant");

            if (instant)
            {
                Opacity = 1;
                var cstI = (ScaleTransform)_centerGroup.RenderTransform;
                cstI.ScaleX = 1; cstI.ScaleY = 1;
                _centerGroup.Opacity = 1;
                return;
            }

            Opacity = 0;
            var fade = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(200 / sp)));
            fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            BeginAnimation(OpacityProperty, fade);

            var cst = (ScaleTransform)_centerGroup.RenderTransform;
            _centerGroup.Opacity = 0;
            var cgo = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(240 / sp)));
            _centerGroup.BeginAnimation(OpacityProperty, cgo);
            AnimateScale(cst, 1, (int)(420 / sp), 0.55);

            for (int i = 0; i < _petals.Count; i++)
                _petals[i].PlayIn(TimeSpan.FromMilliseconds((40 + i * 40) / sp));
        }""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("flower ok")

# ============ OverlayWindow: autoclose + speed ============
p3 = "app/src/OverlayWindow.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace("""            Show();
            Sounds.Play("open");
            _flower.PlayIn();""",
"""            Show();
            Sounds.Play("open");
            _flower.PlayIn();

            if (App.Config.AutoCloseSeconds > 0)
            {
                var auto = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(App.Config.AutoCloseSeconds) };
                auto.Tick += delegate
                {
                    auto.Stop();
                    if (Current == this) SafeClose();
                };
                auto.Start();
            }""")
s3 = s3.replace("""            Sounds.Play("close");
            if (App.Config != null && !App.Config.AnimationsEnabled) { Close(); return; }
            if (_flower != null) _flower.PlayOut(delegate { Close(); });
            else Close();""",
"""            Sounds.Play("close");
            if (App.Config != null && (!App.Config.AnimationsEnabled || App.Config.EntranceStyle == "instant")) { Close(); return; }
            double sp = App.Config != null ? App.Config.AnimSpeed / 100.0 : 1.0;
            if (_flower != null)
            {
                var fade = new System.Windows.Media.Animation.DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(170 / sp)));
                fade.Completed += delegate { Close(); };
                BeginAnimation(OpacityProperty, fade);
            }
            else Close();""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("overlay ok")
