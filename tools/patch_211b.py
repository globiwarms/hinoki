"""v2.1.1 settings redesign: themes, subtitles, new toggles, button hierarchy."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# ---------- theme-aware chrome gradient ----------
s = s.replace("""            _chrome = new Border();
            _chrome.CornerRadius = new CornerRadius(22);
            var chromeFill = new LinearGradientBrush();
            chromeFill.StartPoint = new Point(0, 0);
            chromeFill.EndPoint = new Point(0.7, 1);
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x18, 0x1A, 0x20), 0));
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x11, 0x12, 0x17), 0.55));
            chromeFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x0D, 0x0E, 0x12), 1));
            chromeFill.Freeze();
            _chrome.Background = chromeFill;
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.09);
            _chrome.BorderThickness = new Thickness(1);
            _chrome.ClipToBounds = true;
            _chrome.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 44, ShadowDepth = 10, Opacity = 0.5 };""",
"""            _chrome = new Border();
            _chrome.CornerRadius = new CornerRadius(22);
            _chrome.Background = MakeChromeFill();
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.09);
            _chrome.BorderThickness = new Thickness(1);
            _chrome.ClipToBounds = true;""")

# theme brush helper
s = s.replace("""        void Entrance()""",
"""        Brush MakeChromeFill()
        {
            var cfgApp = App.Config;
            bool deep = cfgApp != null && cfgApp.DeepSettingsTheme;
            var g = new LinearGradientBrush();
            g.StartPoint = new Point(0, 0);
            g.EndPoint = new Point(0.7, 1);
            if (deep)
            {
                g.GradientStops.Add(new GradientStop(Color.FromRgb(0x0C, 0x0D, 0x10), 0));
                g.GradientStops.Add(new GradientStop(Color.FromRgb(0x09, 0x0A, 0x0D), 0.55));
                g.GradientStops.Add(new GradientStop(Color.FromRgb(0x06, 0x07, 0x09), 1));
            }
            else
            {
                g.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x1C, 0x22), 0));
                g.GradientStops.Add(new GradientStop(Color.FromRgb(0x12, 0x13, 0x18), 0.55));
                g.GradientStops.Add(new GradientStop(Color.FromRgb(0x0E, 0x0F, 0x13), 1));
            }
            g.Freeze();
            return g;
        }

        void Entrance()""")

# ---------- header: title + subtitle ----------
s = s.replace("""            var head = new Grid();
            head.Margin = new Thickness(4, 0, 0, 14);
            _pageTitle = Glass.Label(PageTitle("petals"), 22, false, true);
            head.Children.Add(_pageTitle);""",
"""            var head = new Grid();
            head.Margin = new Thickness(4, 0, 0, 14);
            var headStack = new StackPanel();
            _pageTitle = Glass.Label(PageTitle("petals"), 22, false, true);
            headStack.Children.Add(_pageTitle);
            _pageSub = Glass.Label(PageSub("petals"), 12, true);
            _pageSub.Margin = new Thickness(1, 4, 0, 0);
            headStack.Children.Add(_pageSub);
            head.Children.Add(headStack);""")
s = s.replace("        TextBlock _pageTitle;", "        TextBlock _pageTitle;\n        TextBlock _pageSub;")
s = s.replace("            if (_pageTitle != null) _pageTitle.Text = PageTitle(key);",
              "            if (_pageTitle != null) _pageTitle.Text = PageTitle(key);\n            if (_pageSub != null) _pageSub.Text = PageSub(key);")
s = s.replace("""        string PageTitle(string key)""",
"""        string PageSub(string key)
        {
            switch (key)
            {
                case "look": return "Цвет, стекло и плавность";
                case "sounds": return "Мягкие стеклянные звуки";
                case "behavior": return "Как ведёт себя цветок";
                case "about": return "檜 сделано с любовью";
                default: return "Назначь действия каждому лепестку";
            }
        }

        string PageTitle(string key)""")

# ---------- accent button for add petal ----------
s = s.replace('var add = new PillButton("+ Лепесток", false);', 'var add = new PillButton("+ Лепесток", true);')

# ---------- look: theme + center toggles ----------
s = s.replace("""            var tgLight = new GlassToggle(cfgApp.LightGlass);""",
"""            var tgDeep = new GlassToggle(cfgApp.DeepSettingsTheme);
            tgDeep.Changed += delegate
            {
                App.Config.DeepSettingsTheme = tgDeep.IsOn;
                App.SaveConfig();
                _chrome.Background = MakeChromeFill();
                Pulse();
            };
            stack.Children.Add(Setting("Глубокая тема настроек", "Ещё более тёмный фон этого окна", tgDeep));
            stack.Children.Add(Sep());

            var tgLight = new GlassToggle(cfgApp.LightGlass);""")

s = s.replace("""            var tgLabels = new GlassToggle(cfgApp.ShowLabels);
            tgLabels.Changed += delegate { App.Config.ShowLabels = tgLabels.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            stack.Children.Add(Setting("Показывать названия", "Подписи на лепестках и под кнопкой настроек", tgLabels));""",
"""            var tgLabels = new GlassToggle(cfgApp.ShowLabels);
            tgLabels.Changed += delegate { App.Config.ShowLabels = tgLabels.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            stack.Children.Add(Setting("Показывать названия", "Подписи на лепестках", tgLabels));
            stack.Children.Add(Sep());

            var tgCenter = new GlassToggle(cfgApp.ShowCenterButton);
            tgCenter.Changed += delegate { App.Config.ShowCenterButton = tgCenter.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            stack.Children.Add(Setting("Шестерёнка в центре", "Кнопка настроек в середине цветка", tgCenter));""")

# ---------- behavior: notifications + shot folder ----------
s = s.replace("""            var tgAnim = new GlassToggle(cfgApp.AnimationsEnabled);
            tgAnim.Changed += delegate { App.Config.AnimationsEnabled = tgAnim.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Анимации", "Пружинное раскрытие и плавные переходы", tgAnim));""",
"""            var tgAnim = new GlassToggle(cfgApp.AnimationsEnabled);
            tgAnim.Changed += delegate { App.Config.AnimationsEnabled = tgAnim.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Анимации", "Пружинное раскрытие и плавные переходы", tgAnim));
            stack.Children.Add(Sep());

            var tgNotif = new GlassToggle(cfgApp.ShowNotifications);
            tgNotif.Changed += delegate { App.Config.ShowNotifications = tgNotif.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Уведомления о действиях", "Всплывающие подсказки у часов (скриншоты, ошибки)", tgNotif));
            stack.Children.Add(Sep());

            var tgShot = new GlassToggle(cfgApp.OpenShotFolder);
            tgShot.Changed += delegate { App.Config.OpenShotFolder = tgShot.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Папка после скриншота", "Открывать «Изображения» сразу после снимка", tgShot));""")

# ---------- version ----------
s = s.replace('Glass.Label("v2.1.0", 10, true)', 'Glass.Label("v2.1.1", 10, true)')
s = s.replace('Glass.Label("Версия 2.1.0 · Windows 10/11", 12, true)', 'Glass.Label("Версия 2.1.1 · Windows 10/11", 12, true)')

io.open(p, "w", encoding="utf-8").write(s)
print("settings redesign ok")
