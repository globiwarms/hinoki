"""v1.3.1 patch D: settings lucide rail, no palette, dropdown search, entrance styles, version."""
import io

# ============ SettingsWindow ============
p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# rail icons -> lucide
s = s.replace("""            AddRailItem(nav, 0xE713, "Лепестки", "petals");
            AddRailItem(nav, 0xE790, "Внешний вид", "look");
            AddRailItem(nav, 0xE767, "Звуки", "sounds");
            AddRailItem(nav, 0xE70F, "Поведение", "behavior");
            AddRailItem(nav, 0xE946, "О программе", "about");""",
"""            AddRailItem(nav, "settings", "Лепестки", "petals");
            AddRailItem(nav, "palette", "Внешний вид", "look");
            AddRailItem(nav, "volume-2", "Звуки", "sounds");
            AddRailItem(nav, "sliders-horizontal", "Поведение", "behavior");
            AddRailItem(nav, "info", "О программе", "about");""")

# rail item signature: lucide name
s = s.replace("""        void AddRailItem(StackPanel nav, int glyph, string label, string key)""",
"""        void AddRailItem(StackPanel nav, string glyph, string label, string key)""")
s = s.replace("""            var ic = new TextBlock();
            ic.Text = ((char)glyph).ToString();
            ic.FontFamily = Ui.Icons;
            ic.FontSize = 14.5;
            ic.Foreground = key == "petals" ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
            ic.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(ic);""",
"""            var ic = LucideHost.Build(glyph, 15, false);
            var icHost = new ContentControl();
            icHost.Content = ic;
            icHost.Width = 16; icHost.Height = 16;
            icHost.VerticalAlignment = VerticalAlignment.Center;
            _railGlyphs.Add(icHost);
            sp.Children.Add(icHost);""")

s = s.replace("""        List<Border> _rail = new List<Border>();""",
"""        List<Border> _rail = new List<Border>();
        List<ContentControl> _railGlyphs = new List<ContentControl>();""")

# rail highlight recolors lucide stroke
s = s.replace("""                _rail[i].Background = active ? Ui.Solid(Ui.Accent, 0.22) : Brushes.Transparent;
                var ic = (TextBlock)((StackPanel)_rail[i].Child).Children[0];
                ic.Foreground = active ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);""",
"""                _rail[i].Background = active ? Ui.Solid(Ui.Accent, 0.22) : Brushes.Transparent;
                _railGlyphs[i].Content = LucideHost.Build(RailIcon(keys[i]), 15, false);
                var host = (ContentControl)((StackPanel)_rail[i].Child).Children[0];
                var pathEl = (System.Windows.Shapes.Path)((Canvas)((Viewbox)host.Content).Child).Children[0];
                pathEl.Stroke = active ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);""")
s = s.replace("""            item.MouseEnter += delegate { if (_page != keyCopy) item.Background = Ui.Solid(Colors.White, 0.05); };""",
"""            item.MouseEnter += delegate { if (_page != keyCopy) item.Background = Ui.Solid(Colors.White, 0.05); };""")

# helper for rail icon names
s = s.replace("""        void AddRailItem(StackPanel nav, string glyph, string label, string key)""",
"""        string RailIcon(string key)
        {
            switch (key)
            {
                case "look": return "palette";
                case "sounds": return "volume-2";
                case "behavior": return "sliders-horizontal";
                case "about": return "info";
                default: return "settings";
            }
        }

        void AddRailItem(StackPanel nav, string glyph, string label, string key)""")

# petal list icon -> lucide mini
s = s.replace("""                var ic = new TextBlock();
                ic.Text = Ui.Glyph(p.Icon).ToString();
                ic.FontFamily = Ui.Icons;
                ic.FontSize = 13.5;
                ic.Foreground = sel ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
                ic.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(ic, 0);
                g.Children.Add(ic);""",
"""                var ic = LucideHost.Build(p.Action == "app" && !string.IsNullOrEmpty(p.Target) ? "app-window" : p.Icon, 14, false);
                var icHost = new ContentControl();
                icHost.Content = ic;
                icHost.Width = 15; icHost.Height = 15;
                icHost.VerticalAlignment = VerticalAlignment.Center;
                var icBorder = new Border();
                icBorder.Child = icHost;
                Grid.SetColumn(icBorder, 0);
                g.Children.Add(icBorder);
                System.Action tint = delegate
                {
                    var pathEl = (System.Windows.Shapes.Path)((Canvas)((Viewbox)icHost.Content).Child).Children[0];
                    pathEl.Stroke = sel ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
                };
                tint();""")

# remove accent palette section (kept fixed sage accent)
start = s.index('            stack.Children.Add(Section("Акцентный цвет"));')
end = s.index('            stack.Children.Add(Section("Меню"));')
s = s[:start] + s[end:]

# entrance dropdown: 5 styles
s = s.replace("""            var entranceItems = new List<string> { "Пружина (живой)", "Плавно", "Мгновенно" };
            var entranceMap = new[] { "spring", "smooth", "instant" };
            int curEntr = Array.IndexOf(entranceMap, cfgApp.EntranceStyle);""",
"""            var entranceItems = new List<string> { "Пружина (живой)", "Плавно", "Эластик", "Отскок", "Мгновенно" };
            var entranceMap = new[] { "spring", "smooth", "elastic", "bounce", "instant" };
            int curEntr = Array.IndexOf(entranceMap, cfgApp.EntranceStyle);""")

# version strings
s = s.replace('Glass.Label("v1.3.0", 10, true)', 'Glass.Label("v1.3.1", 10, true)')
s = s.replace('Glass.Label("Версия 1.3.0 · Windows 10/11", 12, true)', 'Glass.Label("Версия 1.3.1 · Windows 10/11", 12, true)')

# remove stale discord hint cases
s = s.replace("""                case "discord_mute":
                    _editor.Children.Add(Note("Поднимет Discord и отправит «" + HotkeyBox.PrettyCombo(App.Config.DiscordMuteKeys) + "». Сочетание — во вкладке «Поведение»."));
                    break;
                case "discord_deafen":
                    _editor.Children.Add(Note("Поднимет Discord и отправит «" + HotkeyBox.PrettyCombo(App.Config.DiscordDeafenKeys) + "». Сочетание — во вкладке «Поведение»."));
                    break;
""", "")
s = s.replace("""                case "mic_toggle": """, "")  # no-op guard
s = s.replace("""                case "media":
                    _editor.Children.Add(Note("Play / Pause для Spotify, YouTube Music и других плееров."));
                    break;""",
"""                case "mic_toggle":
                    _editor.Children.Add(Note("Общий микрофон системы. Нажмите на лепесток — состояние переключится, лепесток подсветится красным, пока микрофон выключен."));
                    break;
                case "volume_mute":
                    _editor.Children.Add(Note("Общий звук системы. Лепесток подсвечивается красным, пока звук выключен."));
                    break;
                case "media":
                    _editor.Children.Add(Note("Play / Pause для Spotify, YouTube Music и других плееров."));
                    break;""")

# fill editor: after choosing app target, refresh petal icon live (already via UpdatePetal -> RefreshIcon)

# drop unused fields warnings
s = s.replace("        GlassDropDown _actionDrop;\n", "")
io.open(p, "w", encoding="utf-8").write(s)
print("settings d1 ok")

# ============ FlowerMenu: elastic/bounce + rail key map helper ============
p2 = "app/src/FlowerMenu.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("""                IEasingFunction e;
                if (style == "smooth") e = new CubicEase { EasingMode = EasingMode.EaseOut };
                else e = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };""",
"""                IEasingFunction e;
                if (style == "smooth") e = new CubicEase { EasingMode = EasingMode.EaseOut };
                else if (style == "elastic") e = new ElasticEase { Oscillations = 2, Springiness = 3.2, EasingMode = EasingMode.EaseOut };
                else if (style == "bounce") e = new BounceEase { Bounces = 2, Bounciness = 2.4, EasingMode = EasingMode.EaseOut };
                else e = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("entrance styles ok")

# ============ GlassDropDown: search inside popup ============
p3 = "app/src/UiControls.cs"
s3 = io.open(p3, encoding="utf-8").read()
old_popup_body = s3[s3.index("        void OpenPopup()"):s3.index("    }\n\n    // ---------- slider ----------")]
new_popup_body = """        void OpenPopup()
        {
            if (_popup != null && _popup.IsOpen) { _popup.IsOpen = false; return; }
            var popup = new Popup();
            popup.PlacementTarget = this;
            popup.Placement = PlacementMode.Bottom;
            popup.StaysOpen = false;
            popup.AllowsTransparency = true;
            popup.PopupAnimation = PopupAnimation.Fade;

            var root = new StackPanel();

            // search box
            var searchBox = new TextBox();
            searchBox.FontFamily = Ui.Text;
            searchBox.FontSize = 13;
            searchBox.Foreground = Ui.Solid(Ui.Ink, 0.97);
            searchBox.CaretBrush = Ui.Solid(Ui.Accent, 1);
            searchBox.Background = Ui.Solid(Colors.White, 0.06);
            searchBox.BorderBrush = Ui.Solid(Colors.White, 0.10);
            searchBox.BorderThickness = new Thickness(1);
            searchBox.Padding = new Thickness(9, 6, 9, 7);
            searchBox.Margin = new Thickness(6, 6, 6, 4);

            var list = new StackPanel();
            list.MinWidth = Math.Max(MinWidth, ActualWidth);

            Action refreshSizes = delegate { list.MinWidth = Math.Max(MinWidth, ActualWidth); };

            Action<string> rebuild = delegate(string query)
            {
                list.Children.Clear();
                string q = (query ?? "").Trim().ToLowerInvariant();
                for (int i = 0; i < _items.Count; i++)
                {
                    if (q.Length > 0 && !_items[i].ToLowerInvariant().Contains(q)) continue;
                    int idx = i;
                    var row = new Border();
                    row.Padding = new Thickness(10, 7, 10, 8);
                    row.CornerRadius = new CornerRadius(9);
                    row.Margin = new Thickness(5, 2, 5, 2);
                    row.Cursor = Cursors.Hand;
                    var wrap = new StackPanel();
                    wrap.Orientation = Orientation.Horizontal;
                    var mark = new TextBlock();
                    mark.Text = idx == _index ? ((char)0xE73E).ToString() : " ";
                    mark.FontFamily = Ui.Icons;
                    mark.FontSize = 11;
                    mark.Width = 18;
                    mark.Foreground = Ui.Solid(Ui.Accent, 1);
                    wrap.Children.Add(mark);
                    var t = new TextBlock();
                    t.Text = _items[idx];
                    t.FontFamily = Ui.Text;
                    t.FontSize = 13;
                    t.Foreground = idx == _index ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.Ink, 0.9);
                    wrap.Children.Add(t);
                    row.Child = wrap;
                    row.Background = idx == _index ? Ui.Solid(Ui.Accent, 0.12) : Brushes.Transparent;
                    row.MouseEnter += delegate { row.Background = Ui.Solid(Colors.White, 0.08); };
                    row.MouseLeave += delegate { row.Background = idx == _index ? Ui.Solid(Ui.Accent, 0.12) : Brushes.Transparent; };
                    row.MouseLeftButtonDown += delegate
                    {
                        _index = idx;
                        Render();
                        for (int j = 0; j < list.Children.Count; j++)
                        {
                            var o = list.Children[j] as Border;
                            if (o == null) continue;
                            var oText = (StackPanel)o.Child;
                            var oMark = (TextBlock)oText.Children[0];
                            var oT = (TextBlock)oText.Children[1];
                            bool sel = (int)o.Tag == _index;
                            oMark.Text = sel ? ((char)0xE73E).ToString() : " ";
                            oT.Foreground = sel ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.Ink, 0.9);
                            o.Background = sel ? Ui.Solid(Ui.Accent, 0.12) : Brushes.Transparent;
                        }
                        if (!KeepOpenOnSelect) popup.IsOpen = false;
                        if (Changed != null) Changed();
                    };
                    row.Tag = idx;
                    list.Children.Add(row);
                }
                if (list.Children.Count == 0)
                {
                    var empty = new TextBlock();
                    empty.Text = "Ничего не найдено";
                    empty.FontFamily = Ui.Text;
                    empty.FontSize = 12.5;
                    empty.Foreground = Ui.Solid(Ui.InkDim, 1);
                    empty.Margin = new Thickness(12, 10, 12, 12);
                    list.Children.Add(empty);
                }
            };

            rebuild("");
            searchBox.TextChanged += delegate { rebuild(searchBox.Text); };

            var sv = new ScrollViewer();
            sv.MaxHeight = 330;
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.Content = list;

            root.Children.Add(searchBox);
            root.Children.Add(sv);

            var host = new Border();
            host.CornerRadius = new CornerRadius(13);
            host.Background = Ui.Solid(Color.FromRgb(0x1A, 0x1C, 0x22), 0.99);
            host.BorderBrush = Ui.Solid(Colors.White, 0.14);
            host.BorderThickness = new Thickness(1);
            host.Padding = new Thickness(3);
            host.Child = root;
            host.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 34, ShadowDepth = 8, Opacity = 0.5 };
            popup.Child = host;
            _popup = popup;
            popup.Closed += delegate { _popup = null; };
            popup.IsOpen = true;
            searchBox.Focus();
        }
"""
s3 = s3.replace(old_popup_body, new_popup_body)
io.open(p3, "w", encoding="utf-8").write(s3)
print("dropdown search ok")
