"""v1.2.4 settings patch: version bump, center opacity slider, restart button."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

s = s.replace('bn.Children.Add(Glass.Label("v1.2.0", 10, true));',
              'bn.Children.Add(Glass.Label("v1.2.4", 10, true));')
s = s.replace('var ver = Glass.Label("Версия 1.2.0 · Windows 10/11", 12, true);',
              'var ver = Glass.Label("Версия 1.2.4 · Windows 10/11", 12, true);')

anchor = '            stack.Children.Add(Setting("Сила свечения", null, AttachSlider(glowSlider, glowVal)));\n            stack.Children.Add(Sep());\n'
assert anchor in s, "glow anchor"
add = anchor + '''
            var cVal = Glass.Label(cfgApp.CenterOpacity + "%", 13, true);
            var cSlider = new GlassSlider(30, 100, cfgApp.CenterOpacity);
            cSlider.Changed += delegate
            {
                App.Config.CenterOpacity = (int)Math.Round(cSlider.Value);
                cVal.Text = App.Config.CenterOpacity + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();
            };
            stack.Children.Add(Setting("Прозрачность центра", null, AttachSlider(cSlider, cVal)));
            stack.Children.Add(Sep());
'''
s = s.replace(anchor, add, 1)

old_test = '''            var testBtn = new PillButton("Открыть меню (тест)", false);
            testBtn.HorizontalAlignment = HorizontalAlignment.Left;
            testBtn.Margin = new Thickness(2, 14, 0, 0);
            testBtn.Clicked += delegate { App.TestMenu(); };
            stack.Children.Add(testBtn);'''
assert old_test in s, "test anchor"
new_test = '''            var rowBtns = new StackPanel();
            rowBtns.Orientation = Orientation.Horizontal;
            rowBtns.Margin = new Thickness(0, 4, 0, 0);
            var testBtn = new PillButton("Открыть меню (тест)", false);
            testBtn.Clicked += delegate { App.TestMenu(); };
            rowBtns.Children.Add(testBtn);
            var restart = new PillButton("Перезапустить Hinoki", false);
            restart.Margin = new Thickness(8, 0, 0, 0);
            restart.Clicked += delegate { App.RestartApp(); };
            rowBtns.Children.Add(restart);
            stack.Children.Add(rowBtns);'''

s = s.replace(old_test, new_test)

io.open(p, "w", encoding="utf-8").write(s)
print("settings v1.2.4 patched")
