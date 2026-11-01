// Hinoki — Apple-style glass UI controls
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
    public static class Glass
    {
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

    public static class Rows
    {
        public static FrameworkElement Setting(string title, string sub, FrameworkElement right)
        {
            var grid = new Grid();
            grid.Margin = new Thickness(2, 10, 0, 10);
            var sp = new StackPanel();
            sp.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(Glass.Label(title, 14, false, true));
            if (!string.IsNullOrEmpty(sub))
            {
                var s = Glass.Label(sub, 11.5, true);
                s.Margin = new Thickness(0, 2, 14, 0);
                s.TextWrapping = TextWrapping.Wrap;
                sp.Children.Add(s);
            }
            grid.Children.Add(sp);
            if (right != null)
            {
                right.VerticalAlignment = VerticalAlignment.Center;
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(right, 1);
                grid.Children.Add(right);
            }
            return grid;
        }

        public static FrameworkElement Section(string text)
        {
            var t = new TextBlock();
            t.Text = text.ToUpperInvariant();
            t.FontFamily = Ui.Text;
            t.FontSize = 11;
            t.FontWeight = FontWeights.SemiBold;
            t.Foreground = Ui.Solid(Ui.InkDim, 0.85);
            t.Margin = new Thickness(2, 20, 0, 10);
            return t;
        }

        public static FrameworkElement Sep()
        {
            var line = new Border();
            line.Height = 1;
            line.Background = Ui.Solid(Colors.White, 0.05);
            line.Margin = new Thickness(0, 12, 0, 4);
            return line;
        }

        public static FrameworkElement AttachSlider(GlassSlider slider, TextBlock value)
        {
            var host = new Grid();
            host.MinWidth = 220;
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            slider.MinWidth = 150;
            slider.Margin = new Thickness(0, 0, 12, 0);
            Grid.SetColumn(slider, 0);
            value.Width = 52;
            value.HorizontalAlignment = HorizontalAlignment.Right;
            value.TextAlignment = TextAlignment.Right;
            Grid.SetColumn(value, 1);
            host.Children.Add(slider);
            host.Children.Add(value);
            return host;
        }
    }

    // ---------- toggle ----------
    public class GlassToggle : Border
    {
        bool _on;
        public bool IsOn { get { return _on; } set { Set(value, false); } }
        public event Action Changed;
        Border _accentLayer;
        Ellipse _knob;
        TranslateTransform _knobX;

        public GlassToggle(bool isOn)
        {
            _on = isOn;
            Width = 44; Height = 26;
            CornerRadius = new CornerRadius(13);
            Background = Ui.Solid(Colors.White, isOn ? 0.0 : 0.08);
            BorderThickness = new Thickness(0);
            Cursor = Cursors.Hand;

            var grid = new Grid();
            _accentLayer = new Border();
            _accentLayer.CornerRadius = new CornerRadius(13);
            _accentLayer.Background = Ui.Solid(Ui.Accent, 1);
            _accentLayer.Opacity = isOn ? 1 : 0;
            grid.Children.Add(_accentLayer);

            _knob = new Ellipse();
            _knob.Width = 22; _knob.Height = 22;
            _knob.Fill = Ui.Solid(Colors.White, 0.95);
            _knob.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 6, ShadowDepth = 1, Opacity = 0.3 };
            _knob.HorizontalAlignment = HorizontalAlignment.Left;
            _knob.VerticalAlignment = VerticalAlignment.Center;
            _knob.Margin = new Thickness(2, 0, 0, 0);
            _knob.RenderTransformOrigin = new Point(0.5, 0.5);
            _knobX = new TranslateTransform(isOn ? 18 : 0, 0);
            _knob.RenderTransform = _knobX;
            grid.Children.Add(_knob);

            Child = grid;
            MouseLeftButtonDown += delegate { Set(!_on, true); };
        }

        void Set(bool on, bool fireEvent)
        {
            _on = on;
            var bg = new DoubleAnimation(on ? 1 : 0, new Duration(TimeSpan.FromMilliseconds(200)));
            bg.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            _accentLayer.BeginAnimation(OpacityProperty, bg);
            var kx = new DoubleAnimation(on ? 18 : 0, new Duration(TimeSpan.FromMilliseconds(220)));
            kx.EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut };
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
            CornerRadius = new CornerRadius(10);
            Background = Ui.Solid(Colors.White, 0.05);
            BorderBrush = Ui.Solid(Colors.White, 0.10);
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
            Box.Padding = new Thickness(10, 7, 10, 8);
            Box.MaxLength = 300;
            if (placeholder != null)
            {
                Box.Tag = placeholder;
                if (string.IsNullOrEmpty(text))
                {
                    Box.Foreground = Ui.Solid(Ui.InkDim, 0.6);
                    Box.Text = placeholder;
                }
                Box.GotKeyboardFocus += delegate
                {
                    if ((string)Box.Tag == Box.Text) { Box.Foreground = Ui.Solid(Ui.Ink, 0.97); Box.Text = ""; }
                };
                Box.LostKeyboardFocus += delegate
                {
                    if (Box.Text.Length == 0) { Box.Foreground = Ui.Solid(Ui.InkDim, 0.6); Box.Text = (string)Box.Tag; }
                };
            }
            Child = Box;

            GotKeyboardFocus += delegate { BorderBrush = Ui.Solid(Ui.Accent, 0.7); Background = Ui.Solid(Colors.White, 0.08); };
            LostKeyboardFocus += delegate { BorderBrush = Ui.Solid(Colors.White, 0.10); Background = Ui.Solid(Colors.White, 0.05); };
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
                    if (string.IsNullOrEmpty(value)) { Box.Foreground = Ui.Solid(Ui.InkDim, 0.6); Box.Text = (string)Box.Tag; return; }
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

        public string Combo { get { return _combo; } set { _combo = value; Render(); } }

        public HotkeyBox(string combo)
        {
            _combo = combo;
            MinWidth = 190;
            CornerRadius = new CornerRadius(10);
            Background = Ui.Solid(Colors.White, 0.05);
            BorderBrush = Ui.Solid(Colors.White, 0.10);
            BorderThickness = new Thickness(1);
            Cursor = Cursors.Hand;
            Focusable = true;

            _display = new TextBlock();
            _display.FontFamily = Ui.Text;
            _display.FontSize = 13.5;
            _display.Foreground = Ui.Solid(Ui.Ink, 0.97);
            _display.Margin = new Thickness(10, 7, 10, 8);
            Child = _display;
            Render();

            MouseLeftButtonDown += delegate { Focus(); StartCapture(); };
            LostKeyboardFocus += delegate { StopCapture(); };
            PreviewKeyDown += OnKey;
        }

        void StartCapture()
        {
            _capturing = true;
            MouseHook.Suppressed = true;
            BorderBrush = Ui.Solid(Ui.Accent, 0.7);
            _display.Foreground = Ui.Solid(Ui.Accent, 1);
            _display.Text = "Нажмите сочетание…";
        }

        void StopCapture()
        {
            _capturing = false;
            MouseHook.Suppressed = false;
            BorderBrush = Ui.Solid(Colors.White, 0.10);
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
        public bool KeepOpenOnSelect { get; set; }
        TextBlock _label;
        Popup _popup;

        public int SelectedIndex
        {
            get { return _index; }
            set { _index = Math.Max(0, Math.Min(_items.Count - 1, value)); Render(); }
        }

        public GlassDropDown(List<string> items, int selected)
        {
            _items = items;
            _index = Math.Max(0, Math.Min(items.Count - 1, selected));
            MinWidth = 200;
            CornerRadius = new CornerRadius(10);
            Background = Ui.Solid(Colors.White, 0.05);
            BorderBrush = Ui.Solid(Colors.White, 0.10);
            BorderThickness = new Thickness(1);
            Cursor = Cursors.Hand;

            var grid = new Grid();
            _label = new TextBlock();
            _label.FontFamily = Ui.Text;
            _label.FontSize = 13.5;
            _label.Foreground = Ui.Solid(Ui.Ink, 0.95);
            _label.VerticalAlignment = VerticalAlignment.Center;
            _label.Margin = new Thickness(10, 8, 26, 9);
            _label.TextTrimming = TextTrimming.CharacterEllipsis;
            grid.Children.Add(_label);
            var chev = new TextBlock();
            chev.Text = ((char)0xE70D).ToString();
            chev.FontFamily = Ui.Icons;
            chev.FontSize = 9;
            chev.Foreground = Ui.Solid(Ui.InkDim, 1);
            chev.HorizontalAlignment = HorizontalAlignment.Right;
            chev.VerticalAlignment = VerticalAlignment.Center;
            chev.Margin = new Thickness(0, 0, 10, 0);
            grid.Children.Add(chev);
            Child = grid;
            Render();

            PreviewMouseLeftButtonUp += delegate { OpenPopup(); };
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

            var root = new StackPanel();
            var searchBox = new TextBox();
            searchBox.FontFamily = Ui.Text;
            searchBox.FontSize = 13;
            searchBox.Foreground = Ui.Solid(Ui.Ink, 0.95);
            searchBox.CaretBrush = Ui.Solid(Ui.Accent, 1);
            searchBox.Background = Ui.Solid(Colors.White, 0.04);
            searchBox.BorderBrush = Ui.Solid(Colors.White, 0.07);
            searchBox.BorderThickness = new Thickness(1);
            searchBox.Padding = new Thickness(8, 6, 8, 7);
            searchBox.Margin = new Thickness(5, 5, 5, 3);
            searchBox.HorizontalAlignment = HorizontalAlignment.Stretch;

            var list = new StackPanel();
            list.MinWidth = Math.Max(MinWidth, ActualWidth);

            Action<string> rebuild = delegate(string query)
            {
                list.Children.Clear();
                string q = (query ?? "").Trim().ToLowerInvariant();
                for (int i = 0; i < _items.Count; i++)
                {
                    if (q.Length > 0 && !_items[i].ToLowerInvariant().Contains(q)) continue;
                    int idx = i;
                    var row = new Border();
                    row.Padding = new Thickness(10, 6, 10, 7);
                    row.CornerRadius = new CornerRadius(8);
                    row.Margin = new Thickness(4, 1, 4, 1);
                    row.Cursor = Cursors.Hand;
                    var wrap = new StackPanel();
                    wrap.Orientation = Orientation.Horizontal;
                    var mark = new TextBlock();
                    mark.Text = idx == _index ? ((char)0xE73E).ToString() : " ";
                    mark.FontFamily = Ui.Icons;
                    mark.FontSize = 10;
                    mark.Width = 16;
                    mark.Foreground = Ui.Solid(Ui.Accent, 1);
                    wrap.Children.Add(mark);
                    var t = new TextBlock();
                    t.Text = _items[idx];
                    t.FontFamily = Ui.Text;
                    t.FontSize = 13;
                    t.Foreground = idx == _index ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.Ink, 0.9);
                    wrap.Children.Add(t);
                    row.Child = wrap;
                    row.Background = idx == _index ? Ui.Solid(Ui.Accent, 0.10) : Brushes.Transparent;
                    row.MouseEnter += delegate { row.Background = Ui.Solid(Colors.White, 0.06); };
                    row.MouseLeave += delegate { row.Background = idx == _index ? Ui.Solid(Ui.Accent, 0.10) : Brushes.Transparent; };
                    row.MouseLeftButtonDown += delegate
                    {
                        _index = idx;
                        Render();
                        for (int j = 0; j < list.Children.Count; j++)
                        {
                            var o = list.Children[j] as Border;
                            if (o == null) continue;
                            var oWrap = (StackPanel)o.Child;
                            var oMk = (TextBlock)oWrap.Children[0];
                            var oT = (TextBlock)oWrap.Children[1];
                            bool sel = (int)o.Tag == _index;
                            oMk.Text = sel ? ((char)0xE73E).ToString() : " ";
                            oT.Foreground = sel ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.Ink, 0.9);
                            o.Background = sel ? Ui.Solid(Ui.Accent, 0.10) : Brushes.Transparent;
                        }
                        if (!KeepOpenOnSelect) popup.IsOpen = false;
                        if (Changed != null) Changed();
                    };
                    row.Tag = idx;
                    list.Children.Add(row);
                }
            };
            rebuild("");

            var sv = new ScrollViewer();
            sv.MaxHeight = 300;
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.Content = list;
            root.Children.Add(searchBox);
            root.Children.Add(sv);
            searchBox.TextChanged += delegate { rebuild(searchBox.Text); };

            var host = new Border();
            host.CornerRadius = new CornerRadius(12);
            host.Background = Ui.Solid(Color.FromRgb(0x1A, 0x1C, 0x22), 0.99);
            host.BorderBrush = Ui.Solid(Colors.White, 0.10);
            host.BorderThickness = new Thickness(1);
            host.Padding = new Thickness(2);
            host.Child = root;
            host.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 28, ShadowDepth = 6, Opacity = 0.4 };
            popup.Child = host;
            _popup = popup;
            popup.Closed += delegate { _popup = null; };
            popup.IsOpen = true;
            searchBox.Focus();
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

        public double Value { get { return _value; } set { Set(Math.Max(_min, Math.Min(_max, value)), false); } }

        public GlassSlider(double min, double max, double value)
        {
            _min = min; _max = max; _value = value;
            Height = 26;
            Cursor = Cursors.Hand;

            _track = new Border();
            _track.Height = 4;
            _track.CornerRadius = new CornerRadius(2);
            _track.Background = Ui.Solid(Colors.White, 0.08);
            _track.VerticalAlignment = VerticalAlignment.Center;
            Children.Add(_track);

            _fill = new Border();
            _fill.Height = 4;
            _fill.CornerRadius = new CornerRadius(2);
            _fill.Background = Ui.Solid(Ui.Accent, 0.7);
            _fill.VerticalAlignment = VerticalAlignment.Center;
            Children.Add(_fill);

            _knob = new Ellipse();
            _knob.Width = 16; _knob.Height = 16;
            _knob.Fill = Ui.Solid(Colors.White, 0.95);
            _knob.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 6, ShadowDepth = 1, Opacity = 0.35 };
            _knob.VerticalAlignment = VerticalAlignment.Center;
            Children.Add(_knob);

            SizeChanged += delegate { Layout(); };
            MouseLeftButtonDown += delegate { _dragging = true; CaptureMouse(); Track(Mouse.GetPosition(this).X); };
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
            SetTop(_track, 11); SetTop(_fill, 11);
            _track.Width = w;
            _fill.Width = Math.Max(4, w * t);
            SetLeft(_knob, w * t - 8);
            SetTop(_knob, 5);
        }
    }

    // ---------- pill button ----------
    public class PillButton : Border
    {
        public event Action Clicked;

        public PillButton(string text, bool accent)
        {
            CornerRadius = new CornerRadius(10);
            Cursor = Cursors.Hand;
            Padding = new Thickness(14, 7, 14, 8);
            var t = new TextBlock();
            t.Text = text;
            t.FontFamily = Ui.Text;
            t.FontSize = 13;
            t.FontWeight = FontWeights.SemiBold;
            t.HorizontalAlignment = HorizontalAlignment.Center;
            Child = t;

            if (accent)
            {
                Background = Ui.Solid(Ui.Accent, 0.9);
                t.Foreground = Ui.Solid(Color.FromRgb(0x1B, 0x22, 0x16), 1);
            }
            else
            {
                Background = Ui.Solid(Colors.White, 0.06);
                BorderBrush = Ui.Solid(Colors.White, 0.10);
                BorderThickness = new Thickness(1);
                t.Foreground = Ui.Solid(Ui.Ink, 0.9);
            }

            MouseEnter += delegate { Opacity = 0.85; };
            MouseLeave += delegate { Opacity = 1; };
            MouseLeftButtonDown += delegate { Scale(0.96); };
            MouseLeftButtonUp += delegate
            {
                Scale(1);
                var h = Clicked; if (h != null) h();
            };
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
            var a = new DoubleAnimation(s, new Duration(TimeSpan.FromMilliseconds(140)));
            a.EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }
    }

    // ---------- icon tile ----------
    public class IconTile : Border
    {
        bool _selected;
        public readonly string Glyph;
        public event Action<string> Picked;
        UIElement _icon;
        public static List<IconTile> ActiveGroup = new List<IconTile>();

        public IconTile(string glyphHex)
        {
            Glyph = glyphHex;
            Width = 44; Height = 40;
            CornerRadius = new CornerRadius(10);
            Background = Ui.Solid(Colors.White, 0.04);
            BorderThickness = new Thickness(0);
            Cursor = Cursors.Hand;

            _icon = LucideHost.Build(glyphHex, 18, false);
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
            Background = on ? Ui.Solid(Ui.Accent, 0.16) : Ui.Solid(Colors.White, 0.04);
            var pt = _icon as System.Windows.Shapes.Path;
            if (pt != null) pt.Stroke = on ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Colors.White, 0.95);
        }
    }
}
