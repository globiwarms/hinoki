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
        public string Icon { get; set; }     // lucide icon name
        public string Action { get; set; }   // see Actions.Keys
        public string Target { get; set; }   // app path / folder / cmd / url
        public string Args { get; set; }     // launch arguments
        public string Color { get; set; }    // petal tint hex ("" = default glass)
        public string Hotkey { get; set; }   // per-petal global hotkey ("" = none)

        public PetalConfig() { }
        public PetalConfig(string label, string icon, string action, string target, string args)
        {
            Label = label; Icon = icon; Action = action; Target = target; Args = args;
            Color = ""; Hotkey = "";
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
        public bool CloseOnOutsideClick { get; set; }
        public bool AnimationsEnabled { get; set; }
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
        public int CenterOpacity { get; set; }     // 30..100
        public int PetalRoundness { get; set; }    // 50..100 (% of capsule radius)
        public int AnimSpeed { get; set; }         // 50..200 (%)
        public string EntranceStyle { get; set; }  // spring | smooth | instant
        public int AutoCloseSeconds { get; set; }  // 0 = off
        public bool DeepSettingsTheme { get; set; }   // darker settings background
        public bool ShowCenterButton { get; set; }    // gear in the middle of the flower
        public bool ShowNotifications { get; set; }   // balloons for actions
        public bool OpenShotFolder { get; set; }      // open explorer after screenshot
        public bool ConfirmPower { get; set; }        // glass confirm for sleep/restart/shutdown
        public bool CloseOnRightClick { get; set; }   // right-click closes the menu
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
                            // v1.0 config: options absent — seed with defaults
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
                        if (cfg.Version < 3)
                        {
                            cfg.AnimationsEnabled = true;
                            cfg.CloseOnOutsideClick = true;
                        }
                        if (cfg.Version < 6)
                        {
                            var emap = new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "E720", "mic" }, { "E74F", "volume-x" }, { "E767", "volume-2" },
                                { "E768", "play" }, { "E769", "pause" }, { "E8D6", "music" },
                                { "E71D", "app-window" }, { "E71B", "link-2" }, { "E713", "settings" },
                                { "E8B7", "folder" }, { "E765", "keyboard" }, { "E774", "globe" },
                                { "E734", "star" }, { "E72E", "lock" }, { "E722", "camera" },
                                { "E7A5", "crop" }, { "E896", "download" }, { "E898", "upload" },
                                { "E8A5", "file-text" }, { "E7C3", "file-text" }, { "E74D", "trash-2" },
                                { "E8C8", "copy" }, { "E7E8", "power" }, { "E72C", "refresh-cw" },
                                { "E8FB", "refresh-cw" }, { "E946", "info" }, { "E790", "palette" },
                                { "E76E", "smile" }, { "E7F4", "monitor" }, { "EA3A", "monitor" },
                                { "E721", "search" }, { "E8A9", "folder-open" }, { "E8BD", "message-circle" },
                                { "EC4F", "music" }, { "E995", "volume-2" }, { "E994", "volume-1" },
                                { "E892", "skip-forward" }, { "E921", "minimize-2" }, { "E718", "pin" },
                                { "E708", "moon" }, { "E777", "rotate-cw" }, { "E9D9", "activity" },
                                { "E8EF", "calculator" }, { "E77F", "clipboard" }, { "E7FC", "gamepad-2" },
                                { "E9F9", "monitor" }, { "E7E7", "bell" }, { "E787", "calendar" },
                                { "E77B", "user" }, { "E716", "user" }, { "E80F", "grid-2x2" },
                                { "E7A5 ", "crop" }, { "E74E", "file-text" }
                            };
                            foreach (var p in cfg.Petals)
                            {
                                if (p.Action == "discord_mute") { p.Action = "mic_toggle"; p.Label = "Микрофон"; }
                                else if (p.Action == "discord_deafen") { p.Action = "screenshot_file"; p.Label = "Скриншот"; }
                                string newIcon;
                                if (p.Icon != null && p.Icon.StartsWith("E") && emap.TryGetValue(p.Icon.Trim(), out newIcon))
                                    p.Icon = newIcon;
                                else if (p.Icon != null && p.Icon.StartsWith("E")) p.Icon = "settings";
                            }
                        }
                        if (cfg.Version < 4)
                        {
                            cfg.CenterOpacity = 92;
                        }
                        if (cfg.Version < 5)
                        {
                            cfg.PetalRoundness = 100;
                            cfg.AnimSpeed = 100;
                            cfg.EntranceStyle = "spring";
                            cfg.AutoCloseSeconds = 0;
                        }
                        cfg.Version = 6;
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
            if (CenterOpacity < 30) CenterOpacity = 30;
            if (CenterOpacity > 100) CenterOpacity = 100;
            if (PetalRoundness < 50) PetalRoundness = 50;
            if (PetalRoundness > 100) PetalRoundness = 100;
            if (AnimSpeed < 50) AnimSpeed = 50;
            if (AnimSpeed > 200) AnimSpeed = 200;
            if (AutoCloseSeconds < 0) AutoCloseSeconds = 0;
            if (AutoCloseSeconds > 30) AutoCloseSeconds = 30;
            if (EntranceStyle != "spring" && EntranceStyle != "smooth" && EntranceStyle != "instant" && EntranceStyle != "elastic" && EntranceStyle != "bounce") EntranceStyle = "spring";
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
                if (p.Color == null) p.Color = "";
                if (p.Hotkey == null) p.Hotkey = "";
            }
        }

        public static AppConfig Defaults()
        {
            var c = new AppConfig();
            c.Version = 9;
            c.InterceptClick = true;
            c.ShowAtCursor = true;
            c.CloseAfterAction = true;
            c.CloseOnOutsideClick = true;
            c.AnimationsEnabled = true;
            c.ShowLabels = true;
            c.Autostart = false;
            c.MenuSizePercent = 78;
            c.GlobalHotkey = "";
            c.DiscordMuteKeys = "Ctrl+Shift+M";
            c.DiscordDeafenKeys = "Ctrl+Shift+D";
            c.AccentHex = "A8B899";
            c.PetalOpacity = 92;
            c.GlowStrength = 40;
            c.CenterOpacity = 92;
            c.PetalRoundness = 100;
            c.AnimSpeed = 100;
            c.EntranceStyle = "spring";
            c.AutoCloseSeconds = 0;
            c.DeepSettingsTheme = false;
            c.ShowCenterButton = true;
            c.ShowNotifications = true;
            c.OpenShotFolder = false;
            c.ConfirmPower = true;
            c.CloseOnRightClick = true;
            c.BlurBackground = true;
            c.LightGlass = false;
            c.SoundEnabled = true;
            c.SoundVolume = 55;
            c.SoundOpen = true;
            c.SoundClose = true;
            c.SoundClick = true;
            c.Petals = new List<PetalConfig>
            {
                new PetalConfig("Микрофон", "mic",        "mic_toggle", "", ""),
                new PetalConfig("Звук",     "volume-2",   "volume_mute", "", ""),
                new PetalConfig("Скриншот", "camera",     "screenshot", "", ""),
                new PetalConfig("Музыка",   "play",       "media", "", ""),
                new PetalConfig("Приложение", "app-window", "app", "", ""),
                new PetalConfig("Папка",    "folder",     "open_folder", "", "")
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

        public static double AnimFactor()
        {
            var cfg = App.Config;
            return cfg != null ? cfg.AnimSpeed / 100.0 : 1.0;
        }

        public static bool TryParseHex(string hex, out Color color)
        {
            color = Colors.White;
            try
            {
                if (string.IsNullOrEmpty(hex) || hex.Length != 6) return false;
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                color = Color.FromRgb(r, g, b);
                return true;
            }
            catch { return false; }
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
            "none", "app", "volume_mute", "mic_toggle", "volume_down", "volume_up",
            "screenshot", "media", "open_folder", "run_cmd", "power_menu",
            "show_desktop", "url"
        };

        public static readonly string[] Names = new string[]
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
        }
    }
}
