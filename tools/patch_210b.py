"""v2.1 part B: minimal scrollbars + icon picker first-row/expand."""
import io

# ============ SettingsWindow: slim scrollbar style + picker expand ============
p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# 1) resources with slim scrollbar style, add after chrome blob creation (before layout)
anchor = "            var layout = new Grid();"
add = """            Resources = new ResourceDictionary();
            var thumbStyle = new Style(typeof(System.Windows.Controls.Primitives.Thumb));
            thumbStyle.Setters.Add(new Setter(TemplateProperty, new ControlTemplate(typeof(System.Windows.Controls.Primitives.Thumb))));
            thumbStyle.Setters.Add(new Setter(BackgroundProperty, Ui.Solid(Colors.White, 0.16)));
            thumbStyle.Setters.Add(new Setter(CornerRadiusProperty, new CornerRadius(3)));
            var sbTemplate = new ControlTemplate(typeof(System.Windows.Controls.Primitives.ScrollBar));
            var trackFac = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.Track));
            trackFac.Name = "PART_Track";
            var thumbFac = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.Thumb));
            thumbFac.Setters.Add(new Setter(BackgroundProperty, Ui.Solid(Colors.White, 0.18)));
            thumbFac.Setters.Add(new Setter(TemplateProperty, new ControlTemplate(typeof(System.Windows.Controls.Primitives.Thumb))));
            trackFac.SetValue(System.Windows.Controls.Primitives.Track.ThumbProperty, thumbFac);
            sbTemplate.VisualTree = trackFac;
            var sbStyle = new Style(typeof(System.Windows.Controls.Primitives.ScrollBar));
            sbStyle.Setters.Add(new Setter(WidthProperty, 6.0));
            sbStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            sbStyle.Setters.Add(new Setter(TemplateProperty, sbTemplate));
            Resources.Add(typeof(System.Windows.Controls.Primitives.ScrollBar), sbStyle);

            var layout = new Grid();"""
assert anchor in s
s = s.replace(anchor, add, 1)

# 2) picker: first row + expand chevron
old_picker = s[s.index('            IconTile.ActiveGroup = new List<IconTile>();\n            Action<string> rebuildIcons'):s.index('            rebuildIcons("");\n            _iconSearch.Box.TextChanged += delegate { rebuildIcons(_iconSearch.Value); };')]
new_picker = """            IconTile.ActiveGroup = new List<IconTile>();
            _expandedIcons = false;
            Action rebuildIcons = null;

            var expandBtn = new Border();
            expandBtn.CornerRadius = new CornerRadius(10);
            expandBtn.Background = Ui.Solid(Colors.White, 0.04);
            expandBtn.Cursor = Cursors.Hand;
            expandBtn.Margin = new Thickness(0, 6, 0, 0);
            var ebGrid = new Grid();
            ebGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ebGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var ebText = Glass.Label("", 12, true);
            ebText.Margin = new Thickness(10, 6, 0, 8);
            Grid.SetColumn(ebText, 0);
            ebGrid.Children.Add(ebText);
            var chev = new TextBlock();
            chev.Text = ((char)0xE70D).ToString();
            chev.FontFamily = Ui.Icons;
            chev.FontSize = 10;
            chev.Foreground = Ui.Solid(Ui.InkDim, 1);
            chev.HorizontalAlignment = HorizontalAlignment.Center;
            chev.VerticalAlignment = VerticalAlignment.Center;
            chev.RenderTransformOrigin = new Point(0.5, 0.5);
            chev.RenderTransform = new RotateTransform(0);
            Grid.SetColumn(chev, 1);
            chev.Margin = new Thickness(0, 0, 10, 0);
            ebGrid.Children.Add(chev);
            expandBtn.Child = ebGrid;
            expandBtn.MouseEnter += delegate { expandBtn.Background = Ui.Solid(Colors.White, 0.08); };
            expandBtn.MouseLeave += delegate { expandBtn.Background = Ui.Solid(Colors.White, 0.04); };

            expandBtn.MouseLeftButtonUp += delegate
            {
                _expandedIcons = !_expandedIcons;
                var rot = (RotateTransform)chev.RenderTransform;
                var ra = new DoubleAnimation(_expandedIcons ? 180 : 0, new Duration(TimeSpan.FromMilliseconds(260)));
                ra.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                rot.BeginAnimation(RotateTransform.AngleProperty, ra);
                var target = _expandedIcons ? 216.0 : 58.0;
                var ha = new DoubleAnimation(target, new Duration(TimeSpan.FromMilliseconds(320)));
                ha.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                iconScroll.BeginAnimation(HeightProperty, ha);
                ebText.Text = _expandedIcons ? "Свернуть" : _expandLabel;
            };

            rebuildIcons = delegate(string query)
            {
                _iconPanel.Children.Clear();
                string q = (query ?? "").Trim().ToLowerInvariant();
                IconTile.ActiveGroup = new List<IconTile>();
                int shown = 0;
                foreach (var def in IconLibrary)
                {
                    string tags = def[1];
                    if (q.Length > 0 && !tags.Contains(q) && !def[0].ToLowerInvariant().Contains(q)) continue;
                    var gl = def[0];
                    var tile = new IconTile(gl);
                    tile.Picked += delegate (string g)
                    {
                        App.Config.Petals[_selPetal].Icon = g;
                        App.SaveConfig();
                        _preview.UpdatePetal(_selPetal);
                        RefreshPetalList();
                        Pulse();
                    };
                    IconTile.ActiveGroup.Add(tile);
                    _iconPanel.Children.Add(tile);
                    shown++;
                }
                _expandLabel = "Все иконки (" + shown + ")";
                if (!_expandedIcons)
                {
                    // collapse: hide everything beyond the first 8 tiles
                    int keep = 8;
                    for (int i = _iconPanel.Children.Count - 1; i >= keep; i--)
                        _iconPanel.Children.RemoveAt(i);
                }
                ebText.Text = _expandedIcons ? "Свернуть" : _expandLabel;
                iconScroll.Height = _expandedIcons ? 216 : 58;
            };

            _editor.Children.Add(expandBtn);
            expandBtn.Visibility = Visibility.Visible;
"""
# wire expandBtn into editor after iconScroll
s = s.replace(old_picker, new_picker)
s = s.replace("""            _editor.Children.Add(iconScroll);

            rebuildIcons("");""",
"""            _editor.Children.Add(iconScroll);
            _editor.Children.Add(expandBtn);

            rebuildIcons("");""")

# expandBtn must exist before rebuildIcons uses it — move declaration before rebuild (already in replaced block)
# hide expand button when searching
s = s.replace("""            rebuildIcons("");
            _iconSearch.Box.TextChanged += delegate { rebuildIcons(_iconSearch.Value); };""",
"""            rebuildIcons("");
            _iconSearch.Box.TextChanged += delegate
            {
                rebuildIcons(_iconSearch.Value);
                expandBtn.Visibility = _iconSearch.Value.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            };""")

# fields
s = s.replace("        GlassTextBox _iconSearch;\n        WrapPanel _iconPanel;",
              "        GlassTextBox _iconSearch;\n        WrapPanel _iconPanel;\n        bool _expandedIcons;\n        string _expandLabel = \"\";")

io.open(p, "w", encoding="utf-8").write(s)
print("picker expand ok")
