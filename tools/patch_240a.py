"""v2.4.0: flicker fix (prepare before Show), toast integration, confirm dialogs, toggles."""
import io

# ============ FlowerMenu: split PrepareIn / PlayIn ============
p = "app/src/FlowerMenu.cs"
s = io.open(p, encoding="utf-8").read()
s = s.replace("""        public void PlayIn()
        {
            var cfgApp = App.Config;
            string style = cfgApp != null ? cfgApp.EntranceStyle : "spring";
            double sp = cfgApp != null ? cfgApp.AnimSpeed / 100.0 : 1.0;
            bool instant = cfgApp != null && (!cfgApp.AnimationsEnabled || style == "instant");

            if (instant)
            {
                Opacity = 1;
                var cstI = (ScaleTransform)_centerGroup.RenderTransform;
                cstI.ScaleX = 1; cstI.ScaleY = 1;
                _centerGroup.Opacity = 1;
                return;
            }

            Opacity = 0;""",
"""        /// <summary>Set all initial (hidden) states BEFORE the window is shown — kills first-frame flicker.</summary>
        public void PrepareIn()
        {
            Opacity = 0;
            if (_centerGroup != null)
            {
                var cst0 = (ScaleTransform)_centerGroup.RenderTransform;
                cst0.ScaleX = 0.55; cst0.ScaleY = 0.55;
                _centerGroup.Opacity = 0;
            }
        }

        public void PlayIn()
        {
            var cfgApp = App.Config;
            string style = cfgApp != null ? cfgApp.EntranceStyle : "spring";
            double sp = cfgApp != null ? cfgApp.AnimSpeed / 100.0 : 1.0;
            bool instant = cfgApp != null && (!cfgApp.AnimationsEnabled || style == "instant");

            if (instant)
            {
                Opacity = 1;
                var cstI = (ScaleTransform)_centerGroup.RenderTransform;
                cstI.ScaleX = 1; cstI.ScaleY = 1;
                _centerGroup.Opacity = 1;
                return;
            }

            Opacity = 0;""")

# ============ OverlayWindow: prepare before Show + right-click toggle ============
p2 = "app/src/OverlayWindow.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("""            Show();
            Sounds.Play("open");
            _flower.RefreshToggleStates();
            _flower.PlayIn();""",
"""            _flower.PrepareIn();   // hide everything before the first frame — no flicker
            Show();
            Sounds.Play("open");
            _flower.RefreshToggleStates();
            _flower.PlayIn();""")
s2 = s2.replace("""            // click on empty glass inside the window closes it
            MouseLeftButtonDown += delegate { SafeClose(); };
            MouseRightButtonDown += delegate { SafeClose(); };""",
"""            // click on empty glass inside the window closes it
            MouseLeftButtonDown += delegate { SafeClose(); };
            MouseRightButtonDown += delegate
            {
                if (App.Config == null || App.Config.CloseOnRightClick) SafeClose();
            };""")

io.open(p2, "w", encoding="utf-8").write(s2)
print("overlay ok")

# ============ ActionEngine: confirm for power actions + toast balloons ============
p3 = "app/src/ActionEngine.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace('''                case "lock_pc": Native.LockWorkStation(); break;
                case "sleep_pc": Native.SetSuspendState(false, false, false); break;
                case "restart_pc": Shell("shutdown", "/r /t 0"); break;
                case "shutdown_pc": Shell("shutdown", "/s /t 0"); break;''',
'''                case "lock_pc": Native.LockWorkStation(); break;
                case "sleep_pc": PowerAction("moon", "Спящий режим", "Сон", "Уйти в спящий режим?", "Спать", delegate { Native.SetSuspendState(false, false, false); }); break;
                case "restart_pc": PowerAction("rotate-cw", "Перезагрузка", "Перезагрузить", "Перезагрузить компьютер сейчас?", "Перезагрузить", delegate { Shell("shutdown", "/r /t 0"); }); break;
                case "shutdown_pc": PowerAction("power", "Выключение", "Выключить", "Выключить компьютер сейчас?", "Выключить", delegate { Shell("shutdown", "/s /t 0"); }); break;''')
s3 = s3.replace('''        static void Shell(string exe, string args)''',
'''        /// <summary>Power actions run on a worker thread — marshal the confirm dialog to the UI thread.</summary>
        static void PowerAction(string icon, string title, string confirmLabel, string question, string verb, Action act)
        {
            bool confirm = App.Config == null || App.Config.ConfirmPower;
            bool approved = true;
            if (confirm)
            {
                var d = Application.Current != null ? Application.Current.Dispatcher : null;
                if (d != null)
                    d.Invoke(new Action(delegate
                    {
                        approved = GlassConfirm.Show(title, question, verb, icon, true);
                    }));
            }
            if (approved) act();
            else Report("Hinoki", "Отменено");
        }

        static void Shell(string exe, string args)''')
s3 = s3.replace('''                    Report("Hinoki", "Скриншот сохранён: " + file);''',
                '''                    Toast.Show("Скриншот готов", file, "camera");''')
io.open(p3, "w", encoding="utf-8").write(s3)
print("engine ok")

# ============ Tray: balloons -> toasts ============
p4 = "app/src/Tray.cs"
s4 = io.open(p4, encoding="utf-8").read()
s4 = s4.replace("""        public static void Balloon(string title, string text)
        {
            if (_icon == null) return;
            try { _icon.ShowBalloonTip(3000, title, text, WF.ToolTipIcon.None); }
            catch { }
        }""",
"""        public static void Balloon(string title, string text)
        {
            Toast.Show(title, text, "bell");
        }""")
io.open(p4, "w", encoding="utf-8").write(s4)
print("tray ok")

# ============ App: startup balloons -> toast with brand icon ============
p5 = "app/src/App.cs"
s5 = io.open(p5, encoding="utf-8").read()
s5 = s5.replace('''                        if (_firstRun)
                            Tray.Balloon("Hinoki работает", "Нажмите среднюю кнопку мыши, чтобы открыть меню. Настройки — двойной клик по иконке в трее.");
                        else
                            Tray.Balloon("Hinoki работает", "Средняя кнопка мыши — меню. Двойной клик по иконке — настройки.");''',
'''                        if (_firstRun)
                            Toast.Show("Hinoki работает", "Нажмите среднюю кнопку мыши, чтобы открыть меню", "flower-2");
                        else
                            Toast.Show("Hinoki работает", "Средняя кнопка мыши — меню", "flower-2");''')
s5 = s5.replace('''                    Report("Hinoki", "Отменено");''', '''                    Toast.Show("Отменено", "Действие отменено", "info");''') if 'Report("Hinoki", "Отменено")' in s5 else s5
io.open(p5, "w", encoding="utf-8").write(s5)
print("app ok")

# ============ Config v8: ConfirmPower + CloseOnRightClick ============
p6 = "app/src/Config.cs"
s6 = io.open(p6, encoding="utf-8").read()
s6 = s6.replace("        public bool OpenShotFolder { get; set; }      // open explorer after screenshot",
'''        public bool OpenShotFolder { get; set; }      // open explorer after screenshot
        public bool ConfirmPower { get; set; }        // glass confirm for sleep/restart/shutdown
        public bool CloseOnRightClick { get; set; }   // right-click closes the menu''')
s6 = s6.replace("""                        if (cfg.Version < 7)
                        {
                            cfg.DeepSettingsTheme = false;
                            cfg.ShowCenterButton = true;
                            cfg.ShowNotifications = true;
                            cfg.OpenShotFolder = false;
                        }
                        cfg.Version = 7;""",
"""                        if (cfg.Version < 7)
                        {
                            cfg.DeepSettingsTheme = false;
                            cfg.ShowCenterButton = true;
                            cfg.ShowNotifications = true;
                            cfg.OpenShotFolder = false;
                        }
                        if (cfg.Version < 8)
                        {
                            cfg.ConfirmPower = true;
                            cfg.CloseOnRightClick = true;
                        }
                        cfg.Version = 8;""")
s6 = s6.replace("            c.Version = 7;", "            c.Version = 8;")
s6 = s6.replace("            c.OpenShotFolder = false;",
"""            c.OpenShotFolder = false;
            c.ConfirmPower = true;
            c.CloseOnRightClick = true;""")
io.open(p6, "w", encoding="utf-8").write(s6)
print("config v8 ok")
