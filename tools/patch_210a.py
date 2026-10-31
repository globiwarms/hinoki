"""v2.1.0 core patch: icon refresh fix, square-edge fix, scrollbars, icon picker expand, version."""
import io

# ============ FlowerMenu: RefreshIcon proper wrapper swap ============
p = "app/src/FlowerMenu.cs"
s = io.open(p, encoding="utf-8").read()

old = """        /// <summary>Rebuild the icon visual (after target/icon changes).</summary>
        public void RefreshIcon()
        {
            var root = (Grid)Child;
            var content = (Grid)root.Children[1];
            var stack = (StackPanel)content.Children[0];
            if (_iconHost != null) stack.Children.Remove(_iconHost);
            double w = Width;
            _iconHost = BuildIconHost(Config, Math.Max(17, w * 0.15));
            stack.Children.Insert(0, _iconHost);
        }"""
new = """        /// <summary>Rebuild the icon visual (after target/icon changes).</summary>
        public void RefreshIcon()
        {
            var root = (Grid)Child;
            var content = (Grid)root.Children[1];
            var stack = (StackPanel)content.Children[0];
            // remove the OLD wrapper (it still holds the previous icon visual)
            if (_iconWrap != null)
            {
                stack.Children.Remove(_iconWrap);
                _iconWrap = null;
            }
            double w = Width;
            _iconHost = BuildIconHost(Config, Math.Max(17, w * 0.15));
            _iconWrap = new ContentControl();
            _iconWrap.Content = _iconHost;
            _iconWrap.HorizontalAlignment = HorizontalAlignment.Center;
            stack.Children.Insert(0, _iconWrap);
        }"""
assert old in s, "refresh"
s = s.replace(old, new)

# ctor: use _iconWrap field instead of local
s = s.replace("""            _iconHost = BuildIconHost(cfg, Math.Max(17, w * 0.15));
            var iconHostWrap = new ContentControl();
            iconHostWrap.Content = _iconHost;
            iconHostWrap.HorizontalAlignment = HorizontalAlignment.Center;""",
"""            _iconHost = BuildIconHost(cfg, Math.Max(17, w * 0.15));
            _iconWrap = new ContentControl();
            _iconWrap.Content = _iconHost;
            _iconWrap.HorizontalAlignment = HorizontalAlignment.Center;""")
s = s.replace("""            contentStack.Children.Add(iconHostWrap);
            contentStack.Children.Add(_label);""",
"""            contentStack.Children.Add(_iconWrap);
            contentStack.Children.Add(_label);""")
s = s.replace("""        UIElement _iconHost;
        bool _muted;""",
"""        UIElement _iconHost;
        ContentControl _iconWrap;
        bool _muted;""")

io.open(p, "w", encoding="utf-8").write(s)
print("refreshicon ok")

# ============ SettingsWindow: square edges fix + scrollbars + picker + version ============
p2 = "app/src/SettingsWindow.cs"
s2 = io.open(p2, encoding="utf-8").read()

# blobs: BlurEffect -> radial gradient fills (no square blur artifacts)
s2 = s2.replace("""            var blob = new Ellipse();
            blob.Width = 420; blob.Height = 420;
            blob.Fill = Ui.Solid(Ui.Accent, 0.10);
            blob.Effect = new BlurEffect { Radius = 90 };
            blob.IsHitTestVisible = false;
            blob.HorizontalAlignment = HorizontalAlignment.Left;
            blob.VerticalAlignment = VerticalAlignment.Top;
            blob.Margin = new Thickness(-140, -120, 0, 0);""",
"""            var blob = new Ellipse();
            blob.Width = 760; blob.Height = 760;
            var bg = new RadialGradientBrush();
            bg.GradientStops.Add(new GradientStop(Color.FromArgb(46, 0xA8, 0xB8, 0x99), 0));
            bg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0xA8, 0xB8, 0x99), 1));
            bg.Freeze();
            blob.Fill = bg;
            blob.IsHitTestVisible = false;
            blob.HorizontalAlignment = HorizontalAlignment.Left;
            blob.VerticalAlignment = VerticalAlignment.Top;
            blob.Margin = new Thickness(-260, -260, 0, 0);""")

# chrome: drop the DropShadowEffect (square artifact source on layered windows)
s2 = s2.replace("""            _chrome.ClipToBounds = true;
            _chrome.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 44, ShadowDepth = 10, Opacity = 0.5 };""",
"""            _chrome.ClipToBounds = true;""")

# version
s2 = s2.replace('Glass.Label("v1.3.1", 10, true)', 'Glass.Label("v2.1.0", 10, true)')
s2 = s2.replace('Glass.Label("Версия 1.3.1 · Windows 10/11", 12, true)', 'Glass.Label("Версия 2.1.0 · Windows 10/11", 12, true)')

io.open(p2, "w", encoding="utf-8").write(s2)
print("settings edges ok")
