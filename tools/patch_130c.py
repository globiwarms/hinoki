"""v1.3.0 settings part: icon search picker, new sliders, palette 16."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# ---------- fields ----------
s = s.replace("        GlassDropDown _actionDrop;",
              "        GlassDropDown _actionDrop;\n        GlassTextBox _iconSearch;\n        WrapPanel _iconPanel;")

# ---------- icon library (glyph + russian/english search tags) ----------
s = s.replace("        // ================= petals =================",
'''        // ================= icon library =================

        static readonly string[][] IconLibrary = new string[][]
        {
            new[] { "E713", "настройки gear settings шестерёнка" },
            new[] { "E720", "микрофон микрофон mic звукозапись" },
            new[] { "E74F", "мут выкл off mute тише" },
            new[] { "E767", "звук громкость volume speaker динамик" },
            new[] { "E768", "play играть музыка старт пуск" },
            new[] { "E769", "pause пауза стоп stop" },
            new[] { "E76E", "эмодзи смайл emoji smile" },
            new[] { "E70F", "редактировать карандаш edit pencil правка" },
            new[] { "E721", "поиск search лупа найти" },
            new[] { "E722", "камера фото camera photo снимок" },
            new[] { "E72C", "обновить refresh перезагрузка заново" },
            new[] { "E72E", "замок блокировка lock закрыть" },
            new[] { "E734", "звезда favorite star избранное лайк" },
            new[] { "E73E", "галочка check done готово" },
            new[] { "E74D", "корзина delete удалить мусор bin" },
            new[] { "E74E", "сохранить save дискета" },
            new[] { "E765", "клавиатура keyboard" },
            new[] { "E774", "глобус интернет globe web сеть" },
            new[] { "E77B", "контакт человек contact user" },
            new[] { "E787", "календарь calendar дата" },
            new[] { "E790", "палитра цвет paint palette краска" },
            new[] { "E7A5", "скриншот crop обрезка снимок" },
            new[] { "E7BE", "флаг flag метка" },
            new[] { "E7C3", "страница page документ бумага" },
            new[] { "E7E7", "звонок уведомление ringer bell колокольчик" },
            new[] { "E7E8", "питание power выключить on off" },
            new[] { "E7F4", "монитор экран tv display дисплей" },
            new[] { "E896", "скачать download стрелка вниз загрузка" },
            new[] { "E898", "загрузить upload вверх экспорт" },
            new[] { "E892", "следующий next трек media вперёд" },
            new[] { "E8A5", "документ document файл file текст" },
            new[] { "E8A9", "открыть файл openfile" },
            new[] { "E8B7", "папка folder директория" },
            new[] { "E8BD", "сообщение message чат comment" },
            new[] { "E8C8", "копировать copy" },
            new[] { "E8D6", "музыка нота music note трек" },
            new[] { "E8EF", "калькулятор calculator посчитать" },
            new[] { "E8FB", "обновить refresh цикл" },
            new[] { "E946", "информация info сведения" },
            new[] { "E9CE", "наушники headphones гарнитура звук" },
            new[] { "E9D9", "диагностика tools debug wrench" },
            new[] { "EA3A", "рабочий стол desktop монитор экран" },
            new[] { "EA80", "игры game геймпад контроллер" },
            new[] { "E71D", "приложения apps грид все программы" },
            new[] { "E71B", "ссылка link цепь url адрес" },
            new[] { "E80F", "список list строки" }
        };

        // ================= petals =================''')

# ---------- icon picker with search ----------
old_icons = """            _editor.Children.Add(Section("Иконка"));
            var iconPanel = new WrapPanel();
            iconPanel.Margin = new Thickness(0, 2, 0, 0);
            string[] glyphs = { "E720", "E74F", "E767", "E768", "E769", "E8D6", "E71D", "E71B", "E713", "E8B7", "E765", "E774", "E734", "E72E", "E722", "E72C" };
            IconTile.ActiveGroup = new List<IconTile>();
            foreach (var gl in glyphs)
            {
                var tile = new IconTile(gl);
                tile.Picked += delegate (string g)
                {
                    App.Config.Petals[_selPetal].Icon = g;
                    App.SaveConfig();
                    _preview.UpdatePetal(_selPetal);
                    RefreshPetalList();
                    Pulse();
                };
                IconTile.ActiveGroup.Add(tile);
                iconPanel.Children.Add(tile);
                if (gl.Equals(p.Icon, StringComparison.OrdinalIgnoreCase)) tile.SetSelected(true);
            }
            _editor.Children.Add(iconPanel);"""
new_icons = """            _editor.Children.Add(Section("Иконка"));
            _iconSearch = new GlassTextBox("", "Поиск иконки… (например: микрофон)", 240);
            _iconSearch.Margin = new Thickness(0, 2, 0, 8);
            _editor.Children.Add(_iconSearch);

            _iconPanel = new WrapPanel();
            _iconPanel.Margin = new Thickness(0, 2, 0, 0);
            var iconScroll = new ScrollViewer();
            iconScroll.MaxHeight = 216;
            iconScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            iconScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            iconScroll.Content = _iconPanel;
            _editor.Children.Add(iconScroll);

            IconTile.ActiveGroup = new List<IconTile>();
            Action<string> rebuildIcons = delegate(string query)
            {
                _iconPanel.Children.Clear();
                string q = (query ?? "").Trim().ToLowerInvariant();
                IconTile.ActiveGroup = new List<IconTile>();
                int shown = 0;
                foreach (var def in IconLibrary)
                {
                    string tags = def[1];
                    if (q.Length > 0 && !tags.Contains(q) && !def[0].ToLowerInvariant().Contains(q)) continue;
                    var gl = def[0];
                    var tile = new IconTile(gl);
                    tile.Picked += delegate (string g)
                    {
                        App.Config.Petals[_selPetal].Icon = g;
                        App.SaveConfig();
                        _preview.UpdatePetal(_selPetal);
                        RefreshPetalList();
                        Pulse();
                    };
                    IconTile.ActiveGroup.Add(tile);
                    _iconPanel.Children.Add(tile);
                    if (gl.Equals(p.Icon, StringComparison.OrdinalIgnoreCase)) tile.SetSelected(true);
                    shown++;
                }
                if (shown == 0)
                    _iconPanel.Children.Add(Glass.Label("Ничего не найдено — попробуйте по-другому", 12, true));
            };
            rebuildIcons("");
            _iconSearch.Box.TextChanged += delegate { rebuildIcons(_iconSearch.Value); };"""
assert old_icons in s, "icon block"
s = s.replace(old_icons, new_icons)

# icon tile visual: no border, compact
io.open(p, "w", encoding="utf-8").write(s)
print("icon search ok")
