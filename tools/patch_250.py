"""v2.5.0: constant-stroke lucide rendering (Path.Stretch instead of Viewbox),
idle float for petals, springy hover, shutter sound, polish."""
import io

# ============ UiControls: LucideHost.Build -> constant stroke ============
p = "app/src/UiControls.cs"
s = io.open(p, encoding="utf-8").read()
old = s[s.index("    public static class LucideHost"):s.index("    // ---------- toggle ----------")]
new = """    public static class LucideHost
    {
        public static UIElement Build(string name, double size, bool light)
        {
            var path = new System.Windows.Shapes.Path();
            path.Data = Lucide.Get(name);
            path.Stretch = Stretch.Uniform;              // geometry fits, stroke stays constant
            path.Stroke = light ? Ui.Solid(Color.FromRgb(0x24, 0x2B, 0x1E), 0.95) : Ui.Solid(Colors.White, 0.95);
            path.StrokeThickness = 1.6;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.Width = size; path.Height = size;
            path.HorizontalAlignment = HorizontalAlignment.Center;
            path.VerticalAlignment = VerticalAlignment.Center;
            return path;
        }
    }

    // ---------- toggle ----------"""
s = s.replace(old, new)
io.open(p, "w", encoding="utf-8").write(s)
print("lucide host constant stroke ok")

# ============ FlowerMenu: PetalView same + idle float + springy hover + app icon ============
p2 = "app/src/FlowerMenu.cs"
s2 = io.open(p2, encoding="utf-8").read()

s2 = s2.replace("""        static UIElement MakeIconVisual(string icon, double size, bool light)
        {
            return LucideHost.Build(icon, size, light);
        }""",
"""        static UIElement MakeIconVisual(string icon, double size, bool light)
        {
            var path = new System.Windows.Shapes.Path();
            path.Data = Lucide.Get(icon);
            path.Stretch = Stretch.Uniform;
            path.Stroke = light ? Ui.Solid(Color.FromRgb(0x24, 0x2B, 0x1E), 0.95) : Ui.Solid(Colors.White, 0.97);
            path.StrokeThickness = 1.5;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.Width = size; path.Height = size;
            path.HorizontalAlignment = HorizontalAlignment.Center;
            path.VerticalAlignment = VerticalAlignment.Center;
            return path;
        }""")

# app icon in BuildIconHost: keep; but sizes — fine.

# idle float: add translate to group in RotateShape + start float
s2 = s2.replace("""        /// <summary>Rotate only the glass shape; the icon and label stay horizontal.
        /// The rotation lives on the inner shape (root keeps a clean center-origin scale),
        /// otherwise RenderTransformOrigin compounds with the group and petals drift off-orbit.</summary>
        public void RotateShape(double theta)
        {
            var g = new TransformGroup();
            g.Children.Add(Scale);
            g.Children.Add(new RotateTransform(theta, Width / 2, Height / 2));
            RenderTransform = g;
        }""",
"""        readonly TranslateTransform _floatY = new TranslateTransform();

        /// <summary>Rotate only the glass shape; the icon and label stay horizontal.
        /// The rotation lives on the inner shape (root keeps a clean center-origin scale),
        /// otherwise RenderTransformOrigin compounds with the group and petals drift off-orbit.</summary>
        public void RotateShape(double theta)
        {
            var g = new TransformGroup();
            g.Children.Add(Scale);
            g.Children.Add(new RotateTransform(theta, Width / 2, Height / 2));
            g.Children.Add(_floatY);
            RenderTransform = g;
        }

        /// <summary>Gentle perpetual float — the glass feels alive.</summary>
        public void StartFloat(int index)
        {
            if (App.Config != null && !App.Config.AnimationsEnabled) return;
            var a = new DoubleAnimation(-2.2, new Duration(TimeSpan.FromMilliseconds(2400 + index * 260)));
            a.AutoReverse = true;
            a.RepeatBehavior = RepeatBehavior.Forever;
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
            var b = new DoubleAnimation(2.2, new Duration(TimeSpan.FromMilliseconds(2400 + index * 260)));
            b.AutoReverse = true;
            b.RepeatBehavior = RepeatBehavior.Forever;
            b.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
            _floatY.BeginAnimation(TranslateTransform.YProperty, a);
            _floatY.BeginAnimation(TranslateTransform.XProperty, b);
        }""")

# springy hover: BackEase instead of cubic in PetalView.AnimateScale
s2 = s2.replace("""        static void AnimateScale(ScaleTransform st, double to, int ms)
        {
            var e = new CubicEase { EasingMode = EasingMode.EaseOut };
            var ax = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            var ay = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, ax);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
        }

        static void Animate(UIElement el, string prop, double to, int ms)""",
"""        static void AnimateScale(ScaleTransform st, double to, int ms)
        {
            var e = new BackEase { Amplitude = 0.22, EasingMode = EasingMode.EaseOut };
            var ax = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            var ay = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, ax);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
        }

        static void Animate(UIElement el, string prop, double to, int ms)""")

io.open(p2, "w", encoding="utf-8").write(s2)
print("petal visuals ok")

# ============ Overlay: start float after entrance ============
p3 = "app/src/OverlayWindow.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace("""            _flower.RefreshToggleStates();
            _flower.PlayIn();""",
"""            _flower.RefreshToggleStates();
            _flower.PlayIn();
            _flower.StartFloats();""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("overlay float ok")

# ============ FlowerMenu.StartFloats ============
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace("""        public void ReleasePulseAt(int index)""",
"""        public void StartFloats()
        {
            for (int i = 0; i < _petals.Count; i++)
                _petals[i].StartFloat(i);
        }

        public void ReleasePulseAt(int index)""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("startfloats ok")

# ============ ActionEngine: shutter sound on screenshots ============
p4 = "app/src/ActionEngine.cs"
s4 = io.open(p4, encoding="utf-8").read()
s4 = s4.replace("""                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    string file = System.IO.Path.Combine(dir, "Hinoki_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                    Toast.Show("Скриншот готов", file, "camera");""",
"""                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    string file = System.IO.Path.Combine(dir, "Hinoki_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                    Sounds.Play("shutter");
                    Toast.Show("Скриншот готов", file, "camera");""")
io.open(p4, "w", encoding="utf-8").write(s4)
print("shutter hook ok")

# ============ Sounds: shutter ============
p5 = "app/src/Sounds.cs"
s5 = io.open(p5, encoding="utf-8").read()
s5 = s5.replace("""                case "toggle_off":
                    tones = new List<double[]> { new double[] { 784, 0.08, 0.45 }, new double[] { 440, 0.12, 0.5 } };
                    break;""",
"""                case "toggle_off":
                    tones = new List<double[]> { new double[] { 784, 0.08, 0.45 }, new double[] { 440, 0.12, 0.5 } };
                    break;
                case "shutter":
                    tones = new List<double[]> { new double[] { 1568, 0.04, 0.6 }, new double[] { 2093, 0.05, 0.5 } };
                    break;""")
s5 = s5.replace('            if (name == "click" || name == "toggle_on" || name == "toggle_off") allow = cfg.SoundClick;',
                '            if (name == "click" || name == "toggle_on" || name == "toggle_off" || name == "shutter") allow = cfg.SoundClick;')
io.open(p5, "w", encoding="utf-8").write(s5)
print("shutter sound ok")

# ============ Settings: version 2.5.0 ============
p6 = "app/src/SettingsWindow.cs"
s6 = io.open(p6, encoding="utf-8").read()
s6 = s6.replace('Glass.Label("v2.4.0", 10, true)', 'Glass.Label("v2.5.0", 10, true)')
s6 = s6.replace('Glass.Label("Версия 2.4.0 · Windows 10/11", 12, true)', 'Glass.Label("Версия 2.5.0 · Windows 10/11", 12, true)')
io.open(p6, "w", encoding="utf-8").write(s6)
print("version ok")
