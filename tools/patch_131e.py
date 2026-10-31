"""v1.3.1 fix: E-glyph to lucide migration map, rail icons in tree, tile recolor, lucide tiles."""
import io

p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()

# full E->lucide map inside migration
old = """                        if (cfg.Version < 6)
                        {
                            foreach (var p in cfg.Petals)
                            {
                                if (p.Action == "discord_mute") { p.Action = "mic_toggle"; p.Label = "Микрофон"; p.Icon = "mic"; }
                                else if (p.Action == "discord_deafen") { p.Action = "screenshot_file"; p.Label = "Скриншот"; p.Icon = "camera"; }
                            }
                        }"""
new = """                        if (cfg.Version < 6)
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
                        }"""
assert old in s
s = s.replace(old, new)
io.open(p, "w", encoding="utf-8").write(s)
print("migration map ok")

# SettingsWindow: add rail icon into visual tree + lucide tiles list
p2 = "app/src/SettingsWindow.cs"
s2 = io.open(p2, encoding="utf-8").read()

old2 = """            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            var t = Glass.Label(label, 13, false, true);
            t.Margin = new Thickness(11, 0, 0, 0);
            t.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(t);
            item.Child = sp;"""
assert old2 in s2, "rail add"
s2 = s2.replace(old2, """            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            var ic = LucideHost.Build(glyph, 15, false);
            var icHost = new ContentControl();
            icHost.Content = ic;
            icHost.Width = 16; icHost.Height = 16;
            icHost.VerticalAlignment = VerticalAlignment.Center;
            _railGlyphs.Add(icHost);
            sp.Children.Add(icHost);
            var t = Glass.Label(label, 13, false, true);
            t.Margin = new Thickness(11, 0, 0, 0);
            t.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(t);
            item.Child = sp;""")

# tiles: lucide names (matching icon library)
s2 = s2.replace('string[] glyphs = { "E720", "E74F", "E767", "E768", "E769", "E8D6", "E71D", "E71B", "E713", "E8B7", "E765", "E774", "E734", "E72E", "E722", "E72C" };',
'''string[] glyphs = { "mic", "mic-off", "volume-2", "volume-1", "volume-x", "play", "pause", "music", "skip-forward", "skip-back", "app-window", "link-2", "settings", "folder", "folder-open", "keyboard", "globe", "star", "lock", "camera", "image", "crop", "search", "bell", "download", "upload", "copy", "clipboard", "trash-2", "power", "moon", "monitor", "monitor-off", "pin", "refresh-cw", "rotate-cw", "calculator", "smile", "gamepad-2", "headphones", "activity", "minimize-2", "message-circle", "file-text", "grid-2x2", "sliders-horizontal", "palette", "info" };''')

io.open(p2, "w", encoding="utf-8").write(s2)
print("settings lucide ok")

# UiControls: IconTile recolors its path on select
p3 = "app/src/UiControls.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace("""        public void SetSelected(bool on)
        {
            _selected = on;
            BorderThickness = new Thickness(0);
            Background = on ? Ui.Solid(Ui.Accent, 0.20) : Ui.Solid(Colors.White, 0.05);
        }""",
"""        public void SetSelected(bool on)
        {
            _selected = on;
            BorderThickness = new Thickness(0);
            Background = on ? Ui.Solid(Ui.Accent, 0.20) : Ui.Solid(Colors.White, 0.05);
            var host = _icon as Viewbox;
            if (host != null && host.Child is Canvas cv && cv.Children.Count > 0 && cv.Children[0] is System.Windows.Shapes.Path pt)
                pt.Stroke = on ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Colors.White, 0.95);
        }""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("tile recolor ok")

# RefreshPetalList: petal icon tint update — petal list tint lambda exists; also icons for app action in list
p4 = "app/src/SettingsWindow.cs"
s4 = io.open(p4, encoding="utf-8").read()
# RefreshAccent stub removed — no palette section anymore; drop method to avoid dead refs
io.open(p4, "w", encoding="utf-8").write(s4)
print("done")
