"""v3.0.0 patch D: petals page 3-column, petal color swatches, per-petal hotkey, tinted glass."""
import io

# ============ Ui: tinted glass helper + hex parse ============
p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()
s = s.replace("""        public static char Glyph(string hex)""",
"""        public static bool TryParseHex(string hex, out Color color)
        {
            color = Colors.White;
            try
            {
                if (string.IsNullOrEmpty(hex) || hex.Length != 6) return false;
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                color = Color.FromRgb(r, g, b);
                return true;
            }
            catch { return false; }
        }

        public static char Glyph(string hex)""")
io.open(p, "w", encoding="utf-8").write(s)
print("ui hex ok")

# ============ PetalView: per-petal tint ============
p2 = "app/src/FlowerMenu.cs"
s2 = io.open(p2, encoding="utf-8").read()

s2 = s2.replace("""        UIElement _iconHost;
        ContentControl _iconWrap;
        bool _muted;""",
"""        UIElement _iconHost;
        ContentControl _iconWrap;
        bool _muted;
        Border _shapeRef;""")

s2 = s2.replace("""        public void SetData(PetalConfig cfg)
        {
            RefreshIcon();
            _label.Text = cfg.Label;
        }""",
"""        public void SetData(PetalConfig cfg)
        {
            RefreshIcon();
            RefreshTint(cfg);
            _label.Text = cfg.Label;
        }

        /// <summary>Apply per-petal tint color ("" = default glass).</summary>
        public void RefreshTint(PetalConfig cfg)
        {
            var cfgApp = App.Config;
            double k = cfgApp != null ? cfgApp.PetalOpacity / 100.0 : 1.0;
            bool light = cfgApp != null && cfgApp.LightGlass;
            Color tint;
            bool custom = Ui.TryParseHex(cfg.Color, out tint);
            if (_shapeRef == null) return;
            if (custom)
            {
                var g = new LinearGradientBrush();
                g.StartPoint = new Point(0, 0); g.EndPoint = new Point(0, 1);
                g.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(115 * k), tint.R, tint.G, tint.B), 0));
                g.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(60 * k), tint.R, tint.G, tint.B), 1));
                g.Freeze();
                _shapeRef.Background = g;
                _shapeRef.BorderBrush = Ui.Solid(tint, 0.62);
            }
            else
            {
                _shapeRef.Background = light ? Ui.GlassFill(0.72 * k, 0.42 * k) : Ui.GlassFill(0.26 * k, 0.10 * k);
                _shapeRef.BorderBrush = Ui.Solid(Colors.White, light ? 0.62 : 0.36);
            }
        }""")

# store _shapeRef in ctor
s2 = s2.replace("""            _shape = shapeHost;

            var grid = new Grid();""",
"""            _shape = shapeHost;
            _shapeRef = shapeHost;

            var grid = new Grid();""")

# SetSelected recolor: keep icon path accent — the path is inside content stack now; find via content
s2 = s2.replace("""        public void SetSelected(bool sel)
        {
            Animate(_ring, "Opacity", sel ? 1 : 0, 180);
            if (sel) _label.Foreground = Ui.Solid(Ui.Accent, 1);
        }""",
"""        public void SetSelected(bool sel)
        {
            Animate(_ring, "Opacity", sel ? 1 : 0, 180);
            if (sel) _label.Foreground = Ui.Solid(Ui.Accent, 1);
        }""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("petal tint ok")

# ============ SettingsWindow: 3-column petals + color swatches + hotkey ============
p3 = "app/src/SettingsWindow.cs"
s3 = io.open(p3, encoding="utf-8").read()

s3 = s3.replace('Glass.Label("v2.6.0", 10, true)', 'Glass.Label("v3.0.0", 10, true)')
s3 = s3.replace('Glass.Label("Версия 2.6.0 · Windows 10/11", 12, true)', 'Glass.Label("Версия 3.0.0 · Windows 10/11", 12, true)')

# 3 columns: list | preview | editor
s3 = s3.replace("""            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(228) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _petalList = new StackPanel();""",
"""            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(196) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(272) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _petalList = new StackPanel();""")

# preview moves to its own middle column
s3 = s3.replace("""            var previewWrap = new Border();
            previewWrap.CornerRadius = new CornerRadius(15);
            previewWrap.Background = Ui.Solid(Colors.White, 0.03);
            previewWrap.BorderBrush = Ui.Solid(Colors.White, 0.07);
            previewWrap.BorderThickness = new Thickness(1);
            previewWrap.Margin = new Thickness(0, 12, 0, 0);
            var vb = new Viewbox();
            vb.Stretch = Stretch.Uniform;
            vb.Margin = new Thickness(8, 4, 8, 8);
            _preview = new FlowerMenu(300);""",
"""            var previewWrap = new Border();
            previewWrap.CornerRadius = new CornerRadius(15);
            previewWrap.Background = Ui.Solid(Colors.White, 0.03);
            previewWrap.BorderBrush = Ui.Solid(Colors.White, 0.07);
            previewWrap.BorderThickness = new Thickness(1);
            previewWrap.Margin = new Thickness(0, 12, 0, 0);
            var vb = new Viewbox();
            vb.Stretch = Stretch.Uniform;
            vb.Margin = new Thickness(8, 4, 8, 8);
            _preview = new FlowerMenu(280);""")
s3 = s3.replace("""            previewWrap.Child = vb;
            Grid.SetRow(previewWrap, 1);
            left.Children.Add(previewWrap);

            var btnRow = new StackPanel();""",
"""            previewWrap.Child = vb;
            Grid.SetRow(previewWrap, 1);
            left.Children.Add(previewWrap);

            var btnRow = new StackPanel();""")
# move previewWrap out of left grid into middle column: change Grid.SetColumn(left, 0) area
s3 = s3.replace("""            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            _editor = new StackPanel();
            var editorScroll = Scroller(_editor);
            Grid.SetColumn(editorScroll, 2);
            grid.Children.Add(editorScroll);""",
"""            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            // middle: live preview column
            var mid = new Grid();
            mid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var pvWrap = new Border();
            pvWrap.CornerRadius = new CornerRadius(15);
            pvWrap.Background = Ui.Solid(Colors.White, 0.03);
            pvWrap.BorderBrush = Ui.Solid(Colors.White, 0.07);
            pvWrap.BorderThickness = new Thickness(1);
            var pvb = new Viewbox();
            pvb.Stretch = Stretch.Uniform;
            pvb.Margin = new Thickness(8, 4, 8, 8);
            var pvSmall = new FlowerMenu(240);
            pvSmall.PetalActivated += delegate (int i)
            {
                _selPetal = i;
                _preview.SetSelected(_selPetal);
                RefreshPetalList();
                FillEditor();
            };
            pvSmall.CenterActivated += delegate { _preview.PlayIn(); };
            pvb.Child = pvSmall;
            pvWrap.Child = pvb;
            Grid.SetRow(pvWrap, 0);
            mid.Children.Add(pvWrap);
            Grid.SetColumn(mid, 2);
            grid.Children.Add(mid);

            _editor = new StackPanel();
            var editorScroll = Scroller(_editor);
            Grid.SetColumn(editorScroll, 4);
            grid.Children.Add(editorScroll);""")

# color swatches + hotkey in editor
s3 = s3.replace("""            _editor.Children.Add(Section("Действие"));""",
"""            _editor.Children.Add(Section("Цвет лепестка"));
            var colorRow = new WrapPanel();
            colorRow.Margin = new Thickness(0, 2, 0, 0);
            string[] colors = { "", "E56C5C", "E8A855", "E8D555", "7FD8BE", "5CB8E5", "7F9FE0", "B48CE0", "E08CC8", "96A182", "6B7280" };
            foreach (var c in colors)
            {
                var sw = new Border();
                sw.Width = 26; sw.Height = 26;
                sw.CornerRadius = new CornerRadius(13);
                sw.Margin = new Thickness(0, 0, 8, 8);
                sw.Cursor = Cursors.Hand;
                bool cur = (cfg.Color ?? "") == c;
                if (c == "")
                {
                    sw.Background = Ui.GlassFill(0.3, 0.15);
                    sw.BorderBrush = Ui.Solid(Colors.White, cur ? 0.95 : 0.2);
                    sw.BorderThickness = new Thickness(cur ? 2.5 : 1);
                }
                else
                {
                    Color cc;
                    Ui.TryParseHex(c, out cc);
                    sw.Background = Ui.Solid(cc, 0.85);
                    sw.BorderBrush = Ui.Solid(Colors.White, cur ? 0.95 : 0.1);
                    sw.BorderThickness = new Thickness(cur ? 2.5 : 0);
                }
                var cCopy = c;
                sw.MouseLeftButtonDown += delegate
                {
                    App.Config.Petals[_selPetal].Color = cCopy;
                    App.SaveConfig();
                    _preview.UpdatePetal(_selPetal);
                    Pulse();
                };
                colorRow.Children.Add(sw);
            }
            _editor.Children.Add(colorRow);
            _editor.Children.Add(Note("Первый вариант — прозрачное стекло как у всех. Остальные — свой цвет лепестка."));

            _editor.Children.Add(Section("Горячая клавиша"));
            var hk = new HotkeyBox(p.Hotkey ?? "");
            hk.Margin = new Thickness(0, 2, 0, 0);
            hk.Changed += delegate
            {
                App.Config.Petals[_selPetal].Hotkey = hk.Combo;
                App.SaveConfig();
                App.ApplyPetalHotkeys();
                Pulse();
            };
            _editor.Children.Add(hk);
            _editor.Children.Add(Note("Глобальное сочетание — нажмёте в любом месте, лепесток сработает."));

            _editor.Children.Add(Section("Действие"));""")

io.open(p3, "w", encoding="utf-8").write(s3)
print("settings v3 layout ok")
