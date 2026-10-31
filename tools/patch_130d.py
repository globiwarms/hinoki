"""v1.3.0 settings part 2: roundness/speed/entrance/autoclose + palette 16."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# ---- look: petal roundness after center opacity ----
anchor = '''            stack.Children.Add(Setting("Прозрачность центра", null, AttachSlider(cSlider, cVal)));
            stack.Children.Add(Sep());'''
add = anchor + '''
            var rVal = Glass.Label(cfgApp.PetalRoundness + "%", 13, true);
            var rSlider = new GlassSlider(50, 100, cfgApp.PetalRoundness);
            rSlider.Changed += delegate
            {
                App.Config.PetalRoundness = (int)Math.Round(rSlider.Value);
                rVal.Text = App.Config.PetalRoundness + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();
            };
            stack.Children.Add(Setting("Скругление лепестков", null, AttachSlider(rSlider, rVal)));
            stack.Children.Add(Sep());'''
assert anchor in s, "center anchor"
s = s.replace(anchor, add, 1)

# ---- look: entrance + speed after size ----
anchor2 = '''            stack.Children.Add(Setting("Размер меню", null, AttachSlider(sizeSlider, sizeVal)));'''
add2 = anchor2 + '''
            stack.Children.Add(Sep());

            var entranceItems = new List<string> { "Пружина (живой)", "Плавно", "Мгновенно" };
            var entranceMap = new[] { "spring", "smooth", "instant" };
            int curEntr = Array.IndexOf(entranceMap, cfgApp.EntranceStyle);
            var entranceDrop = new GlassDropDown(entranceItems, curEntr < 0 ? 0 : curEntr);
            entranceDrop.MinWidth = 190;
            entranceDrop.Changed += delegate
            {
                App.Config.EntranceStyle = entranceMap[entranceDrop.SelectedIndex];
                App.SaveConfig(); Pulse();
            };
            stack.Children.Add(Setting("Появление меню", "Как раскрывается цветок", entranceDrop));
            stack.Children.Add(Sep());

            var spVal = Glass.Label(cfgApp.AnimSpeed + "%", 13, true);
            var spSlider = new GlassSlider(50, 200, cfgApp.AnimSpeed);
            spSlider.Changed += delegate
            {
                App.Config.AnimSpeed = (int)Math.Round(spSlider.Value);
                spVal.Text = App.Config.AnimSpeed + "%";
                App.SaveConfig(); Pulse();
            };
            stack.Children.Add(Setting("Скорость анимаций", null, AttachSlider(spSlider, spVal)));'''
assert anchor2 in s, "size anchor"
s = s.replace(anchor2, add2, 1)

# ---- palette 16 ----
s = s.replace('string[] palette = { "A8B899", "7FD8BE", "A8B4E8", "E8B4A0", "E89E9E", "9ED0F0", "E8D5A0", "C9A0E8" };',
              'string[] palette = { "A8B899", "7FD8BE", "A8B4E8", "E8B4A0", "E89E9E", "9ED0F0", "E8D5A0", "C9A0E8", "F2A6C2", "C4E88C", "E8C89A", "9AE8CF", "B8A8E8", "8CD8B4", "D8D88C", "E89E6C" };')

# ---- behavior: autoclose ----
anchor3 = '''            var tgOutside = new GlassToggle(cfgApp.CloseOnOutsideClick);
            tgOutside.Changed += delegate { App.Config.CloseOnOutsideClick = tgOutside.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Закрывать по клику мимо", "Клик в любом другом месте сворачивает меню", tgOutside));
            stack.Children.Add(Sep());'''
add3 = anchor3 + '''
            var acVal = Glass.Label(cfgApp.AutoCloseSeconds > 0 ? cfgApp.AutoCloseSeconds + " с" : "выкл", 13, true);
            var acSlider = new GlassSlider(0, 30, cfgApp.AutoCloseSeconds);
            acSlider.Changed += delegate
            {
                App.Config.AutoCloseSeconds = (int)Math.Round(acSlider.Value);
                acVal.Text = App.Config.AutoCloseSeconds > 0 ? App.Config.AutoCloseSeconds + " с" : "выкл";
                App.SaveConfig(); Pulse();
            };
            stack.Children.Add(Setting("Автозакрытие меню", "Само свернётся через N секунд без действий (0 — никогда)", NewRow(AttachSlider(acSlider, acVal), null)));
            stack.Children.Add(Sep());'''
assert anchor3 in s, "outside anchor"
s = s.replace(anchor3, add3, 1)

io.open(p, "w", encoding="utf-8").write(s)
print("settings v1.3 sliders ok")
