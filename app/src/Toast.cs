// Hinoki — custom liquid-glass toast notifications, power menu & confirmation dialog
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace Hinoki
{
    public static class Toast
    {
        internal static ToastWindow _current;

        /// <summary>Show a glass toast near the tray. Safe from any thread.</summary>
        public static void Show(string title, string text, string icon = "info")
        {
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d == null) return;
            d.BeginInvoke(new Action(delegate
            {
                try
                {
                    if (_current != null) { _current.ReplaceContent(title, text, icon); return; }
                    _current = new ToastWindow(title, text, icon);
                    _current.Show();
                }
                catch { }
            }));
        }
    }

    public class ToastWindow : Window
    {
        DispatcherTimer _life;
        TextBlock _title, _text;
        Canvas _iconHost;
        TranslateTransform _slide;
        bool _closing;

        public ToastWindow(string title, string text, string icon)
        {
            Width = 380;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;

            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 18;
            Top = wa.Bottom - 130;

            var chrome = new Border();
            chrome.CornerRadius = new CornerRadius(18);
            var fill = new LinearGradientBrush();
            fill.StartPoint = new Point(0, 0);
            fill.EndPoint = new Point(0.6, 1);
            fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x22, 0x24, 0x2B), 0));
            fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x14, 0x15, 0x1A), 1));
            fill.Freeze();
            chrome.Background = fill;
            chrome.BorderBrush = Ui.Solid(Colors.White, 0.14);
            chrome.BorderThickness = new Thickness(1);
            chrome.Padding = new Thickness(14, 12, 16, 13);
            chrome.MouseLeftButtonDown += delegate { FadeClose(); };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconWrap = new Border();
            iconWrap.Width = 40; iconWrap.Height = 40;
            iconWrap.CornerRadius = new CornerRadius(13);
            iconWrap.Background = Ui.Solid(Ui.Accent, 0.22);
            iconWrap.VerticalAlignment = VerticalAlignment.Center;
            _iconHost = new Canvas();
            _iconHost.Width = 24; _iconHost.Height = 24;
            var p = new System.Windows.Shapes.Path();
            p.Data = Lucide.Get(icon);
            p.Stroke = Ui.Solid(Ui.Accent, 1);
            p.StrokeThickness = 1.6;
            p.StrokeLineJoin = PenLineJoin.Round;
            p.StrokeStartLineCap = PenLineCap.Round;
            p.StrokeEndLineCap = PenLineCap.Round;
            _iconHost.Children.Add(p);
            iconWrap.Child = _iconHost;
            Grid.SetColumn(iconWrap, 0);
            g.Children.Add(iconWrap);

            var stack = new StackPanel();
            stack.Margin = new Thickness(12, 0, 0, 0);
            stack.VerticalAlignment = VerticalAlignment.Center;
            _title = Glass.Label(title, 13, false, true);
            stack.Children.Add(_title);
            _text = Glass.Label(text, 12, true);
            _text.TextWrapping = TextWrapping.Wrap;
            _text.LineHeight = 16;
            _text.Margin = new Thickness(0, 3, 0, 0);
            _text.MaxHeight = 48;
            stack.Children.Add(_text);
            Grid.SetColumn(stack, 1);
            g.Children.Add(stack);

            chrome.Child = g;
            Content = chrome;

            _slide = new TranslateTransform(60, 0);
            RenderTransform = _slide;
            Opacity = 0;
        }

        public void ReplaceContent(string title, string text, string icon)
        {
            if (_life != null) _life.Stop();
            _title.Text = title;
            _text.Text = text;
            ((System.Windows.Shapes.Path)_iconHost.Children[0]).Data = Lucide.Get(icon);
            StartLife();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            StartLife();
        }

        void StartLife()
        {
            var slideIn = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(320)));
            slideIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            _slide.BeginAnimation(TranslateTransform.XProperty, slideIn);
            var fadeIn = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(260)));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            BeginAnimation(OpacityProperty, fadeIn);

            if (_life != null) _life.Stop();
            _life = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.8) };
            _life.Tick += delegate
            {
                _life.Stop();
                FadeClose();
            };
            _life.Start();
        }

        void FadeClose()
        {
            if (_closing) return;
            _closing = true;
            Toast._current = null;
            var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(260)));
            fade.Completed += delegate { Close(); };
            BeginAnimation(OpacityProperty, fade);
            var slideOut = new DoubleAnimation(60, new Duration(TimeSpan.FromMilliseconds(280)));
            _slide.BeginAnimation(TranslateTransform.XProperty, slideOut);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_life != null) _life.Stop();
            if (Toast._current == this) Toast._current = null;
        }
    }

    public static class GlassConfirm
    {
        class ResultHolder { public bool Value; }

        /// <summary>Small glass confirmation dialog. Returns true when confirmed. Blocks caller (call from UI thread).</summary>
        public static bool Show(string title, string text, string confirmLabel, string icon, bool danger)
        {
            var holder = new ResultHolder();
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d == null) return false;
            if (d.CheckAccess()) { BuildAndShow(title, text, confirmLabel, icon, danger, holder); return holder.Value; }
            d.Invoke(new Action(delegate { BuildAndShow(title, text, confirmLabel, icon, danger, holder); }));
            return holder.Value;
        }

        static void BuildAndShow(string title, string text, string confirmLabel, string icon, bool danger, ResultHolder holder)
        {
            try
            {
                var win = new Window();
                win.Width = 380;
                win.SizeToContent = SizeToContent.WidthAndHeight;
                win.WindowStyle = WindowStyle.None;
                win.AllowsTransparency = true;
                win.Background = Brushes.Transparent;
                win.ShowInTaskbar = false;
                win.Topmost = true;
                win.ResizeMode = ResizeMode.NoResize;
                win.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                var chrome = new Border();
                chrome.CornerRadius = new CornerRadius(20);
                var fill = new LinearGradientBrush();
                fill.StartPoint = new Point(0, 0); fill.EndPoint = new Point(0.6, 1);
                fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x20, 0x22, 0x29), 0));
                fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x13, 0x14, 0x19), 1));
                fill.Freeze();
                chrome.Background = fill;
                chrome.BorderBrush = Ui.Solid(Colors.White, 0.13);
                chrome.BorderThickness = new Thickness(1);
                chrome.Padding = new Thickness(22, 20, 22, 18);

                var stack = new StackPanel();

                var iconWrap = new Border();
                iconWrap.Width = 52; iconWrap.Height = 52;
                iconWrap.CornerRadius = new CornerRadius(17);
                iconWrap.Background = Ui.Solid(danger ? Ui.Danger : Ui.Accent, 0.2);
                iconWrap.HorizontalAlignment = HorizontalAlignment.Center;
                var ivb = new Viewbox();
                ivb.Width = 26; ivb.Height = 26;
                var ic = new Canvas(); ic.Width = 24; ic.Height = 24;
                var p = new System.Windows.Shapes.Path();
                p.Data = Lucide.Get(icon);
                p.Stretch = Stretch.Uniform;
                p.Stroke = Ui.Solid(danger ? Ui.Danger : Ui.Accent, 1);
                p.StrokeThickness = 1.6;
                p.StrokeLineJoin = PenLineJoin.Round;
                p.StrokeStartLineCap = PenLineCap.Round;
                p.StrokeEndLineCap = PenLineCap.Round;
                p.Width = 24; p.Height = 24;
                ic.Children.Add(p);
                ivb.Child = ic;
                iconWrap.Child = ivb;
                stack.Children.Add(iconWrap);

                var t = Glass.Label(title, 17, false, true);
                t.HorizontalAlignment = HorizontalAlignment.Center;
                t.Margin = new Thickness(0, 14, 0, 0);
                stack.Children.Add(t);

                var sub = Glass.Label(text, 12.5, true);
                sub.TextWrapping = TextWrapping.Wrap;
                sub.TextAlignment = TextAlignment.Center;
                sub.LineHeight = 17;
                sub.Margin = new Thickness(0, 8, 0, 0);
                stack.Children.Add(sub);

                var btns = new StackPanel();
                btns.Orientation = Orientation.Horizontal;
                btns.HorizontalAlignment = HorizontalAlignment.Center;
                btns.Margin = new Thickness(0, 20, 0, 0);

                var cancel = new PillButton("Отмена", false);
                cancel.Clicked += delegate { win.Close(); };
                btns.Children.Add(cancel);

                var ok = new PillButton(confirmLabel, !danger);
                ok.Margin = new Thickness(10, 0, 0, 0);
                if (danger) ok.Background = Ui.Solid(Ui.Danger, 0.9);
                ok.Clicked += delegate { holder.Value = true; win.Close(); };
                btns.Children.Add(ok);

                stack.Children.Add(btns);
                chrome.Child = stack;
                win.Content = chrome;
                win.ShowDialog();
            }
            catch { }
        }
    }

    public static class PowerMenu
    {
        /// <summary>Glass power dialog: sleep / restart / shutdown. Returns choice or "".</summary>
        public static string Show()
        {
            string result = "";
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d == null) return result;
            if (d.CheckAccess()) { BuildAndShow(ref result); return result; }
            d.Invoke(new Action(delegate { BuildAndShow(ref result); }));
            return result;
        }

        class Choice { public string Value; }

        static void BuildAndShow(ref string result)
        {
            try
            {
                var win = new Window();
                win.Width = 400;
                win.SizeToContent = SizeToContent.WidthAndHeight;
                win.WindowStyle = WindowStyle.None;
                win.AllowsTransparency = true;
                win.Background = Brushes.Transparent;
                win.ShowInTaskbar = false;
                win.Topmost = true;
                win.ResizeMode = ResizeMode.NoResize;
                win.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                var chrome = new Border();
                chrome.CornerRadius = new CornerRadius(20);
                var fill = new LinearGradientBrush();
                fill.StartPoint = new Point(0, 0); fill.EndPoint = new Point(0.6, 1);
                fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x20, 0x22, 0x29), 0));
                fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x13, 0x14, 0x19), 1));
                fill.Freeze();
                chrome.Background = fill;
                chrome.BorderBrush = Ui.Solid(Colors.White, 0.13);
                chrome.BorderThickness = new Thickness(1);
                chrome.Padding = new Thickness(14);

                var stack = new StackPanel();

                var t = Glass.Label("Питание", 16, false, true);
                t.Margin = new Thickness(8, 4, 0, 10);
                stack.Children.Add(t);

                var holder = new Choice();
                string[][] rows = new string[][]
                {
                    new[] { "moon", "Спящий режим", "sleep", "0" },
                    new[] { "rotate-cw", "Перезагрузка", "restart", "0" },
                    new[] { "power", "Выключение", "shutdown", "1" }
                };
                foreach (string[] rowDef in rows)
                {
                    var row = new Border();
                    row.CornerRadius = new CornerRadius(12);
                    row.Padding = new Thickness(12, 9, 12, 10);
                    row.Margin = new Thickness(0, 2, 0, 2);
                    row.Cursor = Cursors.Hand;
                    row.Background = Ui.Solid(Colors.White, 0.04);

                    var g = new Grid();
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var ic = LucideHost.Build(rowDef[0], 18, false);
                    var icHost = new ContentControl();
                    icHost.Content = ic;
                    icHost.Width = 20; icHost.Height = 20;
                    icHost.VerticalAlignment = VerticalAlignment.Center;
                    if (rowDef[3] == "1") ((System.Windows.Shapes.Path)ic).Stroke = Ui.Solid(Ui.Danger, 1);
                    Grid.SetColumn(icHost, 0);
                    g.Children.Add(icHost);
                    var tl = Glass.Label(rowDef[1], 13.5, false, true);
                    tl.Margin = new Thickness(12, 0, 0, 0);
                    if (rowDef[3] == "1") tl.Foreground = Ui.Solid(Ui.Danger, 1);
                    tl.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(tl, 1);
                    g.Children.Add(tl);
                    row.Child = g;

                    row.MouseEnter += delegate { row.Background = Ui.Solid(Colors.White, 0.08); };
                    row.MouseLeave += delegate { row.Background = Ui.Solid(Colors.White, 0.04); };
                    row.MouseLeftButtonDown += delegate
                    {
                        holder.Value = rowDef[2];
                        win.Close();
                    };
                    stack.Children.Add(row);
                }

                var cancel = new PillButton("Отмена", false);
                cancel.Margin = new Thickness(0, 10, 0, 0);
                cancel.HorizontalAlignment = HorizontalAlignment.Center;
                cancel.Clicked += delegate { win.Close(); };
                stack.Children.Add(cancel);

                chrome.Child = stack;
                win.Content = chrome;
                win.ShowDialog();
                result = holder.Value;
            }
            catch { }
        }
    }
}
