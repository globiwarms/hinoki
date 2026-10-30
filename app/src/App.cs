// Hinoki — entry point & application lifecycle
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using WF = System.Windows.Forms;

namespace Hinoki
{
    public class App : Application
    {
        public static AppConfig Config;
        static Mutex _mutex;
        static EventWaitHandle _showSettings;
        static App _app;
        static bool _firstRun;

        [STAThread]
        public static void Main(string[] args)
        {
            bool createdNew;
            _mutex = new Mutex(true, "Local\\Hinoki.SingleInstance", out createdNew);
            if (!createdNew)
            {
                // already running — just ask the running copy to open settings
                try
                {
                    var ev = EventWaitHandle.OpenExisting("Local\\Hinoki.ShowSettings");
                    ev.Set();
                }
                catch { }
                return;
            }

            _showSettings = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\Hinoki.ShowSettings");

            bool openNow = false, silent = false;
            string snapshot = null;
            foreach (var a in args)
            {
                if (a == "--settings" || a == "/settings") openNow = true;
                if (a == "--silent" || a == "/silent") silent = true;
                if (a == "--snapshot") snapshot = "settings";
                if (a == "--snapshot-overlay") snapshot = "overlay";
            }

            _app = new App();
            _app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // crash logging — never take the tray down silently
            string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hinoki_crash.log");
            Action<string> log = delegate(string msg)
            {
                try { System.IO.File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss.fff ") + msg + "\r\n"); }
                catch { }
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                log("DOMAIN: " + e.ExceptionObject);
            };
            _app.DispatcherUnhandledException += delegate(object s, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            {
                log("DISPATCHER: " + e.Exception);
                e.Handled = true; // keep the tray alive
            };

            _app.Startup += delegate
            {
                try
                {
                    _firstRun = !System.IO.File.Exists(AppConfig.FilePath);
                    Config = AppConfig.Load();
                    MouseHook.Intercept = Config.InterceptClick;
                    MouseHook.Install();
                    MouseHook.MiddleDown += OnMiddleDown;

                    // tray lives on the dispatcher thread
                    Tray.Init();

                    var waiter = new Thread(delegate()
                    {
                        while (true)
                        {
                            try { _showSettings.WaitOne(); } catch { return; }
                            _app.Dispatcher.BeginInvoke(new Action(delegate { OpenSettings(); }));
                        }
                    });
                    waiter.IsBackground = true;
                    waiter.Start();

                    // wire overlay diagnostics to the crash log
                    OverlayWindow.Log = delegate(string m) { log(m); };

                    // warm up WPF window creation so the first menu bloom is instant
                    _app.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        try
                        {
                            var warm = new Window
                            {
                                Width = 10, Height = 10,
                                WindowStyle = WindowStyle.None,
                                ShowInTaskbar = false,
                                ShowActivated = false,
                                AllowsTransparency = true,
                                Background = Brushes.Transparent,
                                Opacity = 0,
                                Topmost = true,
                                Left = -40, Top = -40
                            };
                            warm.Show();
                            warm.Close();
                        }
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                    if (snapshot != null)
                    {
                        RunSnapshot(snapshot);
                        return;
                    }

                    if (openNow) OpenSettings();

                    if (!silent)
                    {
                        if (_firstRun)
                            Tray.Balloon("Hinoki работает", "Нажмите среднюю кнопку мыши, чтобы открыть меню. Настройки — двойной клик по иконке в трее.");
                        else
                            Tray.Balloon("Hinoki работает", "Средняя кнопка мыши — меню. Двойной клик по иконке — настройки.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hinoki не смог запуститься:\n" + ex.Message, "Hinoki",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ExitApp();
                }
            };
            _app.Run();
        }

        // Renders a window off-screen to PNG (used by build tooling / docs)
        static void RunSnapshot(string kind)
        {
            _app.Dispatcher.BeginInvoke(new Action(delegate
            {
                try
                {
                    Window target;
                    string file;
                    if (kind == "overlay")
                    {
                        var ov = new OverlayWindow();
                        ov.Open(960, 540);
                        target = ov;
                        file = "overlay.png";
                    }
                    else
                    {
                        target = new SettingsWindow();
                        ((SettingsWindow)target).Show();
                        file = "settings.png";
                    }

                    var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1300) };
                    t.Tick += delegate
                    {
                        t.Stop();
                        try
                        {
                            string outPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                            var src = PresentationSource.FromVisual(target);
                            double dpiK = src != null ? 1.0 : 1.0;
                            int pw = (int)(target.ActualWidth * 2);
                            int ph = (int)(target.ActualHeight * 2);
                            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(pw, ph, 192, 192,
                                System.Windows.Media.PixelFormats.Pbgra32);
                            rtb.Render((System.Windows.Media.Visual)target.Content);
                            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                            using (var fs = System.IO.File.Create(outPath)) enc.Save(fs);
                        }
                        catch (Exception ex) { try { System.IO.File.AppendAllText(
                            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hinoki_crash.log"),
                            "SNAPSHOT: " + ex); } catch { }
                        }
                        finally { ExitApp(); }
                    };
                    t.Start();
                }
                catch (Exception ex)
                {
                    try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hinoki_crash.log"),
                        "SNAPSHOT2: " + ex); } catch { }
                    ExitApp();
                }
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        static void OnMiddleDown(int x, int y)
        {
            try
            {
                // middle click inside the settings window — swallow only
                if (SettingsWindow.ContainsPhysicalPoint(x, y)) return;

                if (OverlayWindow.Current != null)
                {
                    OverlayWindow.Current.SafeClose();
                    return;
                }
                var ov = new OverlayWindow();
                ov.Open(x, y);
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hinoki_crash.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff ") + "MIDDLEDOWN: " + ex + "\r\n"); } catch { }
            }
        }

        public static void OpenSettings()
        {
            SettingsWindow.ShowOrFocus();
        }

        public static void TestMenu()
        {
            var p = WF.Cursor.Position;
            OnMiddleDown(p.X, p.Y);
        }

        public static void SaveConfig()
        {
            if (Config != null) Config.Save();
            if (Tray.HasIcon) Tray.Sync();
        }

        public static void ApplyAutostart()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (Config.Autostart)
                        key.SetValue("Hinoki", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
                    else if (key.GetValue("Hinoki") != null)
                        key.DeleteValue("Hinoki");
                }
            }
            catch { }
            Tray.Sync();
        }

        public static void ResetConfig(Window owner)
        {
            try { if (System.IO.File.Exists(AppConfig.FilePath)) System.IO.File.Delete(AppConfig.FilePath); }
            catch { }
            Config = AppConfig.Defaults();
            Config.Save();
            MouseHook.Intercept = Config.InterceptClick;
            ApplyAutostart();
            // reopen settings so everything refreshes
            var w = SettingsWindow.ActiveWindow;
            if (w != null) w.Close();
            OpenSettings();
        }

        public static void ExitApp()
        {
            try
            {
                MouseHook.Uninstall();
                Tray.Dispose();
            }
            catch { }
            if (_app != null) _app.Shutdown();
            Environment.Exit(0);
        }
    }
}
