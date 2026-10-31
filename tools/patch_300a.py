"""v3.0.0 patch A: Config — exactly 13 user actions, per-petal color & hotkey, migration."""
import io

p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()

# ============ PetalConfig: Color + Hotkey ============
s = s.replace("""    public class PetalConfig
    {
        public string Label { get; set; }
        public string Icon { get; set; }     // lucide icon name
        public string Action { get; set; }   // see Actions.All
        public string Target { get; set; }   // app path / url / key combo
        public string Args { get; set; }     // launch arguments""",
"""    public class PetalConfig
    {
        public string Label { get; set; }
        public string Icon { get; set; }     // lucide icon name
        public string Action { get; set; }   // see Actions.Keys
        public string Target { get; set; }   // app path / folder / cmd / url
        public string Args { get; set; }     // launch arguments
        public string Color { get; set; }    // petal tint hex ("" = default glass)
        public string Hotkey { get; set; }   // per-petal global hotkey ("" = none)""")

# ============ Keys: exactly the user's 13 + none ============
start = s.index("        public static readonly string[] Keys = new string[]")
end = s.index("        };", start) + len("        };")
keys = """        public static readonly string[] Keys = new string[]
        {
            "none", "app", "volume_mute", "mic_toggle", "volume_down", "volume_up",
            "screenshot", "media", "open_folder", "run_cmd", "power_menu",
            "show_desktop", "url"
        };"""
s = s[:start] + keys + s[end:]

# ============ Names aligned ============
start = s.index("        public static readonly string[] Names = new string[]")
end = s.index("        };", start) + len("        };")
names = """        public static readonly string[] Names = new string[]
        {
            "Ничего",
            "Открыть приложение",
            "Звук выкл / вкл",
            "Микрофон выкл / вкл",
            "Тише (25%)",
            "Громче (25%)",
            "Скриншот → в буфер",
            "Пауза / вкл музыки",
            "Открыть выбранную папку",
            "Команда в CMD",
            "Питание: сон / выкл",
            "Свернуть все окна",
            "Открыть ссылку"
        };"""
s = s[:start] + names + s[end:]

# ============ IconOf ============
start = s.index("        public static string IconOf(string key)")
end = s.index("        }", s.index('                case "notification_center"', start)) + len("        }")
iconof = """        public static string IconOf(string key)
        {
            switch (key)
            {
                case "app": return "app-window";
                case "volume_mute": return "volume-x";
                case "mic_toggle": return "mic";
                case "volume_down": return "volume-1";
                case "volume_up": return "volume-2";
                case "screenshot": return "camera";
                case "media": return "play";
                case "open_folder": return "folder";
                case "run_cmd": return "terminal";
                case "power_menu": return "power";
                case "show_desktop": return "monitor";
                case "url": return "link-2";
                default: return "settings";
            }
        }"""
s = s[:start] + iconof + s[end:]

# ============ migration v8 -> v9 ============
s = s.replace("""                        if (cfg.Version < 8)
                        {
                            cfg.ConfirmPower = true;
                            cfg.CloseOnRightClick = true;
                        }
                        cfg.Version = 8;""",
"""                        if (cfg.Version < 8)
                        {
                            cfg.ConfirmPower = true;
                            cfg.CloseOnRightClick = true;
                        }
                        if (cfg.Version < 9)
                        {
                            // v3.0 action set — map old keys onto the 13 user actions
                            var keep = new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "app", "app" }, { "volume_mute", "volume_mute" },
                                { "mic_toggle", "mic_toggle" }, { "volume_down", "volume_down" },
                                { "volume_up", "volume_up" }, { "screenshot", "screenshot" },
                                { "screenshot_file", "screenshot" }, { "screenshot_buffer", "screenshot" },
                                { "screenshot_window", "screenshot" }, { "media", "media" },
                                { "open_folder", "open_folder" }, { "show_desktop", "show_desktop" },
                                { "url", "url" }, { "discord_mute", "mic_toggle" },
                                { "discord_deafen", "volume_mute" },
                                { "sleep_pc", "power_menu" }, { "restart_pc", "power_menu" },
                                { "shutdown_pc", "power_menu" }, { "lock_pc", "power_menu" }
                            };
                            foreach (var p in cfg.Petals)
                            {
                                string mapped;
                                p.Action = keep.TryGetValue(p.Action, out mapped) ? mapped : "none";
                                if (p.Color == null) p.Color = "";
                                if (p.Hotkey == null) p.Hotkey = "";
                            }
                        }
                        cfg.Version = 9;""")
s = s.replace("            c.Version = 8;", "            c.Version = 9;")

# ============ defaults ============
s = s.replace('''            c.Petals = new List<PetalConfig>
            {
                new PetalConfig("Микрофон", "mic",       "mic_toggle", "", ""),
                new PetalConfig("Звук",     "volume-2",  "volume_mute", "", ""),
                new PetalConfig("Скриншот", "camera",    "screenshot_file", "", ""),
                new PetalConfig("Музыка",   "play",      "media", "", ""),
                new PetalConfig("Приложение", "app-window", "app", "", ""),
                new PetalConfig("Ссылка",   "link-2",    "url", "", "")
            };''',
'''            c.Petals = new List<PetalConfig>
            {
                new PetalConfig("Микрофон", "mic",        "mic_toggle", "", ""),
                new PetalConfig("Звук",     "volume-2",   "volume_mute", "", ""),
                new PetalConfig("Скриншот", "camera",     "screenshot", "", ""),
                new PetalConfig("Музыка",   "play",       "media", "", ""),
                new PetalConfig("Приложение", "app-window", "app", "", ""),
                new PetalConfig("Папка",    "folder",     "open_folder", "", "")
            };''')

# normalize color/hotkey
s = s.replace("""                if (p.Args == null) p.Args = "";
            }
        }""",
"""                if (p.Args == null) p.Args = "";
                if (p.Color == null) p.Color = "";
                if (p.Hotkey == null) p.Hotkey = "";
            }
        }""")

io.open(p, "w", encoding="utf-8").write(s)
print("config v9 ok")
