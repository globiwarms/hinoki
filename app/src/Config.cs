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
        public string Action { get; set; }   // see Actions.All
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
        public bool InterceptClick { get; set; }
        public bool ShowAtCursor { get; set; }
        public bool BlurBackground { get; set; }
        public bool Autostart { get; set; }
        public int MenuSizePercent { get; set; }
        public string DiscordMuteKeys { get; set; }
        public string DiscordDeafenKeys { get; set; }
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
                    if (cfg != null && cfg.Petals != null && cfg.Petals.Count >= 3)
                    {
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
            if (MenuSizePercent < 55) MenuSizePercent = 55;
            if (MenuSizePercent > 100) MenuSizePercent = 100;
            if (string.IsNullOrEmpty(DiscordMuteKeys)) DiscordMuteKeys = "Ctrl+Shift+M";
            if (string.IsNullOrEmpty(DiscordDeafenKeys)) DiscordDeafenKeys = "Ctrl+Shift+D";
            if (Petals == null) Petals = new List<PetalConfig>();
            while (Petals.Count < 3)
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
            c.Version = 1;
            c.InterceptClick = true;
            c.ShowAtCursor = true;
            c.BlurBackground = true;
            c.Autostart = false;
            c.MenuSizePercent = 80;
            c.DiscordMuteKeys = "Ctrl+Shift+M";
            c.DiscordDeafenKeys = "Ctrl+Shift+D";
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

    /// <summary>Design tokens — the sage accent comes straight from the logo plate.</summary>
    public static class Ui
    {
        public static readonly FontFamily Text = new FontFamily("Segoe UI Variable Display, Segoe UI");
        public static readonly FontFamily TextLight = new FontFamily("Segoe UI Variable Display Semilight, Segoe UI Semilight, Segoe UI");
        public static readonly FontFamily Icons = new FontFamily("Segoe MDL2 Assets, Segoe Fluent Icons");

        public static readonly Color Accent      = Color.FromRgb(0xA8, 0xB8, 0x99); // sage from the logo
        public static readonly Color AccentSoft  = Color.FromRgb(0x86, 0x96, 0x74);
        public static readonly Color Ink         = Color.FromRgb(0xF2, 0xF4, 0xF0);
        public static readonly Color InkDim      = Color.FromRgb(0x9A, 0xA2, 0x98);
        public static readonly Color Danger      = Color.FromRgb(0xE5, 0x6C, 0x5C);

        public static SolidColorBrush Solid(Color c, double a)
        {
            var b = c; b.A = (byte)(a * 255);
            var sb = new SolidColorBrush(b); sb.Freeze(); return sb;
        }

        public static LinearGradientBrush GlassFill(double topA, double bottomA)
        {
            var g = new LinearGradientBrush();
            g.StartPoint = new System.Windows.Point(0, 0);
            g.EndPoint = new System.Windows.Point(0, 1);
            var top = Color.FromArgb((byte)(topA * 255), 0xFF, 0xFF, 0xFF);
            var bot = Color.FromArgb((byte)(bottomA * 255), 0xE8, 0xEC, 0xF2);
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
            "none", "discord_mute", "discord_deafen", "volume_mute", "media", "app", "url", "hotkey"
        };

        public static readonly string[] Names = new string[]
        {
            "Ничего",
            "Мут микрофона — Discord",
            "Мут звука — Discord",
            "Мут звука — вся система",
            "Play / Pause — Spotify и др.",
            "Запустить приложение или steam://",
            "Открыть ссылку",
            "Своё сочетание клавиш"
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
                case "media": return "E768";
                case "app": return "E71D";
                case "url": return "E71B";
                case "hotkey": return "E765";
                default: return "E713";
            }
        }
    }
}
