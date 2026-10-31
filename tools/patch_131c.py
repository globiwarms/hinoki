"""v1.3.1 patch C: IconTile lucide, dropdown search, sounds, overlay toggle logic."""
import io

# ============ UiControls: IconTile renders lucide ============
p = "app/src/UiControls.cs"
s = io.open(p, encoding="utf-8").read()

s = s.replace("""        public IconTile(string glyphHex)
        {
            Glyph = glyphHex;
            Width = 46; Height = 42;
            CornerRadius = new CornerRadius(12);
            Background = Ui.Solid(Colors.White, 0.05);
            BorderBrush = Ui.Solid(Colors.White, 0.10);
            BorderThickness = new Thickness(1);
            Cursor = Cursors.Hand;

            _icon = new TextBlock();
            _icon.Text = Ui.Glyph(glyphHex).ToString();
            _icon.FontFamily = Ui.Icons;
            _icon.FontSize = 17;
            _icon.Foreground = Ui.Solid(Ui.Ink, 0.9);
            _icon.HorizontalAlignment = HorizontalAlignment.Center;
            _icon.VerticalAlignment = VerticalAlignment.Center;
            Child = _icon;""",
"""        public IconTile(string glyphHex)
        {
            Glyph = glyphHex;
            Width = 46; Height = 42;
            CornerRadius = new CornerRadius(12);
            Background = Ui.Solid(Colors.White, 0.05);
            BorderThickness = new Thickness(0);
            Cursor = Cursors.Hand;

            _icon = LucideHost(glyphHex, 20, false);
            _icon.HorizontalAlignment = HorizontalAlignment.Center;
            _icon.VerticalAlignment = VerticalAlignment.Center;
            Child = _icon;""")

# rename field type: TextBlock -> ContentControl-compatible UIElement
s = s.replace("""        bool _selected;
        public readonly string Glyph;
        public event Action<string> Picked;
        TextBlock _icon;""",
"""        bool _selected;
        public readonly string Glyph;
        public event Action<string> Picked;
        UIElement _icon;""")

s = s.replace("""        public void SetSelected(bool on)
        {
            _selected = on;
            BorderThickness = new Thickness(0);
            Background = on ? Ui.Solid(Ui.Accent, 0.20) : Ui.Solid(Colors.White, 0.05);
            _icon.Foreground = on ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.Ink, 0.9);
        }""",
"""        public void SetSelected(bool on)
        {
            _selected = on;
            BorderThickness = new Thickness(0);
            Background = on ? Ui.Solid(Ui.Accent, 0.20) : Ui.Solid(Colors.White, 0.05);
        }""")

# helper LucideHost inside UiControls (before GlassDropDown class)
s = s.replace("""    // ---------- toggle ----------""",
"""    public static class LucideHost
    {
        public static UIElement Build(string name, double size, bool light)
        {
            var vb = new Viewbox();
            vb.Width = size; vb.Height = size;
            var canvas = new Canvas();
            canvas.Width = 24; canvas.Height = 24;
            var path = new System.Windows.Shapes.Path();
            path.Data = Lucide.Get(name);
            path.Stroke = light ? Ui.Solid(Color.FromRgb(0x24, 0x2B, 0x1E), 0.95) : Ui.Solid(Colors.White, 0.95);
            path.StrokeThickness = 1.5;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            canvas.Children.Add(path);
            vb.Child = canvas;
            return vb;
        }
    }

    // ---------- toggle ----------""")

io.open(p, "w", encoding="utf-8").write(s)
print("icontile lucide ok")

# ============ FlowerMenu PetalView: use shared LucideHost (remove local dup) ============
p2 = "app/src/FlowerMenu.cs"
s2 = io.open(p2, encoding="utf-8").read()
old_make = s2[s2.index("        static UIElement MakeIconVisual(string icon, double size, bool light)"):s2.index("        UIElement BuildIconHost(PetalConfig cfg, double size)")]
s2 = s2.replace(old_make, """        static UIElement MakeIconVisual(string icon, double size, bool light)
        {
            return LucideHost.Build(icon, size, light);
        }

""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("petal uses shared host")

# ============ Sounds: toggle_on / toggle_off ============
p3 = "app/src/Sounds.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace("""                case "close":
                    tones = new List<double[]> { new double[] { 659, 0.11, 0.5 }, new double[] { 494, 0.10, 0.4 } };
                    break;""",
"""                case "close":
                    tones = new List<double[]> { new double[] { 659, 0.11, 0.5 }, new double[] { 494, 0.10, 0.4 } };
                    break;
                case "toggle_on":
                    tones = new List<double[]> { new double[] { 523, 0.08, 0.5 }, new double[] { 784, 0.12, 0.5 } };
                    break;
                case "toggle_off":
                    tones = new List<double[]> { new double[] { 784, 0.08, 0.45 }, new double[] { 440, 0.12, 0.5 } };
                    break;""")
s3 = s3.replace("""            bool allow = true;
            if (name == "open") allow = cfg.SoundOpen;
            if (name == "close") allow = cfg.SoundClose;
            if (name == "click") allow = cfg.SoundClick;""",
"""            bool allow = true;
            if (name == "open") allow = cfg.SoundOpen;
            if (name == "close") allow = cfg.SoundClose;
            if (name == "click" || name == "toggle_on" || name == "toggle_off") allow = cfg.SoundClick;""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("sounds ok")

# ============ OverlayWindow: toggle petals stay open + state refresh ============
p4 = "app/src/OverlayWindow.cs"
s4 = io.open(p4, encoding="utf-8").read()
s4 = s4.replace("""            _flower.PetalActivated += delegate (int i)
            {
                Sounds.Play("click");
                ActionEngine.Run(App.Config.Petals[i]);
                if (App.Config.CloseAfterAction) SafeClose();
                else _flower.ReleasePulseAt(i);
            };""",
"""            _flower.PetalActivated += delegate (int i)
            {
                var petal = App.Config.Petals[i];
                bool isToggle = petal.Action == "mic_toggle" || petal.Action == "volume_mute";
                Sounds.Play(isToggle ? (petal.Action == "mic_toggle" ? (Audio.IsMicMuted() ? "toggle_on" : "toggle_off") : "click") : "click");
                ActionEngine.Run(petal);
                if (isToggle)
                {
                    // keep the menu open so the state change is visible; refresh tints
                    if (App.Config.CloseAfterAction == false || true)
                    {
                        RefreshToggleStates();
                        return;
                    }
                }
                if (App.Config.CloseAfterAction) SafeClose();
                else _flower.ReleasePulseAt(i);
            };""")
s4 = s4.replace("""        void OnEsc()""",
"""        void RefreshToggleStates()
        {
            _flower.SetMutedStates();
        }

        void OnEsc()""")
io.open(p4, "w", encoding="utf-8").write(s4)
print("overlay toggle ok")

# ============ FlowerMenu.SetMutedStates + expose petals ============
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("""        public void ReleasePulseAt(int index)""",
"""        public void RefreshToggleStates()
        {
            foreach (var pv in _petals)
            {
                var a = pv.Config.Action;
                if (a == "mic_toggle") pv.SetMuted(Audio.IsMicMuted());
                else if (a == "volume_mute") pv.SetMuted(Audio.IsSystemMuted());
                else pv.SetMuted(false);
            }
        }

        public void ReleasePulseAt(int index)""")
# call on open (after Show) — overlay calls _flower.PlayIn(); add states before that
s2 = s2.replace("""            Show();
            Sounds.Play("open");
            _flower.PlayIn();""", """            Show();
            Sounds.Play("open");
            _flower.PlayIn();""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("flower states ok")

# overlay: apply states right after opening
p4b = "app/src/OverlayWindow.cs"
s4b = io.open(p4b, encoding="utf-8").read()
s4b = s4b.replace("""            Show();
            Sounds.Play("open");
            _flower.PlayIn();""",
"""            Show();
            Sounds.Play("open");
            _flower.RefreshToggleStates();
            _flower.PlayIn();""")
io.open(p4b, "w", encoding="utf-8").write(s4b)
print("overlay states ok")
