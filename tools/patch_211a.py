"""v2.1.1: settings redesign pass + new toggles (theme, center button, notifications, shot folder)."""
import io

# ============ Config v7 ============
p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()

s = s.replace("        public int AutoCloseSeconds { get; set; }  // 0 = off",
"""        public int AutoCloseSeconds { get; set; }  // 0 = off
        public bool DeepSettingsTheme { get; set; }   // darker settings background
        public bool ShowCenterButton { get; set; }    // gear in the middle of the flower
        public bool ShowNotifications { get; set; }   // balloons for actions
        public bool OpenShotFolder { get; set; }      // open explorer after screenshot""")

s = s.replace("""                        if (cfg.Version < 5)
                        {
                            cfg.PetalRoundness = 100;
                            cfg.AnimSpeed = 100;
                            cfg.EntranceStyle = "spring";
                            cfg.AutoCloseSeconds = 0;
                        }
                        cfg.Version = 5;""",
"""                        if (cfg.Version < 5)
                        {
                            cfg.PetalRoundness = 100;
                            cfg.AnimSpeed = 100;
                            cfg.EntranceStyle = "spring";
                            cfg.AutoCloseSeconds = 0;
                        }
                        if (cfg.Version < 7)
                        {
                            cfg.DeepSettingsTheme = false;
                            cfg.ShowCenterButton = true;
                            cfg.ShowNotifications = true;
                            cfg.OpenShotFolder = false;
                        }
                        cfg.Version = 7;""")
s = s.replace("            c.Version = 6;", "            c.Version = 7;")
s = s.replace('            c.AutoCloseSeconds = 0;',
'''            c.AutoCloseSeconds = 0;
            c.DeepSettingsTheme = false;
            c.ShowCenterButton = true;
            c.ShowNotifications = true;
            c.OpenShotFolder = false;''')
s = s.replace('            if (EntranceStyle != "spring"', '            if (ShowNotifications == false && ShowNotifications == true) ShowNotifications = true;\n            if (EntranceStyle != "spring"')
io.open(p, "w", encoding="utf-8").write(s)
print("config v7 ok")

# ============ ActionEngine: notifications gate + shot folder ============
p2 = "app/src/ActionEngine.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("""        static void Report(string title, string text)
        {
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d != null)
                d.BeginInvoke(new Action(delegate { Tray.Balloon(title, text); }));
        }""",
"""        static void Report(string title, string text)
        {
            if (App.Config != null && !App.Config.ShowNotifications) return;
            var d = Application.Current != null ? Application.Current.Dispatcher : null;
            if (d != null)
                d.BeginInvoke(new Action(delegate { Tray.Balloon(title, text); }));
        }""")
s2 = s2.replace("""                    Report("Hinoki", "Скриншот сохранён: " + file);""",
"""                    Report("Hinoki", "Скриншот сохранён: " + file);
                    if (App.Config != null && App.Config.OpenShotFolder)
                    {
                        var psi = new ProcessStartInfo("explorer.exe", "/select,\\"" + file + "\\"");
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("engine ok")

# ============ FlowerMenu: optional center ============
p3 = "app/src/FlowerMenu.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace("""            _centerGroup = new StackPanel();
            _centerGroup.HorizontalAlignment = HorizontalAlignment.Center;
            _centerGroup.VerticalAlignment = VerticalAlignment.Top;""",
"""            if (!cfg.ShowCenterButton)
            {
                return; // petals only
            }

            _centerGroup = new StackPanel();
            _centerGroup.HorizontalAlignment = HorizontalAlignment.Center;
            _centerGroup.VerticalAlignment = VerticalAlignment.Top;""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("flower center optional ok")
