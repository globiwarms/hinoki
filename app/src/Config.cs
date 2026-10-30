// Hinoki — configuration model + persistence (%APPDATA%\Hinoki\config.json)
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Web.Script.Serialization;

namespace Hinoki
{
    public class PetalConfig
    {
        public string Label { get; set; }
        public string Icon { get; set; }     // hex glyph code, e.g. "E720"
        public string Action { get; set; }   // see Actions.Keys
        public string Target { get; set; }   // app path / url / key combo
        public string Args { get; set; }     // launch arguments

        public PetalConfig() { }
        public PetalConfig(string label, string icon, string action, string target, string args)
        {
            Label = label; Icon = icon; Action = action; Target = target; Args = args;
        }

        public PetalConfig Clone()
        {
            return new PetalConfig(Label, Icon, Action, Target, Args);
        }
    }

    public class AppConfig
    {
        public int Version { get; set; }

        // behavior
        public bool InterceptClick { get; set; }
        public bool ShowAtCursor { get; set; }
        public bool CloseAfterAction { get; set; }
        public bool ShowLabels { get; set; }
        public bool Autostart { get; set; }
        public int MenuSizePercent { get; set; }
        public string GlobalHotkey { get; set; }
        public string DiscordMuteKeys { get; set; }
        public string DiscordDeafenKeys { get; set; }

        // appearance
        public string AccentHex { get; set; }
        public int PetalOpacity { get; set; }      // 30..100
        public int GlowStrength { get; set; }      // 0..100
        public bool BlurBackground { get; set; }   // glass blur inside settings window
        public bool LightGlass { get; set; }       // light glass theme for petals

        // sounds
        public bool SoundEnabled { get; set; }
        public int SoundVolume { get; set; }       // 0..100
        public bool SoundOpen { get; set; }
        public bool SoundClose { get; set; }
        public bool SoundClick { get; set; }

        public List<PetalConfig> Petals { get; set; }

        public static string Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hinoki"); }
        }
        public static string FilePath
        {
            get { return Path.Combine(Dir, "config.json"); }
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var ser = new JavaScriptSerializer();
                    var cfg = ser.Deserialize<AppConfig>(File.ReadAllText(FilePath));
                    if (cfg != null && cfg.Petals != null && cfg.Petals.Count >= 2)
                    {
                        if (cfg.Version < 2)
                        {
                            // v1.0 config: new options were absent — seed them with defaults
                            cfg.Version = 2;
                            cfg.CloseAfterAction = true;
                            cfg.ShowLabels = true;
                            cfg.AccentHex = "A8B899";
                            cfg.PetalOpacity = 92;
                            cfg.GlowStrength = 40;
                            cfg.LightGlass = false;
                            cfg.SoundEnabled = true;
                            cfg.SoundVolume = 55;
                            cfg.SoundOpen = true;
                            cfg.SoundClose = true;
                            cfg.SoundClick = true;
                            cfg.GlobalHotkey = "";
                        }
                        cfg.Normalize();
                        return cfg;
                    }
                }
            }
            catch { }
            return Defaults();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var ser = new JavaScriptSerializer();
                File.WriteAllText(FilePath, ser.Serialize(this));
            }
            catch { }
        }

        public void Normalize()
        {
            if (MenuSizePercent < 50) MenuSizePercent = 50;
            if (MenuSizePercent > 100) MenuSizePercent = 100;
            if (PetalOpacity < 20) PetalOpacity = 20;
            if (PetalOpacity > 100) PetalOpacity = 100;
            if (GlowStrength < 0) GlowStrength = 0;
            if (GlowStrength > 100) GlowStrength = 100;
            if (SoundVolume < 0) SoundVolume = 0;
            if (SoundVolume > 100) SoundVolume = 100;
            if (string.IsNullOrEmpty(AccentHex)) AccentHex = "A8B899";
            if (string.IsNullOrEmpty(GlobalHotkey)) GlobalHotkey = "";
            if (string.IsNullOrEmpty(DiscordMuteKeys)) DiscordMuteKeys = "Ctrl+Shift+M";
            if (string.IsNullOrEmpty(DiscordDeafenKeys)) DiscordDeafenKeys = "Ctrl+Shift+D";
            if (Petals == null) Petals = new List<PetalConfig>();
            while (Petals.Count < 2)
                Petals.Add(new PetalConfig("Лепесток", "E713", "none", "", ""));
            if (Petals.Count > 8) Petals.RemoveRange(8, Petals.Count - 8);
            foreach (var p in Petals)
            {
                if (p.Label == null) p.Label = "";
                if (p.Icon == null) p.Icon = "E713";
                if (p.Action == null) p.Action = "none";
                if (p.Target == null) p.Target = "";
                if (p.Args == null) p.Args = "";
            }
        }

        public static AppConfig Defaults()
        {
            var c = new AppConfig();
            c.Version = 2;
            c.InterceptClick = true;
            c.ShowAtCursor = true;
            c.CloseAfterAction = true;
            c.ShowLabels = true;
            c.Autostart = false;
            c.MenuSizePercent = 78;
            c.GlobalHotkey = "";
            c.DiscordMuteKeys = "Ctrl+Shift+M";
            c.DiscordDeafenKeys = "Ctrl+Shift+D";
            c.AccentHex = "A8B899";
            c.PetalOpacity = 92;
            c.GlowStrength = 40;
            c.BlurBackground = true;
            c.LightGlass = false;
            c.SoundEnabled = true;
            c.SoundVolume = 55;
            c.SoundOpen = true;
            c.SoundClose = true;
            c.SoundClick = true;
            c.Petals = new List<PetalConfig>
            {
                new PetalConfig("Мут микро",    "E720", "discord_mute",  "", ""),
                new PetalConfig("Мут звука ДС", "E767", "discord_deafen","", ""),
                new PetalConfig("Мут ПК",       "E74F", "volume_mute",   "", ""),
                new PetalConfig("Музыка",       "E768", "media",         "", ""),
                new PetalConfig("Приложение",   "E71D", "app",           "", ""),
                new PetalConfig("Ссылка",       "E71B", "url",           "", "")
            };
            return c;
        }
    }

    /// <summary>Design tokens. Accent follows the user's palette choice.</summary>
    public static class Ui
    {
        public static readonly FontFamily Text = new FontFamily("Segoe UI Variable Display, Segoe UI");
        public static readonly FontFamily Icons = new FontFamily("Segoe MDL2 Assets, Segoe Fluent Icons");

        public static Color Accent
        {
            get
            {
                var cfg = App.Config;
                string hex = cfg != null && !string.IsNullOrEmpty(cfg.AccentHex) ? cfg.AccentHex : "A8B899";
                try
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return Color.FromRgb(r, g, b);
                }
                catch { return Color.FromRgb(0xA8, 0xB8, 0x99); }
            }
        }

        public static readonly Color Ink    = Color.FromRgb(0xF2, 0xF4, 0xF0);
        public static readonly Color InkDim = Color.FromRgb(0x9A, 0xA2, 0x98);
        public static readonly Color Danger = Color.FromRgb(0xE5, 0x6C, 0x5C);

        public static SolidColorBrush Solid(Color c, double a)
        {
            var b = c; b.A = (byte)Math.Max(0, Math.Min(255, a * 255));
            var sb = new SolidColorBrush(b); sb.Freeze(); return sb;
        }

        public static LinearGradientBrush GlassFill(double topA, double bottomA)
        {
            var g = new LinearGradientBrush();
            g.StartPoint = new System.Windows.Point(0, 0);
            g.EndPoint = new System.Windows.Point(0, 1);
            var top = Color.FromArgb((byte)Math.Min(255, topA * 255), 0xFF, 0xFF, 0xFF);
            var bot = Color.FromArgb((byte)Math.Min(255, bottomA * 255), 0xE8, 0xEC, 0xF2);
            g.GradientStops.Add(new GradientStop(top, 0));
            g.GradientStops.Add(new GradientStop(bot, 1));
            g.Freeze();
            return g;
        }

        public static char Glyph(string hex)
        {
            int code;
            if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out code)) code = 0xE713;
            return (char)code;
        }
    }

    public static class Actions
    {
        public static readonly string[] Keys = new string[]
        {
            "none", "discord_mute", "discord_deafen",
            "volume_mute", "volume_up", "volume_down",
            "media", "media_next", "media_prev",
            "app", "url", "hotkey",
            "screenshot_file", "screenshot_buffer",
            "show_desktop", "minimize_window", "topmost_window",
            "lock_pc", "sleep_pc", "restart_pc", "shutdown_pc",
            "task_manager", "windows_settings", "calculator",
            "emoji_panel", "paste_clipboard",
            "open_steam", "open_config", "open_settings"
        };

        public static readonly string[] Names = new string[]
        {
            "Ничего",
            "Мут микрофона — Discord",
            "Мут звука — Discord",
            "Мут звука — вся система",
            "Громче",
            "Тише",
            "Play / Pause — музыка",
            "Следующий трек",
            "Предыдущий трек",
            "Запустить приложение или steam://",
            "Открыть ссылку",
            "Своё сочетание клавиш",
            "Скриншот экрана → в Изображения",
            "Скриншот области → в буфер",
            "Показать рабочий стол",
            "Свернуть активное окно",
            "Поверх всех окон — переключить",
            "Заблокировать ПК",
            "Спящий режим",
            "Перезагрузка",
            "Выключение",
            "Диспетчер задач",
            "Параметры Windows",
            "Калькулятор",
            "Панель эмодзи",
            "Вставить из буфера",
            "Открыть Steam",
            "Открыть папку настроек Hinoki",
            "Открыть настройки Hinoki"
        };

        public static string NameOf(string key)
        {
            int i = Array.IndexOf(Keys, key);
            return i >= 0 ? Names[i] : Names[0];
        }

        public static string IconOf(string key)
        {
            switch (key)
            {
                case "discord_mute": return "E720";
                case "discord_deafen": return "E767";
                case "volume_mute": return "E74F";
                case "volume_up": return "E995";
                case "volume_down": return "E994";
                case "media": return "E768";
                case "media_next": return "E892";
                case "media_prev": return "E892";
                case "app": return "E71D";
                case "url": return "E71B";
                case "hotkey": return "E765";
                case "screenshot_file": return "E722";
                case "screenshot_buffer": return "E7A5";
                case "show_desktop": return "EA3A";
                case "minimize_window": return "E921";
                case "topmost_window": return "E718";
                case "lock_pc": return "E72E";
                case "sleep_pc": return "E708";
                case "restart_pc": return "E777";
                case "shutdown_pc": return "E7E8";
                case "task_manager": return "E9D9";
                case "windows_settings": return "E713";
                case "calculator": return "E8EF";
                case "emoji_panel": return "E76E";
                case "paste_clipboard": return "E77F";
                case "open_steam": return "E7FC";
                case "open_config": return "E8B7";
                case "open_settings": return "E713";
                default: return "E713";
            }
        }
    }
}
