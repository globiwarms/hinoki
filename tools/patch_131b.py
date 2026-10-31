"""v1.3.1 patch B: lucide rendering in petals/tiles/center, muted states, app icons, dropdown search, sounds."""
import io

# ============ FlowerMenu.cs / PetalView rewrite of icon rendering ============
p = "app/src/FlowerMenu.cs"
s = io.open(p, encoding="utf-8").read()

# PetalView: replace icon TextBlock with host that can show lucide path, app icon, or legacy glyph
s = s.replace("""        public readonly ScaleTransform Scale = new ScaleTransform(1, 1);
        Border _shape, _hoverLayer, _ring;
        TextBlock _icon, _label;""",
"""        public readonly ScaleTransform Scale = new ScaleTransform(1, 1);
        Border _shape, _hoverLayer, _ring, _mutedLayer;
        TextBlock _label;
        UIElement _iconHost;
        bool _muted;""")

# helper: build a lucide stroke visual of given size
s = s.replace("""        public event Action<int> Activated;

        /// <summary>Rotate only the glass shape""",
"""        public event Action<int> Activated;

        static UIElement MakeIconVisual(string icon, double size, bool light)
        {
            var vb = new Viewbox();
            vb.Width = size; vb.Height = size;
            var canvas = new Canvas();
            canvas.Width = 24; canvas.Height = 24;
            var path = new System.Windows.Shapes.Path();
            path.Data = Lucide.Get(icon);
            path.Stroke = light ? Ui.Solid(Color.FromRgb(0x24, 0x2B, 0x1E), 0.95) : Ui.Solid(Colors.White, 0.97);
            path.StrokeThickness = 1.5;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            canvas.Children.Add(path);
            vb.Child = canvas;
            return vb;
        }

        UIElement BuildIconHost(PetalConfig cfg, double size)
        {
            // app icon for "Открыть приложение" with a real file target
            if (cfg.Action == "app" && !string.IsNullOrWhiteSpace(cfg.Target))
            {
                string t = cfg.Target.Trim();
                string ext = System.IO.Path.GetExtension(t).ToLowerInvariant();
                if ((ext == ".exe" || ext == ".lnk") && System.IO.File.Exists(Environment.ExpandEnvironmentVariables(t)))
                {
                    var img = new Image();
                    var src = AppIconCache.Get(Environment.ExpandEnvironmentVariables(t));
                    if (src != null) { img.Source = src; img.Width = size; img.Height = size; return img; }
                }
            }
            return MakeIconVisual(cfg.Icon, size, App.Config != null && App.Config.LightGlass);
        }

        /// <summary>Rebuild the icon visual (after target/icon changes).</summary>
        public void RefreshIcon()
        {
            var root = (Grid)Child;
            var content = (Grid)root.Children[1];
            var stack = (StackPanel)content.Children[0];
            if (_iconHost != null) stack.Children.Remove(_iconHost);
            double w = Width;
            _iconHost = BuildIconHost(Config, Math.Max(17, w * 0.15));
            stack.Children.Insert(0, _iconHost);
        }

        /// <summary>Red tint when a toggle action is currently OFF (muted).</summary>
        public void SetMuted(bool muted)
        {
            _muted = muted;
            if (_mutedLayer == null) return;
            Animate(_mutedLayer, "Opacity", muted ? 1 : 0, 260);
        }

        /// <summary>Rotate only the glass shape""")

# _mutedLayer creation: add after _ring creation
s = s.replace("""            _ring = new Border();
            _ring.CornerRadius = new CornerRadius(r);
            _ring.BorderBrush = Ui.Solid(Ui.Accent, 0.9);
            _ring.BorderThickness = new Thickness(2);
            _ring.Opacity = 0;
            _ring.IsHitTestVisible = false;
            grid.Children.Add(_ring);

            shapeHost.Child = grid;""",
"""            _ring = new Border();
            _ring.CornerRadius = new CornerRadius(r);
            _ring.BorderBrush = Ui.Solid(Ui.Accent, 0.9);
            _ring.BorderThickness = new Thickness(2);
            _ring.Opacity = 0;
            _ring.IsHitTestVisible = false;
            grid.Children.Add(_ring);

            _mutedLayer = new Border();
            _mutedLayer.CornerRadius = new CornerRadius(r);
            _mutedLayer.Background = Ui.Solid(Color.FromRgb(0xE5, 0x6C, 0x5C), 0.26);
            _mutedLayer.BorderBrush = Ui.Solid(Color.FromRgb(0xE5, 0x6C, 0x5C), 0.65);
            _mutedLayer.BorderThickness = new Thickness(1.2);
            _mutedLayer.Opacity = 0;
            _mutedLayer.IsHitTestVisible = false;
            grid.Children.Add(_mutedLayer);

            shapeHost.Child = grid;""")

# ctor: replace legacy _icon TextBlock block with icon host
old_icon_block = """            _icon = new TextBlock();
            _icon.Text = Ui.Glyph(cfg.Icon).ToString();
            _icon.FontFamily = Ui.Icons;
            _icon.FontSize = Math.Max(17, w * 0.15);
            _icon.Foreground = light ? Ui.Solid(Color.FromRgb(0x24, 0x2B, 0x1E), 0.95) : Ui.Solid(Colors.White, 0.96);
            _icon.HorizontalAlignment = HorizontalAlignment.Center;"""
new_icon_block = """            _iconHost = BuildIconHost(cfg, Math.Max(17, w * 0.15));
            var iconHostWrap = new ContentControl();
            iconHostWrap.Content = _iconHost;
            iconHostWrap.HorizontalAlignment = HorizontalAlignment.Center;"""
assert old_icon_block in s, "ctor icon"
s = s.replace(old_icon_block, new_icon_block)
s = s.replace("""            contentStack.Children.Add(_icon);
            contentStack.Children.Add(_label);""",
"""            contentStack.Children.Add(iconHostWrap);
            contentStack.Children.Add(_label);""")

# SetData: rebuild visuals
s = s.replace("""        public void SetData(PetalConfig cfg)
        {
            _icon.Text = Ui.Glyph(cfg.Icon).ToString();
            _label.Text = cfg.Label;
        }""",
"""        public void SetData(PetalConfig cfg)
        {
            RefreshIcon();
            _label.Text = cfg.Label;
        }""")

# center: lucide gear, no badge label under circle (badge removed), keep circle only
s = s.replace("""            var gearHost = new Grid();
            var gear = new TextBlock();
            gear.Text = ((char)0xE713).ToString();
            gear.FontFamily = Ui.Icons;
            gear.FontSize = centerD * 0.34;
            gear.Foreground = Ui.Solid(Colors.White, 0.97);
            gear.HorizontalAlignment = HorizontalAlignment.Center;
            gear.VerticalAlignment = VerticalAlignment.Center;
            gearHost.Children.Add(gear);
            _centerCircle.Child = gearHost;""",
"""            var gearHost = new Viewbox();
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
            _centerCircle.Child = gearHost;""")

# remove the "Настройки" name block under the circle
s = s.replace("""            var name = new TextBlock();
            name.Text = "Настройки";
            name.FontFamily = Ui.Text;
            name.FontSize = Math.Max(11, S * 0.023);
            name.FontWeight = FontWeights.SemiBold;
            name.Foreground = Ui.Solid(Colors.White, 0.9);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.Margin = new Thickness(0, 8, 0, 0);
            name.Visibility = cfg.ShowLabels ? Visibility.Visible : Visibility.Collapsed;

            _centerGroup.Children.Add(_centerCircle);
            _centerGroup.Children.Add(name);""",
"""            _centerGroup.Children.Add(_centerCircle);""")

# center group positioned exactly over the circle center
s = s.replace("""            _centerGroup.Width = centerD + S * 0.12;
            SetLeft(_centerGroup, S / 2 - _centerGroup.Width / 2);
            SetTop(_centerGroup, S / 2 - centerD / 2);""",
"""            _centerGroup.Width = centerD;
            SetLeft(_centerGroup, S / 2 - centerD / 2);
            SetTop(_centerGroup, S / 2 - centerD / 2);""")

io.open(p, "w", encoding="utf-8").write(s)
print("flower lucide ok")

# ============ AppIconCache ============
p2 = "app/src/FlowerMenu.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("""    /// <summary>One glass petal with icon + label, hover glow and press feedback.</summary>""",
"""    /// <summary>Extracts and caches application icons for petal targets.</summary>
    public static class AppIconCache
    {
        static readonly Dictionary<string, BitmapImage> Cache = new Dictionary<string, BitmapImage>();

        public static BitmapImage Get(string fullPath)
        {
            BitmapImage bi;
            if (Cache.TryGetValue(fullPath, out bi)) return bi;
            try
            {
                using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(fullPath))
                using (var bmp = icon.ToBitmap())
                using (var ms = new System.IO.MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = new System.IO.MemoryStream(ms.ToArray());
                    bi.EndInit();
                    bi.Freeze();
                }
                Cache[fullPath] = bi;
                return bi;
            }
            catch { return null; }
        }
    }

    /// <summary>One glass petal with icon + label, hover glow and press feedback.</summary>""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("app icon cache ok")
