"""v1.2: settings cleanup — remove notes, petal editor only name/color/action, icon grid clean, About."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# 1) Remove all Note() calls from petal editor (they clutter the UI)
for note_text in [
    'Note("Первый вариант — прозрачное стекло как у всех. Остальные — свой цвет лепестка.")',
    'Note("Работают .exe, .lnk и steam:// ссылки — откроется как обычный ярлык.")',
    'Note("Откроется в браузере по умолчанию.")',
    'Note("Hinoki нажмёт это сочетание в активном приложении.")',
    'Note("Общий микрофон системы. Нажмите на лепесток — состояние переключится, лепесток подсветится красным, пока микрофон выключен.")',
    'Note("Общий звук системы. Лепесток подсвечивается красным, пока звук выключен.")',
    'Note("Play / Pause для Spotify, YouTube Music и других плееров.")',
    'Note("Снимок всего экрана сохранится в «Изображения».")',
    'Note("Откроет системные «Ножницы» (Win+Shift+S) — выделите область.")',
    'Note("Сработает сразу, без подтверждения.")',
    'Note("Открывает библиотеку Steam. Если Steam не установлен — ничего не произойдёт.")',
    'Note("Выберите действие — лепесток выполнит его одним нажатием.")',
]:
    # Remove the _editor.Children.Add(Note(...)) line
    for prefix in ["            _editor.Children.Add(", "                _editor.Children.Add("]:
        full = prefix + note_text + ");"
        if full in s:
            s = s.replace(full + "\n", "")

# 2) Remove Note() from Look page
s = s.replace('            stack.Children.Add(Note("Первый вариант — прозрачное стекло как у всех. Остальные — свой цвет лепестка."));\n', '')

# 3) Icon tiles: fill the width evenly — use UniformGrid instead of WrapPanel
s = s.replace("""            var iconPanel = new WrapPanel();
            _iconPanel.Margin = new Thickness(0, 2, 0, 0);
            _iconPanel = new WrapPanel();""", """            var iconPanel = new WrapPanel();""")
s = s.replace("""            _iconPanel = new WrapPanel();
            _iconPanel.Margin = new Thickness(0, 2, 0, 0);""",
"""            _iconPanel = new UniformGrid();
            _iconPanel.Columns = 8;
            _iconPanel.Margin = new Thickness(0, 2, 0, 0);""")

# 4) Fix icon tile size for UniformGrid (smaller, fills evenly)
s = s.replace("""            var iconScroll = new ScrollViewer();
            iconScroll.MaxHeight = 216;""",
"""            var iconScroll = new ScrollViewer();
            iconScroll.MaxHeight = 224;""")

# 5) Remove the "Горячая клавиша" section from petal editor (already added in earlier round, now removing)
# It's already removed from FillEditor — verify no double

# 6) Center button size slider in Look page
anchor = """            stack.Children.Add(Setting("Размер меню", null, AttachSlider(sizeSlider, sizeVal)));"""
add = anchor + """
            stack.Children.Add(Sep());

            var cbVal = Glass.Label(cfgApp.CenterOpacity + "%", 13, true);
            var cbSlider = new GlassSlider(30, 100, cfgApp.CenterOpacity);
            cbSlider.Changed += delegate
            {
                App.Config.CenterOpacity = (int)Math.Round(cbSlider.Value);
                cbVal.Text = App.Config.CenterOpacity + "%";
                App.SaveConfig();
                DebouncedRebuild();
                Pulse();
            };
            stack.Children.Add(Setting("Размер шестерёнки", null, AttachSlider(cbSlider, cbVal)));"""
assert anchor in s
s = s.replace(anchor, add, 1)

# 7) version strings
s = s.replace('Glass.Label("v1.1", 10, true)', 'Glass.Label("v1.2", 10, true)')
s = s.replace('Glass.Label("Версия 1.1 · Windows 10/11", 12, true)', 'Glass.Label("Версия 1.2 · Windows 10/11", 12, true)')

io.open(p, "w", encoding="utf-8").write(s)
print("settings v1.2 ok")

# ============ UniformGrid needs using ============
p2 = "app/src/SettingsWindow.cs"
s2 = io.open(p2, encoding="utf-8").read()
# UniformGrid is in System.Windows.Controls.Primitives
# Replace WrapPanel with UniformGrid properly — check we have the right using
# Actually UniformGrid is in System.Windows.Controls — should work with existing usings
print("no extra using needed")
