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
using System.Windows.Shapes;

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

            double r = h * 0.48;
            var shapeHost = new Border();
            shapeHost.Width = w; shapeHost.Height = h;
            shapeHost.CornerRadius = new CornerRadius(r);
            shapeHost.Background = Ui.GlassFill(0.26, 0.10);
            shapeHost.BorderBrush = Ui.Solid(Colors.White, 0.38);
            shapeHost.BorderThickness = new Thickness(1.4);
            shapeHost.Effect = Glow(0.30);
            _shape = shapeHost;

            var grid = new Grid();

            _hoverLayer = new Border();
            _hoverLayer.CornerRadius = new CornerRadius(r);
            _hoverLayer.Background = Ui.Solid(Colors.White, 0.10);
            _hoverLayer.Opacity = 0;
            _hoverLayer.IsHitTestVisible = false;
            grid.Children.Add(_hoverLayer);

            _ring = new Border();
            _ring.CornerRadius = new CornerRadius(r);
            _ring.BorderBrush = Ui.Solid(Ui.Accent, 0.95);
            _ring.BorderThickness = new Thickness(2.4);
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
            _icon.FontSize = Math.Max(18, w * 0.155);
            _icon.Foreground = Ui.Solid(Colors.White, 0.96);
            _icon.HorizontalAlignment = HorizontalAlignment.Center;

            _label = new TextBlock();
            _label.Text = cfg.Label;
            _label.FontFamily = Ui.Text;
            _label.FontSize = Math.Max(11.5, w * 0.075);
            _label.FontWeight = FontWeights.SemiBold;
            _label.Foreground = Ui.Solid(Colors.White, 0.93);
            _label.HorizontalAlignment = HorizontalAlignment.Center;
            _label.Margin = new Thickness(0, w * 0.055, 0, 0);
            _label.TextTrimming = TextTrimming.CharacterEllipsis;
            _label.MaxWidth = w * 0.8;

            contentStack.Children.Add(_icon);
            contentStack.Children.Add(_label);
            content.Children.Add(contentStack);

            var root = new Grid();
            root.Children.Add(shapeHost);
            root.Children.Add(content);
            Child = root;

            RenderTransformOrigin = new Point(0.5, 0.5);
            RenderTransform = Scale;

                        Cursor = Cursors.Hand;
            MouseEnter += delegate { Hover(true); };
            MouseLeave += delegate { Hover(false); };
            MouseLeftButtonDown += delegate
            {
                Press();
                var handler = Activated; if (handler != null) handler(Index);
            };
        }

        public event Action<int> Activated;

        /// <summary>Rotate only the glass shape; the icon and label stay horizontal.</summary>
        public void RotateShape(double theta)
        {
            var g = new TransformGroup();
            g.Children.Add(new RotateTransform(theta, Width / 2, Height / 2));
            _shape.RenderTransform = g;
        }

        DropShadowEffect Glow(double opacity)
        {
            var e = new DropShadowEffect();
            e.Color = Colors.White;
            e.BlurRadius = 26;
            e.ShadowDepth = 0;
            e.Opacity = opacity;
            return e;
        }

        void Hover(bool on)
        {
            Animate(_hoverLayer, "Opacity", on ? 1 : 0, 140);
            AnimateScale(Scale, on ? 1.045 : 1, 170);
            var glow = (DropShadowEffect)_shape.Effect;
            var a = new DoubleAnimation(on ? 0.55 : 0.30, new Duration(TimeSpan.FromMilliseconds(170)));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, a);
        }

        void Press()
        {
            AnimateScale(Scale, 1.10, 90);
        }

        public void ReleasePulse()
        {
            AnimateScale(Scale, 1.0, 200);
        }

        public void SetSelected(bool sel)
        {
            Animate(_ring, "Opacity", sel ? 1 : 0, 160);
            if (sel) _label.Foreground = Ui.Solid(Ui.Accent, 1);
        }

        public void SetData(PetalConfig cfg)
        {
            _icon.Text = Ui.Glyph(cfg.Icon).ToString();
            _label.Text = cfg.Label;
        }

        public void PlayIn(TimeSpan delay)
        {
            Scale.ScaleX = 0.4; Scale.ScaleY = 0.4;
            Opacity = 0;
            var t = new System.Windows.Threading.DispatcherTimer { Interval = delay };
            t.Tick += delegate
            {
                t.Stop();
                var e = new BackEase { Amplitude = 0.55, EasingMode = EasingMode.EaseOut };
                var gx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(400))) { EasingFunction = e };
                var gy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(400))) { EasingFunction = e };
                Scale.BeginAnimation(ScaleTransform.ScaleXProperty, gx);
                Scale.BeginAnimation(ScaleTransform.ScaleYProperty, gy);
                var go = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(160)));
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

    /// <summary>The full flower: glass petals around a logo center that opens settings.</summary>
    public class FlowerMenu : Canvas
    {
        public event Action<int> PetalActivated;
        public event Action CenterActivated;

        List<PetalView> _petals = new List<PetalView>();
        Border _centerCircle;
        StackPanel _centerGroup;
        Image _logo;
        TextBlock _name, _badgeText;
        Border _badge;
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

            double pw = S * 0.34, ph = S * 0.30, R = S * 0.30;
            double centerD = S * 0.165;

            for (int i = 0; i < n; i++)
            {
                double theta = i * 360.0 / n; // degrees, 0 = 12 o'clock, clockwise
                double rad = theta * Math.PI / 180.0;
                double cx = S / 2 + R * Math.Sin(rad);
                double cy = S / 2 - R * Math.Cos(rad);

                var pv = new PetalView(i, cfg.Petals[i], pw, ph);
                SetLeft(pv, cx - pw / 2);
                SetTop(pv, cy - ph / 2);
                // radial shape, horizontal content (like the reference)
                pv.RotateShape(theta);
                pv.Activated += OnPetalActivated;
                Children.Add(pv);
                _petals.Add(pv);
            }

            // ---- center: logo + name + "Настройки" badge ----
            _centerGroup = new StackPanel();
            _centerGroup.HorizontalAlignment = HorizontalAlignment.Center;
            _centerGroup.VerticalAlignment = VerticalAlignment.Top;

            _centerCircle = new Border();
            _centerCircle.Width = centerD; _centerCircle.Height = centerD;
            _centerCircle.CornerRadius = new CornerRadius(centerD);
            _centerCircle.Background = Ui.GlassFill(0.26, 0.14);
            _centerCircle.BorderBrush = Ui.Solid(Colors.White, 0.5);
            _centerCircle.BorderThickness = new Thickness(1.6);
            var cg = new DropShadowEffect();
            cg.Color = Colors.White; cg.BlurRadius = 22; cg.ShadowDepth = 0; cg.Opacity = 0.35;
            _centerCircle.Effect = cg;

            _logo = new Image();
            _logo.Source = LogoBitmap;
            _logo.Width = centerD * 0.56;
            _logo.Height = centerD * 0.56;
            _logo.HorizontalAlignment = HorizontalAlignment.Center;
            _logo.VerticalAlignment = VerticalAlignment.Center;
            _logo.RenderTransformOrigin = new Point(0.5, 0.5);
            _centerCircle.Child = _logo;

            _name = new TextBlock();
            _name.Text = "Hinoki";
            _name.FontFamily = Ui.Text;
            _name.FontSize = Math.Max(13, S * 0.028);
            _name.FontWeight = FontWeights.SemiBold;
            _name.Foreground = Ui.Solid(Colors.White, 0.97);
            _name.HorizontalAlignment = HorizontalAlignment.Center;
            _name.Margin = new Thickness(0, 10, 0, 0);

            _badge = new Border();
            _badge.HorizontalAlignment = HorizontalAlignment.Center;
            _badge.Margin = new Thickness(0, 7, 0, 0);
            _badge.CornerRadius = new CornerRadius(10);
            _badge.Background = Ui.Solid(Ui.Accent, 0.92);
            _badge.Padding = new Thickness(11, 3.5, 11, 5.5);
            _badgeText = new TextBlock();
            _badgeText.Text = "Настройки";
            _badgeText.FontFamily = Ui.Text;
            _badgeText.FontSize = Math.Max(10.5, S * 0.0195);
            _badgeText.FontWeight = FontWeights.Medium;
            _badgeText.Foreground = Ui.Solid(Color.FromRgb(0x1B, 0x22, 0x16), 1);
            _badge.Child = _badgeText;

            _centerGroup.Children.Add(_centerCircle);
            _centerGroup.Children.Add(_name);
            _centerGroup.Children.Add(_badge);
            _centerGroup.RenderTransformOrigin = new Point(0.5, 0);
            var cst = new ScaleTransform(1, 1);
            _centerGroup.RenderTransform = cst;
            _centerGroup.Cursor = Cursors.Hand;
            _centerGroup.MouseEnter += delegate
            {
                AnimateScale(cst, 1.03, 170);
                var a = new DoubleAnimation(0.55, new Duration(TimeSpan.FromMilliseconds(170)));
                cg.BeginAnimation(DropShadowEffect.OpacityProperty, a);
            };
            _centerGroup.MouseLeave += delegate
            {
                AnimateScale(cst, 1, 170);
                var a = new DoubleAnimation(0.35, new Duration(TimeSpan.FromMilliseconds(170)));
                cg.BeginAnimation(DropShadowEffect.OpacityProperty, a);
            };
            _centerGroup.MouseLeftButtonDown += delegate
            {
                var h = CenterActivated; if (h != null) h();
            };

            _centerGroup.Width = centerD + S * 0.10;
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
            var fade = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(150)));
            BeginAnimation(OpacityProperty, fade);

            var cst = (ScaleTransform)_centerGroup.RenderTransform;
            _centerGroup.Opacity = 0;
            var cgo = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(180)));
            _centerGroup.BeginAnimation(OpacityProperty, cgo);
            AnimateScale(cst, 1, 340, 0.55);

            for (int i = 0; i < _petals.Count; i++)
                _petals[i].PlayIn(TimeSpan.FromMilliseconds(55 + i * 48));
        }

        public void PlayOut(Action done)
        {
            var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(140)));
            fade.Completed += delegate { if (done != null) done(); };
            BeginAnimation(OpacityProperty, fade);
        }

        static void AnimateScale(ScaleTransform st, double to, int ms) { AnimateScale(st, to, ms, 0); }

        static void AnimateScale(ScaleTransform st, double to, int ms, double from)
        {
            var e = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut };
            if (from > 0) { st.ScaleX = from; st.ScaleY = from; }
            var ax = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            var ay = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = e };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, ax);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, ay);
        }
    }
}
