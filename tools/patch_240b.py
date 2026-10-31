"""v2.4.0 settings: confirm/right-click toggles, version, toast visual test hook."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

s = s.replace("""            var tgAnim = new GlassToggle(cfgApp.AnimationsEnabled);
            tgAnim.Changed += delegate { App.Config.AnimationsEnabled = tgAnim.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Анимации", "Пружинное раскрытие и плавные переходы", tgAnim));""",
"""            var tgAnim = new GlassToggle(cfgApp.AnimationsEnabled);
            tgAnim.Changed += delegate { App.Config.AnimationsEnabled = tgAnim.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Анимации", "Пружинное раскрытие и плавные переходы", tgAnim));
            stack.Children.Add(Sep());

            var tgConfirm = new GlassToggle(cfgApp.ConfirmPower);
            tgConfirm.Changed += delegate { App.Config.ConfirmPower = tgConfirm.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Подтверждать выключение", "Стеклянный диалог перед сном, перезагрузкой и выключением", tgConfirm));
            stack.Children.Add(Sep());

            var tgRight = new GlassToggle(cfgApp.CloseOnRightClick);
            tgRight.Changed += delegate { App.Config.CloseOnRightClick = tgRight.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Закрывать правой кнопкой", "ПКМ по стеклу тоже сворачивает меню", tgRight));""")

s = s.replace('Glass.Label("v2.2.0", 10, true)', 'Glass.Label("v2.4.0", 10, true)')
s = s.replace('Glass.Label("Версия 2.2.0 · Windows 10/11", 12, true)', 'Glass.Label("Версия 2.4.0 · Windows 10/11", 12, true)')

io.open(p, "w", encoding="utf-8").write(s)
print("settings v2.4.0 ok")
