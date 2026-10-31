// Hinoki — the liquid-glass flower menu (shared by the on-screen overlay and the settings preview)
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Hinoki
{
    /// <summary>One glass petal with icon + label, hover glow and press feedback.</summary>
    public class PetalView : Border
    {
        public readonly int Index;
        public readonly PetalConfig Config;
        public readonly ScaleTransform Scale = new ScaleTransform(1, 1);
        Border _shape, _hoverLayer, _ring;
        TextBlock _icon, _label;

        public PetalView(int index, PetalConfig cfg, double w, double h)
        {
            Index = index; Config = cfg;
            Width = w; Height = h;
            Background = Brushes.Transparent; // hit-test root
            Cursor = Cursors.Hand;

            var cfgApp = App.Config;
            double k = cfgApp != null ? cfgApp.PetalOpacity / 100.0 : 1.0;
            bool light = cfgApp != null && cfgApp.LightGlass;
            double glow = cfgApp != null ? 0.10 + 0.85 * (cfgApp.GlowStrength / 100.0) : 0.4;

            double r = h * 0.48;
            var shapeHost = new Border();
            shapeHost.Width = w; shapeHost.Height = h;
            shapeHost.CornerRadius = new CornerRadius(r);
            shapeHost.Background = light ? Ui.GlassFill(0.72 * k, 0.42 * k) : Ui.GlassFill(0.26 * k, 0.10 * k);
            shapeHost.BorderBrush = Ui.Solid(Colors.White, light ? 0.62 : 0.36);
            shapeHost.BorderThickness = new Thickness(1.2);
            var glowFx = new DropShadowEffect();
            glowFx.Color = light ? Colors.Black : Colors.White;
            glowFx.BlurRadius = 26; glowFx.ShadowDepth = 0; glowFx.Opacity = glow;
            shapeHost.Effect = glowFx;
            _shape = shapeHost;

            var grid = new Grid();

            _hoverLayer = new Border();
            _hoverLayer.CornerRadius = new CornerRadius(r);
            _hoverLayer.Background = Ui.Solid(Colors.White, light ? 0.22 : 0.10);
            _hoverLayer.Opacity = 0;
            _hoverLayer.IsHitTestVisible = false;
            grid.Children.Add(_hoverLayer);

            _ring = new Border();
            _ring.CornerRadius = new CornerRadius(r);
            _ring.BorderBrush = Ui.Solid(Ui.Accent, 0.9);
            _ring.BorderThickness = new Thickness(2);
            _ring.Opacity = 0;
            _ring.IsHitTestVisible = false;
            grid.Children.Add(_ring);

            shapeHost.Child = grid;

            // content layer stays horizontal while the shape rotates
            var content = new Grid();
            content.Background = Brushes.Transparent;
            var contentStack = new StackPanel();
            contentStack.VerticalAlignment = VerticalAlignment.Center;
            contentStack.HorizontalAlignment = HorizontalAlignment.Center;

            _icon = new TextBlock();
            _icon.Text = Ui.Glyph(cfg.Icon).ToString();
            _icon.FontFamily = Ui.Icons;
            _icon.FontSize = Math.Max(17, w * 0.15);
            _icon.Foreground = light ? Ui.Solid(Color.FromRgb(0x24, 0x2B, 0x1E), 0.95) : Ui.Solid(Colors.White, 0.96);
            _icon.HorizontalAlignment = HorizontalAlignment.Center;

            _label = new TextBlock();
            _label.Text = cfg.Label;
            _label.FontFamily = Ui.Text;
            _label.FontSize = Math.Max(11, w * 0.072);
            _label.FontWeight = FontWeights.SemiBold;
            _label.Foreground = light ? Ui.Solid(Color.FromRgb(0x24, 0x2B, 0x1E), 0.92) : Ui.Solid(Colors.White, 0.93);
            _label.HorizontalAlignment = HorizontalAlignment.Center;
            _label.Margin = new Thickness(0, w * 0.05, 0, 0);
            _label.TextTrimming = TextTrimming.CharacterEllipsis;
            _label.MaxWidth = w * 0.84;
            _label.Visibility = (cfgApp != null && !cfgApp.ShowLabels) ? Visibility.Collapsed : Visibility.Visible;

            contentStack.Children.Add(_icon);
            contentStack.Children.Add(_label);
            content.Children.Add(contentStack);

            var root = new Grid();
            root.Children.Add(shapeHost);
            root.Children.Add(content);
            Child = root;

            RenderTransformOrigin = new Point(0.5, 0.5);
            RenderTransform = Scale;

            MouseEnter += delegate { Hover(true); };
            MouseLeave += delegate { Hover(false); };
            MouseLeftButtonDown += delegate
            {
                Press();
                var handler = Activated; if (handler != null) handler(Index);
            };
        }

        public event Action<int> Activated;

        /// <summary>Rotate only the glass shape; the icon and label stay horizontal.
        /// The rotation lives on the inner shape (root keeps a clean center-origin scale),
        /// otherwise RenderTransformOrigin compounds with the group and petals drift off-orbit.</summary>
        public void RotateShape(double theta)
        {
            RenderTransform = Scale; // scale around RenderTransformOrigin (0.5, 0.5)
            // only the inner glass shape rotates — the content layer is separate and stays horizontal
            _shape.RenderTransform = new RotateTransform(theta, Width / 2, Height / 2);
        }

        void Hover(bool on)
        {
            Animate(_hoverLayer, "Opacity", on ? 1 : 0, 200);
            AnimateScale(Scale, on ? 1.035 : 1, 260);
        }

        void Press()
        {
            AnimateScale(Scale, 1.09, 110);
        }

        public void ReleasePulse()
        {
            AnimateScale(Scale, 1.0, 260);
        }

        public void SetSelected(bool sel)
        {
            Animate(_ring, "Opacity", sel ? 1 : 0, 180);
            if (sel) _label.Foreground = Ui.Solid(Ui.Accent, 1);
        }

        public void SetData(PetalConfig cfg)
        {
            _icon.Text = Ui.Glyph(cfg.Icon).ToString();
            _label.Text = cfg.Label;
        }

        public void PlayIn(TimeSpan delay)
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
        }

        static void AnimateScale(ScaleTransform st, double to, int ms)
        {
            var e = new CubicEase { EasingMode = EasingMode.EaseOut };
            var ax = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            var ay = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, ax);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
        }

        static void Animate(UIElement el, string prop, double to, int ms)
        {
            var a = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms)));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            if (prop == "Opacity") el.BeginAnimation(UIElement.OpacityProperty, a);
        }
    }

    /// <summary>The flower: glass petals around a settings-button heart.</summary>
    public class FlowerMenu : Canvas
    {
        public event Action<int> PetalActivated;
        public event Action CenterActivated;

        List<PetalView> _petals = new List<PetalView>();
        Border _centerCircle;
        StackPanel _centerGroup;
        double _size;

        public FlowerMenu(double size)
        {
            _size = size;
            Width = size; Height = size;
            Build();
        }

        static BitmapImage _logoBitmap;
        public static BitmapImage LogoBitmap
        {
            get
            {
                if (_logoBitmap == null)
                {
                    using (var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Hinoki.logo.png"))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = s;
                        bmp.EndInit();
                        bmp.Freeze();
                        _logoBitmap = bmp;
                    }
                }
                return _logoBitmap;
            }
        }

        public void Build()
        {
            Children.Clear();
            _petals.Clear();
            double S = _size;
            var cfg = App.Config;
            int n = cfg.Petals.Count;

            double R = S * 0.335;                                  // orbit radius
            double ph = S * 0.27;                                  // petal height
            // petals never overlap: cap width by the space between neighbours
            double pw = Math.Min(S * 0.30, 2 * R * Math.Sin(Math.PI / n) * 0.90);
            double centerD = S * 0.14;
            bool light = cfg.LightGlass;

            for (int i = 0; i < n; i++)
            {
                double theta = i * 360.0 / n;
                double rad = theta * Math.PI / 180.0;
                double cx = S / 2 + R * Math.Sin(rad);
                double cy = S / 2 - R * Math.Cos(rad);

                var pv = new PetalView(i, cfg.Petals[i], pw, ph);
                SetLeft(pv, cx - pw / 2);
                SetTop(pv, cy - ph / 2);
                pv.RotateShape(theta);
                pv.Activated += OnPetalActivated;
                Children.Add(pv);
                _petals.Add(pv);
            }

            // ---- center: glass settings button ----
            _centerGroup = new StackPanel();
            _centerGroup.HorizontalAlignment = HorizontalAlignment.Center;
            _centerGroup.VerticalAlignment = VerticalAlignment.Top;

            double k = cfg.PetalOpacity / 100.0;
            _centerCircle = new Border();
            _centerCircle.Width = centerD; _centerCircle.Height = centerD;
            _centerCircle.CornerRadius = new CornerRadius(centerD);
            double ck = cfg.CenterOpacity / 100.0;
            _centerCircle.Background = light ? Ui.GlassFill(0.80 * ck, 0.55 * ck) : Ui.GlassFill(0.30 * ck, 0.16 * ck);
            _centerCircle.BorderBrush = Ui.Solid(Colors.White, (light ? 0.7 : 0.5) * ck);
            _centerCircle.BorderThickness = new Thickness(1.4);
            var cg = new DropShadowEffect();
            cg.Color = Colors.White; cg.BlurRadius = 24; cg.ShadowDepth = 0;
            cg.Opacity = 0.18 + 0.5 * (cfg.GlowStrength / 100.0);
            _centerCircle.Effect = cg;
            _centerCircle.Cursor = Cursors.Hand;

            var gearHost = new Grid();
            var gear = new TextBlock();
            gear.Text = ((char)0xE713).ToString();
            gear.FontFamily = Ui.Icons;
            gear.FontSize = centerD * 0.34;
            gear.Foreground = Ui.Solid(Colors.White, 0.97);
            gear.HorizontalAlignment = HorizontalAlignment.Center;
            gear.VerticalAlignment = VerticalAlignment.Center;
            gearHost.Children.Add(gear);
            _centerCircle.Child = gearHost;

            var name = new TextBlock();
            name.Text = "Настройки";
            name.FontFamily = Ui.Text;
            name.FontSize = Math.Max(11, S * 0.023);
            name.FontWeight = FontWeights.SemiBold;
            name.Foreground = Ui.Solid(Colors.White, 0.9);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.Margin = new Thickness(0, 8, 0, 0);
            name.Visibility = cfg.ShowLabels ? Visibility.Visible : Visibility.Collapsed;

            _centerGroup.Children.Add(_centerCircle);
            _centerGroup.Children.Add(name);
            _centerGroup.RenderTransformOrigin = new Point(0.5, 0);
            var cst = new ScaleTransform(1, 1);
            _centerGroup.RenderTransform = cst;
            _centerGroup.MouseEnter += delegate
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
            };
            _centerGroup.MouseLeftButtonDown += delegate
            {
                var h = CenterActivated; if (h != null) h();
            };

            _centerGroup.Width = centerD + S * 0.12;
            SetLeft(_centerGroup, S / 2 - _centerGroup.Width / 2);
            SetTop(_centerGroup, S / 2 - centerD / 2);
            Children.Add(_centerGroup);
        }

        public void UpdatePetal(int index)
        {
            if (index >= 0 && index < _petals.Count)
                _petals[index].SetData(App.Config.Petals[index]);
        }

        public void Rebuild()
        {
            Build();
        }

        public void SetSelected(int index)
        {
            for (int i = 0; i < _petals.Count; i++)
                _petals[i].SetSelected(i == index);
        }

        public void ReleasePulseAt(int index)
        {
            if (index >= 0 && index < _petals.Count) _petals[index].ReleasePulse();
        }

        void OnPetalActivated(int index)
        {
            var h = PetalActivated;
            if (h != null) h(index);
        }

        public void PlayIn()
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
        }

        public void PlayOut(Action done)
        {
            var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(170)));
            fade.Completed += delegate { if (done != null) done(); };
            BeginAnimation(OpacityProperty, fade);
        }

        static void AnimateScale(ScaleTransform st, double to, int ms) { AnimateScale(st, to, ms, 0); }

        static void AnimateScale(ScaleTransform st, double to, int ms, double from)
        {
            var e = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut };
            if (from > 0) { st.ScaleX = from; st.ScaleY = from; }
            var ax = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            var ay = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, ax);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
        }
    }
}
