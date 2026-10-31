// Hinoki — liquid glass UI controls, hand-built to match the Apple look
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Hinoki
{
    // ---------- primitives ----------

    public static class Glass
    {
        public static Border Card(double corner, double pad, double topA, double bottomA)
        {
            var b = new Border();
            b.CornerRadius = new CornerRadius(corner);
            b.Background = Ui.GlassFill(topA, bottomA);
            b.BorderBrush = Ui.Solid(Colors.White, 0.14);
            b.BorderThickness = new Thickness(1);
            b.Padding = new Thickness(pad);
            var e = new DropShadowEffect();
            e.Color = Colors.Black; e.BlurRadius = 30; e.ShadowDepth = 6; e.Direction = 270; e.Opacity = 0.30;
            b.Effect = e;
            return b;
        }

        public static TextBlock Label(string text, double size, bool dim = false, bool semibold = false)
        {
            var t = new TextBlock();
            t.Text = text;
            t.FontFamily = Ui.Text;
            t.FontSize = size;
            t.Foreground = dim ? Ui.Solid(Ui.InkDim, 1) : Ui.Solid(Ui.Ink, 0.95);
            if (semibold) t.FontWeight = FontWeights.SemiBold;
            return t;
        }
    }

    // ---------- toggle ----------

    public class GlassToggle : Border
    {
        bool _on;
        public bool IsOn
        {
            get { return _on; }
            set { Set(value, false); }
        }
        public event Action Changed;

        Border _accentLayer;
        Ellipse _knob;
        TranslateTransform _knobX;

        public GlassToggle(bool isOn)
        {
            _on = isOn;
            Width = 50; Height = 30;
            CornerRadius = new CornerRadius(15);
            Background = Ui.Solid(Colors.White, 0.10);
            BorderBrush = Ui.Solid(Colors.White, 0.18);
            BorderThickness = new Thickness(1);
            Cursor = Cursors.Hand;

            var grid = new Grid();
            _accentLayer = new Border();
            _accentLayer.CornerRadius = new CornerRadius(15);
            _accentLayer.Background = Ui.Solid(Ui.Accent, 1);
            _accentLayer.Opacity = isOn ? 1 : 0;
            grid.Children.Add(_accentLayer);

            _knob = new Ellipse();
            _knob.Width = 24; _knob.Height = 24;
            _knob.Fill = Ui.Solid(Colors.White, 0.96);
            var e = new DropShadowEffect();
            e.Color = Colors.Black; e.BlurRadius = 8; e.ShadowDepth = 1; e.Opacity = 0.4;
            _knob.Effect = e;
            _knob.HorizontalAlignment = HorizontalAlignment.Left;
            _knob.VerticalAlignment = VerticalAlignment.Center;
            _knob.Margin = new Thickness(3, 0, 0, 0);
            _knob.RenderTransformOrigin = new Point(0.5, 0.5);
            _knobX = new TranslateTransform(isOn ? 20 : 0, 0);
            _knob.RenderTransform = _knobX;
            grid.Children.Add(_knob);

            Child = grid;
            MouseLeftButtonDown += delegate { Set(!_on, true); };
        }

        void Set(bool on, bool fireEvent)
        {
            _on = on;
            var bg = new DoubleAnimation(on ? 1 : 0, new Duration(TimeSpan.FromMilliseconds(180)));
            bg.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            _accentLayer.BeginAnimation(OpacityProperty, bg);
            var kx = new DoubleAnimation(on ? 20 : 0, new Duration(TimeSpan.FromMilliseconds(200)));
            kx.EasingFunction = new BackEase { Amplitude = 0.6, EasingMode = EasingMode.EaseOut };
            _knobX.BeginAnimation(TranslateTransform.XProperty, kx);
            if (fireEvent && Changed != null) Changed();
        }
    }

    // ---------- text box ----------

    public class GlassTextBox : Border
    {
        public TextBox Box;

        public GlassTextBox(string text, string placeholder, double minWidth)
        {
            MinWidth = minWidth;
            CornerRadius = new CornerRadius(12);
            Background = Ui.Solid(Colors.White, 0.06);
            BorderBrush = Ui.Solid(Colors.White, 0.14);
            BorderThickness = new Thickness(1);
            SnapsToDevicePixels = true;

            Box = new TextBox();
            Box.Text = text;
            Box.FontFamily = Ui.Text;
            Box.FontSize = 13.5;
            Box.Foreground = Ui.Solid(Ui.Ink, 0.97);
            Box.CaretBrush = Ui.Solid(Ui.Accent, 1);
            Box.Background = Brushes.Transparent;
            Box.BorderThickness = new Thickness(0);
            Box.Padding = new Thickness(11, 8, 11, 9);
            Box.MaxLength = 200;
            if (placeholder != null)
            {
                Box.Tag = placeholder;
                if (string.IsNullOrEmpty(text))
                {
                    Box.Foreground = Ui.Solid(Ui.InkDim, 0.7);
                    Box.Text = placeholder;
                }
                Box.GotKeyboardFocus += delegate
                {
                    if ((string)Box.Tag == Box.Text) { Box.Foreground = Ui.Solid(Ui.Ink, 0.97); Box.Text = ""; }
                };
                Box.LostKeyboardFocus += delegate
                {
                    if (Box.Text.Length == 0) { Box.Foreground = Ui.Solid(Ui.InkDim, 0.7); Box.Text = (string)Box.Tag; }
                };
            }
            Child = Box;

            GotKeyboardFocus += delegate { BorderBrush = Ui.Solid(Ui.Accent, 0.85); Background = Ui.Solid(Colors.White, 0.09); };
            LostKeyboardFocus += delegate { BorderBrush = Ui.Solid(Colors.White, 0.14); Background = Ui.Solid(Colors.White, 0.06); };
        }

        public string Value
        {
            get
            {
                if (Box.Tag != null && Box.Text == (string)Box.Tag) return "";
                return Box.Text;
            }
            set
            {
                if (Box.Tag != null)
                {
                    if (string.IsNullOrEmpty(value)) { Box.Foreground = Ui.Solid(Ui.InkDim, 0.7); Box.Text = (string)Box.Tag; return; }
                    Box.Foreground = Ui.Solid(Ui.Ink, 0.97);
                }
                Box.Text = value;
            }
        }
    }

    // ---------- hotkey capture ----------

    public class HotkeyBox : Border
    {
        string _combo;
        bool _capturing;
        public event Action Changed;
        TextBlock _display;

        public string Combo
        {
            get { return _combo; }
            set { _combo = value; Render(); }
        }

        public HotkeyBox(string combo)
        {
            _combo = combo;
            MinWidth = 190;
            CornerRadius = new CornerRadius(12);
            Background = Ui.Solid(Colors.White, 0.06);
            BorderBrush = Ui.Solid(Colors.White, 0.14);
            BorderThickness = new Thickness(1);
            Cursor = Cursors.Hand;
            Focusable = true;

            _display = new TextBlock();
            _display.FontFamily = Ui.Text;
            _display.FontSize = 13.5;
            _display.Foreground = Ui.Solid(Ui.Ink, 0.97);
            _display.Margin = new Thickness(11, 8, 11, 9);
            Child = _display;
            Render();

            MouseLeftButtonDown += delegate
            {
                Focus();
                StartCapture();
            };
            LostKeyboardFocus += delegate { StopCapture(); };
            PreviewKeyDown += OnKey;
        }

        void StartCapture()
        {
            _capturing = true;
            MouseHook.Suppressed = true;
            BorderBrush = Ui.Solid(Ui.Accent, 0.9);
            _display.Foreground = Ui.Solid(Ui.Accent, 1);
            _display.Text = "Нажмите сочетание… (Esc — отмена)";
        }

        void StopCapture()
        {
            _capturing = false;
            MouseHook.Suppressed = false;
            BorderBrush = Ui.Solid(Colors.White, 0.14);
            _display.Foreground = Ui.Solid(Ui.Ink, 0.97);
            Render();
        }

        void OnKey(object sender, KeyEventArgs e)
        {
            if (!_capturing) return;
            e.Handled = true;
            Key k = e.Key == Key.System ? e.SystemKey : e.Key;
            if (k == Key.Escape) { StopCapture(); return; }
            if (k == Key.LeftCtrl || k == Key.RightCtrl || k == Key.LeftShift || k == Key.RightShift ||
                k == Key.LeftAlt || k == Key.RightAlt || k == Key.LWin || k == Key.RWin) return;

            var mods = Keyboard.Modifiers;
            var parts = new List<string>();
            if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
            if ((mods & ModifierKeys.Shift) != 0) parts.Add("Shift");
            if ((mods & ModifierKeys.Alt) != 0) parts.Add("Alt");
            if ((mods & ModifierKeys.Windows) != 0) parts.Add("Win");

            string name = new KeyConverter().ConvertToString(k);
            if (name != null && name.Length == 2 && name[0] == 'D' && char.IsDigit(name[1])) name = name.Substring(1);
            parts.Add(name);

            _combo = string.Join("+", parts.ToArray());
            StopCapture();
            if (Changed != null) Changed();
        }

        void Render()
        {
            _display.Text = string.IsNullOrEmpty(_combo) ? "Не задано" : PrettyCombo(_combo);
        }

        public static string PrettyCombo(string combo)
        {
            var parts = combo.Split('+');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Length > 0 ? char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1) : parts[i];
            return string.Join(" + ", parts);
        }
    }

    // ---------- dropdown ----------

    public class GlassDropDown : Border
    {
        List<string> _items;
        int _index;
        public event Action Changed;
        TextBlock _label;
        Popup _popup;

        /// <summary>When true the popup stays open after picking — for browsing actions.</summary>
        public bool KeepOpenOnSelect { get; set; }

        public int SelectedIndex
        {
            get { return _index; }
            set { _index = Math.Max(0, Math.Min(_items.Count - 1, value)); Render(); }
        }

        public GlassDropDown(List<string> items, int selected)
        {
            _items = items;
            _index = Math.Max(0, Math.Min(items.Count - 1, selected));
            MinWidth = 250;
            CornerRadius = new CornerRadius(12);
            Background = Ui.Solid(Colors.White, 0.06);
            BorderBrush = Ui.Solid(Colors.White, 0.14);
            BorderThickness = new Thickness(1);
            Cursor = Cursors.Hand;

            var grid = new Grid();
            grid.Margin = new Thickness(1, 0, 1, 0);
            _label = new TextBlock();
            _label.FontFamily = Ui.Text;
            _label.FontSize = 13.5;
            _label.VerticalAlignment = VerticalAlignment.Center;
            _label.Margin = new Thickness(11, 9, 30, 10);
            _label.TextTrimming = TextTrimming.CharacterEllipsis;
            grid.Children.Add(_label);

            var chev = new TextBlock();
            chev.Text = ((char)0xE70D).ToString();
            chev.FontFamily = Ui.Icons;
            chev.FontSize = 11;
            chev.Foreground = Ui.Solid(Ui.InkDim, 1);
            chev.HorizontalAlignment = HorizontalAlignment.Right;
            chev.VerticalAlignment = VerticalAlignment.Center;
            chev.Margin = new Thickness(0, 0, 13, 0);
            grid.Children.Add(chev);

            Child = grid;
            Render();

            // open on UP: a Down-opened popup gets instantly dismissed by the same click's capture logic
            PreviewMouseLeftButtonUp += delegate { OverlayWindow.Log("DROPDOWN open requested"); OpenPopup(); };
        }

        void Render() { _label.Text = _items[_index]; }

        void OpenPopup()
        {
            if (_popup != null && _popup.IsOpen) { _popup.IsOpen = false; return; }
            var popup = new Popup();
            popup.PlacementTarget = this;
            popup.Placement = PlacementMode.Bottom;
            popup.StaysOpen = false;
            popup.AllowsTransparency = true;
            popup.PopupAnimation = PopupAnimation.Fade;

            var list = new StackPanel();
            list.MinWidth = Math.Max(MinWidth, ActualWidth);
            for (int i = 0; i < _items.Count; i++)
            {
                int idx = i;
                var row = new Border();
                row.Padding = new Thickness(12, 8, 12, 9);
                row.CornerRadius = new CornerRadius(9);
                row.Margin = new Thickness(5, 2, 5, 2);
                row.Cursor = Cursors.Hand;
                var wrap = new StackPanel();
                wrap.Orientation = Orientation.Horizontal;
                var mark = new TextBlock();
                mark.Text = i == _index ? ((char)0xE73E).ToString() : " ";
                mark.FontFamily = Ui.Icons;
                mark.FontSize = 11;
                mark.Width = 18;
                mark.Foreground = Ui.Solid(Ui.Accent, 1);
                wrap.Children.Add(mark);
                var t = new TextBlock();
                t.Text = _items[i];
                t.FontFamily = Ui.Text;
                t.FontSize = 13;
                t.Foreground = i == _index ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.Ink, 0.9);
                wrap.Children.Add(t);
                row.Child = wrap;
                row.Background = i == _index ? Ui.Solid(Ui.Accent, 0.12) : Brushes.Transparent;
                row.MouseEnter += delegate { row.Background = Ui.Solid(Colors.White, 0.08); };
                row.MouseLeave += delegate { row.Background = i == _index ? Ui.Solid(Ui.Accent, 0.12) : Brushes.Transparent; };
                row.MouseLeftButtonDown += delegate
                {
                    _index = idx;
                    Render();
                    // refresh every row's state without closing
                    for (int j = 0; j < list.Children.Count; j++)
                    {
                        var other = (Border)list.Children[j];
                        var otherText = (StackPanel)other.Child;
                        var mk = (TextBlock)otherText.Children[0];
                        var tx = (TextBlock)otherText.Children[1];
                        bool sel = j == _index;
                        mk.Text = sel ? ((char)0xE73E).ToString() : " ";
                        tx.Foreground = sel ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.Ink, 0.9);
                        other.Background = sel ? Ui.Solid(Ui.Accent, 0.12) : Brushes.Transparent;
                    }
                    if (!KeepOpenOnSelect) popup.IsOpen = false;
                    if (Changed != null) Changed();
                };
                list.Children.Add(row);
            }

            var host = new Border();
            host.CornerRadius = new CornerRadius(13);
            host.Background = Ui.Solid(Color.FromRgb(0x1A, 0x1C, 0x22), 0.98);
            host.BorderBrush = Ui.Solid(Colors.White, 0.14);
            host.BorderThickness = new Thickness(1);
            host.Padding = new Thickness(3, 3, 3, 3);
            var sv = new ScrollViewer();
            sv.MaxHeight = 330;
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.Content = list;
            host.Child = sv;
            host.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 34, ShadowDepth = 8, Opacity = 0.5 };
            popup.Child = host;
            _popup = popup;
            popup.Closed += delegate { _popup = null; };
            popup.IsOpen = true;
        }
    }

    // ---------- slider ----------

    public class GlassSlider : Canvas
    {
        double _min, _max, _value;
        public event Action Changed;
        Border _track, _fill;
        Ellipse _knob;
        bool _dragging;

        public double Value
        {
            get { return _value; }
            set { Set(Math.Max(_min, Math.Min(_max, value)), false); }
        }

        public GlassSlider(double min, double max, double value)
        {
            _min = min; _max = max; _value = value;
            Height = 30;
            Cursor = Cursors.Hand;

            _track = new Border();
            _track.Height = 5;
            _track.CornerRadius = new CornerRadius(3);
            _track.Background = Ui.Solid(Colors.White, 0.12);
            _track.VerticalAlignment = VerticalAlignment.Center;
            Children.Add(_track);

            _fill = new Border();
            _fill.Height = 5;
            _fill.CornerRadius = new CornerRadius(3);
            _fill.Background = Ui.Solid(Ui.Accent, 1);
            _fill.VerticalAlignment = VerticalAlignment.Center;
            Children.Add(_fill);

            _knob = new Ellipse();
            _knob.Width = 17; _knob.Height = 17;
            _knob.Fill = Ui.Solid(Colors.White, 0.98);
            _knob.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 8, ShadowDepth = 1, Opacity = 0.45 };
            _knob.VerticalAlignment = VerticalAlignment.Center;
            Children.Add(_knob);

            SizeChanged += delegate { Layout(); };
            MouseLeftButtonDown += delegate
            {
                _dragging = true;
                CaptureMouse();
                Track(Mouse.GetPosition(this).X);
            };
            MouseMove += delegate { if (_dragging) Track(Mouse.GetPosition(this).X); };
            MouseLeftButtonUp += delegate { _dragging = false; ReleaseMouseCapture(); };
        }

        void Track(double x)
        {
            double t = Math.Max(0, Math.Min(1, x / Math.Max(1, ActualWidth)));
            double v = _min + (_max - _min) * t;
            if (Math.Abs(v - _value) > 0.4) { Set(v, true); }
        }

        void Set(double v, bool fireEvent)
        {
            _value = v;
            Layout();
            if (fireEvent && Changed != null) Changed();
        }

        void Layout()
        {
            double w = Math.Max(1, ActualWidth);
            double t = (_value - _min) / (_max - _min);
            SetTop(_track, 13); SetTop(_fill, 13);
            _track.Width = w;
            _fill.Width = Math.Max(5, w * t);
            SetLeft(_knob, w * t - 8.5);
            SetTop(_knob, 7);
        }
    }

    // ---------- pill button ----------

    public class PillButton : Border
    {
        public event Action Clicked;

        public PillButton(string text, bool accent, double padX = 16)
        {
            CornerRadius = new CornerRadius(17);
            Cursor = Cursors.Hand;
            Padding = new Thickness(padX, 7, padX, 8);
            var t = new TextBlock();
            t.Text = text;
            t.FontFamily = Ui.Text;
            t.FontSize = 13;
            t.FontWeight = FontWeights.SemiBold;
            t.HorizontalAlignment = HorizontalAlignment.Center;
            Child = t;

            if (accent)
            {
                Background = Ui.Solid(Ui.Accent, 1);
                t.Foreground = Ui.Solid(Color.FromRgb(0x1B, 0x22, 0x16), 1);
            }
            else
            {
                Background = Ui.GlassFill(0.12, 0.06);
                BorderBrush = Ui.Solid(Colors.White, 0.16);
                BorderThickness = new Thickness(1);
                t.Foreground = Ui.Solid(Ui.Ink, 0.95);
            }

            MouseEnter += delegate { Opacity = 0.9; };
            MouseLeave += delegate { Opacity = 1; };
            MouseLeftButtonDown += delegate { Scale(0.95); };
            MouseLeftButtonUp += delegate
            {
                Scale(1);
                var h = Clicked; if (h != null) h();
            };
            MouseLeave += delegate { Opacity = 1; Scale(1); };
        }

        void Scale(double s)
        {
            var st = RenderTransform as ScaleTransform;
            if (st == null)
            {
                st = new ScaleTransform(1, 1);
                RenderTransformOrigin = new Point(0.5, 0.5);
                RenderTransform = st;
            }
            var a = new DoubleAnimation(s, new Duration(TimeSpan.FromMilliseconds(110)));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }
    }

    // ---------- segmented tabs ----------

    public class Segmented : Border
    {
        string[] _labels;
        int _index;
        public event Action<int> Changed;
        Border _knob;
        TextBlock[] _texts;
        double _segW = 118;

        public int Selected
        {
            get { return _index; }
            set { Select(value, false); }
        }

        public Segmented(string[] labels, int selected)
        {
            _labels = labels;
            _index = selected;
            CornerRadius = new CornerRadius(16);
            Background = Ui.Solid(Colors.White, 0.07);
            BorderBrush = Ui.Solid(Colors.White, 0.12);
            BorderThickness = new Thickness(1);
            Padding = new Thickness(3);

            var grid = new Grid();
            _knob = new Border();
            _knob.CornerRadius = new CornerRadius(13);
            _knob.Background = Ui.Solid(Colors.White, 0.15);
            _knob.BorderBrush = Ui.Solid(Colors.White, 0.10);
            _knob.BorderThickness = new Thickness(1);
            _knob.Width = _segW;
            _knob.HorizontalAlignment = HorizontalAlignment.Left;
            _knob.Margin = new Thickness(0, 0, 0, 0);
            _knob.RenderTransform = new TranslateTransform(selected * _segW, 0);
            grid.Children.Add(_knob);

            var panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            _texts = new TextBlock[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var t = new TextBlock();
                t.Text = labels[i];
                t.FontFamily = Ui.Text;
                t.FontSize = 12.5;
                t.FontWeight = FontWeights.Medium;
                t.TextAlignment = TextAlignment.Center;
                t.Width = _segW;
                t.Padding = new Thickness(0, 6, 0, 7);
                t.Cursor = Cursors.Hand;
                t.MouseLeftButtonDown += delegate { Select(idx, true); };
                panel.Children.Add(t);
                _texts[i] = t;
            }
            grid.Children.Add(panel);
            Child = grid;
            Select(selected, false);
        }

        void Select(int i, bool fireEvent)
        {
            _index = Math.Max(0, Math.Min(_labels.Length - 1, i));
            var tf = (TranslateTransform)_knob.RenderTransform;
            var a = new DoubleAnimation(_index * _segW, new Duration(TimeSpan.FromMilliseconds(220)));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            tf.BeginAnimation(TranslateTransform.XProperty, a);
            for (int j = 0; j < _texts.Length; j++)
                _texts[j].Foreground = j == _index ? Ui.Solid(Ui.Ink, 1) : Ui.Solid(Ui.InkDim, 1);
            if (fireEvent && Changed != null) Changed(_index);
        }
    }

    // ---------- icon picker tile ----------

    public class IconTile : Border
    {
        bool _selected;
        public readonly string Glyph;
        public event Action<string> Picked;
        TextBlock _icon;
        public static List<IconTile> ActiveGroup = new List<IconTile>();

        public IconTile(string glyphHex)
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
            Child = _icon;

            MouseLeftButtonDown += delegate
            {
                foreach (var t in ActiveGroup) t.SetSelected(false);
                SetSelected(true);
                var h = Picked; if (h != null) h(Glyph);
            };
        }

        public void SetSelected(bool on)
        {
            _selected = on;
            BorderThickness = new Thickness(0);
            Background = on ? Ui.Solid(Ui.Accent, 0.20) : Ui.Solid(Colors.White, 0.05);
            _icon.Foreground = on ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.Ink, 0.9);
        }
    }

    // ---------- settings row ----------

    public static class Rows
    {
        public static FrameworkElement Row(string title, string sub, FrameworkElement right, bool divider = true)
        {
            var grid = new Grid();
            grid.Margin = new Thickness(0, 10, 0, 10);
            var sp = new StackPanel();
            sp.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(Glass.Label(title, 13.5, false, true));
            if (!string.IsNullOrEmpty(sub))
            {
                var s = Glass.Label(sub, 11.5, true);
                s.Margin = new Thickness(0, 2, 12, 0);
                s.TextWrapping = TextWrapping.Wrap;
                sp.Children.Add(s);
            }
            grid.Children.Add(sp);
            if (right != null)
            {
                right.VerticalAlignment = VerticalAlignment.Center;
                var col = new ColumnDefinition();
                col.Width = GridLength.Auto;
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(col);
                Grid.SetColumn(right, 1);
                grid.Children.Add(right);
            }
            if (divider)
            {
                var line = new Border();
                line.Height = 1;
                line.Background = Ui.Solid(Colors.White, 0.07);
                line.Margin = new Thickness(0, 10, 0, -11);
                var outer = new StackPanel();
                outer.Children.Add(grid);
                outer.Children.Add(line);
                return outer;
            }
            return grid;
        }
    }
}
