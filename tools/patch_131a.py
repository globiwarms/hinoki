"""v1.3.1 patch A: config v6 (no discord, mic toggle), action names/icons -> lucide."""
import io

p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()

# actions: remove discord pair, add mic_toggle, copy already there
s = s.replace('''            "none", "discord_mute", "discord_deafen",
            "volume_mute", "volume_up", "volume_down",''',
'''            "none", "mic_toggle", "volume_mute",
            "volume_up", "volume_down",''')
s = s.replace('''            "none",
            "Мут микрофона — Discord",
            "Мут звука — Discord",
            "Мут звука — вся система",
            "Громче",''',
'''            "none",
            "Микрофон вкл / выкл",
            "Звук вкл / выкл",
            "Громче",''')

# icons -> lucide names
s = s.replace('''        public static string IconOf(string key)
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
                case "screenshot_window": return "E7A5";
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
                case "open_downloads": return "E896";
                case "open_desktop": return "E9F9";
                case "open_documents": return "E8A5";
                case "copy": return "E8C8";
                case "monitor_off": return "E7F4";
                case "clear_recycle_bin": return "E74D";
                case "open_discord": return "E8BD";
                case "open_spotify": return "EC4F";
                case "search_start": return "E721";
                case "notification_center": return "E7E7";
                default: return "E713";
            }
        }''',
'''        public static string IconOf(string key)
        {
            switch (key)
            {
                case "mic_toggle": return "mic";
                case "volume_mute": return "volume-x";
                case "volume_up": return "volume-2";
                case "volume_down": return "volume-1";
                case "media": return "play";
                case "media_next": return "skip-forward";
                case "media_prev": return "skip-back";
                case "app": return "app-window";
                case "url": return "link-2";
                case "hotkey": return "keyboard";
                case "screenshot_file": return "camera";
                case "screenshot_buffer": return "crop";
                case "screenshot_window": return "image";
                case "show_desktop": return "monitor";
                case "minimize_window": return "minimize-2";
                case "topmost_window": return "pin";
                case "lock_pc": return "lock";
                case "sleep_pc": return "moon";
                case "restart_pc": return "rotate-cw";
                case "shutdown_pc": return "power";
                case "task_manager": return "activity";
                case "windows_settings": return "settings";
                case "calculator": return "calculator";
                case "emoji_panel": return "smile";
                case "paste_clipboard": return "clipboard";
                case "open_steam": return "gamepad-2";
                case "open_config": return "folder-open";
                case "open_settings": return "settings";
                case "open_downloads": return "download";
                case "open_desktop": return "monitor";
                case "open_documents": return "file-text";
                case "copy": return "copy";
                case "monitor_off": return "monitor-off";
                case "clear_recycle_bin": return "trash-2";
                case "open_discord": return "message-circle";
                case "open_spotify": return "music";
                case "search_start": return "search";
                case "notification_center": return "bell";
                default: return "settings";
            }
        }''')

# entrance styles +2
s = s.replace('if (EntranceStyle != "spring" && EntranceStyle != "smooth" && EntranceStyle != "instant") EntranceStyle = "spring";',
              'if (EntranceStyle != "spring" && EntranceStyle != "smooth" && EntranceStyle != "instant" && EntranceStyle != "elastic" && EntranceStyle != "bounce") EntranceStyle = "spring";')

# migration v5->v6: replace discord petals
s = s.replace("""                        if (cfg.Version < 4)
                        {""",
"""                        if (cfg.Version < 6)
                        {
                            foreach (var p in cfg.Petals)
                            {
                                if (p.Action == "discord_mute") { p.Action = "mic_toggle"; p.Label = "Микрофон"; p.Icon = "mic"; }
                                else if (p.Action == "discord_deafen") { p.Action = "screenshot_file"; p.Label = "Скриншот"; p.Icon = "camera"; }
                            }
                        }
                        if (cfg.Version < 4)
                        {""")
s = s.replace("cfg.Version = 5;", "cfg.Version = 6;")
s = s.replace("            c.Version = 5;", "            c.Version = 6;")

# default petals -> lucide + mic toggle first
s = s.replace('''            c.Petals = new List<PetalConfig>
            {
                new PetalConfig("Мут микро",    "E720", "discord_mute",  "", ""),
                new PetalConfig("Мут звука ДС", "E767", "discord_deafen","", ""),
                new PetalConfig("Мут ПК",       "E74F", "volume_mute",   "", ""),
                new PetalConfig("Музыка",       "E768", "media",         "", ""),
                new PetalConfig("Приложение",   "E71D", "app",           "", ""),
                new PetalConfig("Ссылка",       "E71B", "url",           "", "")
            };''',
'''            c.Petals = new List<PetalConfig>
            {
                new PetalConfig("Микрофон", "mic",       "mic_toggle", "", ""),
                new PetalConfig("Звук",     "volume-2",  "volume_mute", "", ""),
                new PetalConfig("Скриншот", "camera",    "screenshot_file", "", ""),
                new PetalConfig("Музыка",   "play",      "media", "", ""),
                new PetalConfig("Приложение", "app-window", "app", "", ""),
                new PetalConfig("Ссылка",   "link-2",    "url", "", "")
            };''')

io.open(p, "w", encoding="utf-8").write(s)
print("config v6 ok")

# ActionEngine: mic toggle impl + remove discord branches
p2 = "app/src/ActionEngine.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace('''                case "discord_mute": SendToDiscord(App.Config.DiscordMuteKeys, "Мут микрофона"); break;
                case "discord_deafen": SendToDiscord(App.Config.DiscordDeafenKeys, "Мут звука"); break;
                case "volume_mute": TapKey(0xAD); break;            // VK_VOLUME_MUTE''',
'''                case "mic_toggle":
                {
                    bool muted = Audio.IsMicMuted();
                    Audio.SetMicMuted(!muted);
                    if (!muted) Report("Hinoki", "Микрофон выключен");
                    break;
                }
                case "volume_mute": TapKey(0xAD); break;            // VK_VOLUME_MUTE''')
io.open(p2, "w", encoding="utf-8").write(s2)
print("engine ok")
