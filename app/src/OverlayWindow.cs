// Hinoki — the floating glass flower: a compact window exactly around the bloom, no dark backdrop
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Hinoki
{
    public class OverlayWindow : Window
    {
        FlowerMenu _flower;
        bool _closing;

        public static OverlayWindow Current;

        /// <summary>Diagnostics sink, wired to the crash log by App.</summary>
        internal static Action<string> Log = delegate { };

        /// <summary>Physical-pixel bounds of the open overlay (for the global hook).</summary>
        static Rect _physBounds;

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
        }

        public void Open(int cursorX, int cursorY)
        {
            if (Current != null) return;

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

            double S = Math.Min(monW, monH) * 0.78 * (App.Config.MenuSizePercent / 78.0);
            S = Math.Max(320, Math.Min(1100, S));

            double pad = S * 0.06; // breathing room for the glow
            double winSize = S + pad * 2;

            double cx, cy;
            if (App.Config.ShowAtCursor)
            {
                cx = (cursorX * k) - monX;
                cy = (cursorY * k) - monY;
            }
            else
            {
                cx = monW / 2; cy = monH / 2;
            }
            cx = Math.Max(winSize / 2, Math.Min(monW - winSize / 2, cx));
            cy = Math.Max(winSize / 2, Math.Min(monH - winSize / 2, cy));

            Left = monX + cx - winSize / 2;
            Top = monY + cy - winSize / 2;
            Width = winSize; Height = winSize;

            // remember physical bounds for the left-click catch
            _physBounds = new Rect(
                (monX + cx - winSize / 2) / k,
                (monY + cy - winSize / 2) / k,
                winSize / k, winSize / k);

            _flower = new FlowerMenu(S);
            Canvas.SetLeft(_flower, pad);
            Canvas.SetTop(_flower, pad);

            var canvas = new Canvas();
            canvas.Children.Add(_flower);
            canvas.Background = Brushes.Transparent;
            Content = canvas;

            _flower.PetalActivated += delegate (int i)
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
            };
            _flower.CenterActivated += delegate
            {
                SafeClose();
                App.OpenSettings();
            };

            // click on empty glass inside the window closes it
            MouseLeftButtonDown += delegate { SafeClose(); };
            MouseRightButtonDown += delegate
            {
                if (App.Config == null || App.Config.CloseOnRightClick) SafeClose();
            };

            Current = this;
            KeyboardHook.Install();
            KeyboardHook.EscPressed += OnEsc;

            Log(string.Format("OPEN cursor=({0},{1}) mon=({2}x{3}) dpi={4} flower={5:0} win=({6:0},{7:0},{8:0},{9:0})",
                cursorX, cursorY, (int)monW, (int)monH, (int)dpi, S, Left, Top, Width, Height));

            _flower.PrepareIn();   // hide everything before the first frame — no flicker
            Show();
            Sounds.Play("open");
            _flower.RefreshToggleStates();
            _flower.PlayIn();

            if (App.Config.AutoCloseSeconds > 0)
            {
                var auto = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(App.Config.AutoCloseSeconds) };
                auto.Tick += delegate
                {
                    auto.Stop();
                    if (Current == this) SafeClose();
                };
                auto.Start();
            }
        }

        void RefreshToggleStates()
        {
            _flower.RefreshToggleStates();
        }

        void OnEsc()
        {
            Dispatcher.BeginInvoke(new Action(delegate { SafeClose(); }));
        }

        /// <summary>Is this physical point inside the open overlay window?</summary>
        public static bool ContainsPhysicalPoint(int px, int py)
        {
            return Current != null && _physBounds.Contains(px, py);
        }

        public void SafeClose()
        {
            if (Current != this || _closing) return;
            _closing = true;
            Current = null;
            KeyboardHook.EscPressed -= OnEsc;
            KeyboardHook.Uninstall();
            Sounds.Play("close");
            if (App.Config != null && (!App.Config.AnimationsEnabled || App.Config.EntranceStyle == "instant")) { Close(); return; }
            double sp = App.Config != null ? App.Config.AnimSpeed / 100.0 : 1.0;
            if (_flower != null)
            {
                var fade = new System.Windows.Media.Animation.DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(170 / sp)));
                fade.Completed += delegate { Close(); };
                BeginAnimation(OpacityProperty, fade);
            }
            else Close();
        }


    }
}
