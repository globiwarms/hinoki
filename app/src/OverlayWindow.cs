// Hinoki — the fullscreen glass overlay that blooms around the cursor
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Hinoki
{
    public class OverlayWindow : Window
    {
        FlowerMenu _flower;
        Border _tint;

        /// <summary>Diagnostics sink, wired to the crash log by App.</summary>
        internal static Action<string> Log = delegate { };

        public static OverlayWindow Current;

        public OverlayWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Deactivated += delegate { SafeClose(); };
        }

        public void Open(int cursorX, int cursorY)
        {
            if (Current != null) return;

            // monitor under the cursor (physical px)
            var pt = new Native.POINT { X = cursorX, Y = cursorY };
            IntPtr hmon = Native.MonitorFromPoint(pt, Native.MONITOR_DEFAULTTONEAREST);
            var mi = new Native.MONITORINFOEX();
            mi.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFOEX));
            if (!Native.GetMonitorInfo(hmon, ref mi)) return;

            double dpi = Native.DpiOfMonitor(hmon);
            double k = 96.0 / dpi;

            double monW = (mi.rcMonitor.Right - mi.rcMonitor.Left) * k;
            double monH = (mi.rcMonitor.Bottom - mi.rcMonitor.Top) * k;
            double monX = mi.rcMonitor.Left * k;
            double monY = mi.rcMonitor.Top * k;

            double F = Math.Min(monW, monH) * 0.80 * (App.Config.MenuSizePercent / 80.0);
            F = Math.Max(340, Math.Min(1150, F));

            Left = monX; Top = monY;
            Width = monW; Height = monH;

            var root = new Grid();

            // dim + vignette backdrop
            _tint = new Border();
            _tint.Background = new RadialGradientBrush(Color.FromArgb(140, 6, 7, 10), Color.FromArgb(190, 4, 4, 7));
            _tint.Opacity = 0;
            root.Children.Add(_tint);

            _flower = new FlowerMenu(F);
            double half = F / 2 + 10;
            double fx, fy;
            if (App.Config.ShowAtCursor)
            {
                fx = (cursorX * k) - monX;
                fy = (cursorY * k) - monY;
                fx = Math.Max(half, Math.Min(monW - half, fx));
                fy = Math.Max(half, Math.Min(monH - half, fy));
            }
            else
            {
                fx = monW / 2; fy = monH / 2;
            }
            _flower.SetValue(Canvas.LeftProperty, fx - F / 2);
            _flower.SetValue(Canvas.TopProperty, fy - F / 2);

            var canvas = new Canvas();
            canvas.Children.Add(_flower);
            root.Children.Add(canvas);
            Content = root;

            _flower.PetalActivated += delegate(int i)
            {
                var p = App.Config.Petals[i];
                ActionEngine.Run(p);
                SafeClose();
            };
            _flower.CenterActivated += delegate
            {
                SafeClose();
                App.OpenSettings();
            };

            // click on the empty glass closes the menu
            MouseLeftButtonDown += delegate { SafeClose(); };
            MouseRightButtonDown += delegate { SafeClose(); };

            Current = this;
            KeyboardHook.Install();
            KeyboardHook.EscPressed += OnEsc;

            Log(string.Format("OPEN cursor=({0},{1}) mon=({2}x{3}) dpi={4} flower=({5:0}x{6:0}) at ({7:0},{8:0})",
                cursorX, cursorY, (int)monW, (int)monH, (int)dpi, F, F, fx + monX, fy + monY));

            Show();
            var fade = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(170)));
            fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            _tint.BeginAnimation(OpacityProperty, fade);
            _flower.PlayIn();
        }

        void OnEsc()
        {
            Log("ESC");
            Dispatcher.BeginInvoke(new Action(delegate { SafeClose(); }));
        }

        public void SafeClose()
        {
            if (Current != this || Current == null) return;
            Log("CLOSE");
            Current = null;
            KeyboardHook.EscPressed -= OnEsc;
            KeyboardHook.Uninstall();
            if (_flower != null) _flower.PlayOut(delegate { Close(); });
            else Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            if (App.Config.BlurBackground)
                Native.ApplyAcrylic(hwnd, 0xBE, 0x0D, 0x0E, 0x12);
        }
    }
}
