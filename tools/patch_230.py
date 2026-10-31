"""v2.3.0 polish: warnings, grouped icon library, muted states in preview, fade entrance, button hover."""
import io

# ============ SettingsWindow ============
p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# 1) remove unused field (compiler warning)
s = s.replace("        bool _suppressPulse;\n", "")

# 2) preview reflects muted states after every rebuild
s = s.replace("""        public void Rebuild()
        {
            Build();
        }""",
"""        public void Rebuild()
        {
            Build();
            RefreshToggleStates();
        }""")

# 3) icon library: grouped by category (media -> system -> files -> web -> misc)
start = s.index("        static readonly string[][] IconLibrary = new string[][]")
end = s.index("        };", start) + len("        };")
lib = """        static readonly string[][] IconLibrary = new string[][]
        {
            // media & sound
            new[] { "mic", "микрофон микрофон mic звукозапись" },
            new[] { "mic-off", "микрофон выкл mic-off мут" },
            new[] { "volume-2", "звук громкость volume динамик" },
            new[] { "volume-1", "звук тише volume маленький" },
            new[] { "volume-x", "звук выкл мут volume-x" },
            new[] { "headphones", "наушники headphones гарнитура" },
            new[] { "music", "музыка нота music трек" },
            new[] { "play", "play играть пуск музыка старт" },
            new[] { "pause", "пауза pause стоп" },
            new[] { "skip-forward", "следующий трек next вперёд" },
            new[] { "skip-back", "предыдущий трек prev назад" },
            // system & power
            new[] { "power", "питание power выключить" },
            new[] { "moon", "луна сон ночь спящий moon" },
            new[] { "lock", "замок lock блокировка закрыть" },
            new[] { "monitor", "монитор экран desktop дисплей" },
            new[] { "monitor-off", "монитор выкл погасить экран" },
            new[] { "minimize-2", "свернуть minimize окно" },
            new[] { "activity", "диспетчер activity пульс задачи" },
            new[] { "settings", "настройки gear шестерёнка settings" },
            new[] { "sliders-horizontal", "ползунки sliders поведение настройки" },
            new[] { "rotate-cw", "перезагрузка rotate поворот" },
            new[] { "refresh-cw", "обновить refresh заново" },
            // files & folders
            new[] { "folder", "папка folder директория" },
            new[] { "folder-open", "папка открыта открыть folder" },
            new[] { "file-text", "документ файл текст file текстовый" },
            new[] { "download", "скачать download загрузка вниз" },
            new[] { "upload", "загрузить upload вверх экспорт" },
            new[] { "copy", "копировать copy" },
            new[] { "clipboard", "буфер clipboard вставить" },
            new[] { "trash-2", "корзина удалить мусор trash bin" },
            // web & communication
            new[] { "globe", "глобус интернет globe web сеть" },
            new[] { "link-2", "ссылка link адрес url цепь" },
            new[] { "message-circle", "сообщение чат message discord" },
            new[] { "search", "поиск найти search лупа" },
            new[] { "bell", "уведомление колокольчик bell звонок" },
            new[] { "gamepad-2", "геймпад игра game steam" },
            // appearance & misc
            new[] { "camera", "камера фото camera снимок скриншот" },
            new[] { "image", "изображение картинка image фото" },
            new[] { "crop", "скриншот обрезка crop выделить" },
            new[] { "star", "звезда star избранное лайк" },
            new[] { "smile", "смайл эмодзи smile улыбка" },
            new[] { "paintbrush", "палитра paintbrush краски цвет" },
            new[] { "grid-2x2", "приложения сетка grid все" },
            new[] { "calculator", "калькулятор calculator посчитать" },
            new[] { "pin", "закрепить pin поверх булавка" },
            new[] { "keyboard", "клавиатура keyboard" },
            new[] { "info", "информация info сведения" }
        };"""
s = s[:start] + lib + s[end:]

# 4) editor petal title bigger
s = s.replace('var title = Glass.Label("Лепесток " + (_selPetal + 1), 17, false, true);',
              'var title = Glass.Label("Лепесток " + (_selPetal + 1), 18, false, true);')

io.open(p, "w", encoding="utf-8").write(s)
print("settings polish ok")

# ============ UiControls: pill hover scale + subtle press ============
p2 = "app/src/UiControls.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("""            MouseEnter += delegate { Opacity = 0.9; };
            MouseLeave += delegate { Opacity = 1; };
            MouseLeftButtonDown += delegate { Scale(0.95); };""",
"""            MouseEnter += delegate { Opacity = 0.92; Scale(1.03); };
            MouseLeave += delegate { Opacity = 1; Scale(1); };
            MouseLeftButtonDown += delegate { Scale(0.95); };""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("pill hover ok")

# ============ FlowerMenu: fade entrance without scale snap ============
p3 = "app/src/FlowerMenu.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace("""                bool pureFade = style == "fade";
                IEasingFunction e;
                if (style == "smooth" || pureFade) e = new CubicEase { EasingMode = EasingMode.EaseOut };
                else if (style == "elastic") e = new ElasticEase { Oscillations = 2, Springiness = 3.2, EasingMode = EasingMode.EaseOut };
                else if (style == "bounce") e = new BounceEase { Bounces = 2, Bounciness = 2.4, EasingMode = EasingMode.EaseOut };
                else if (style == "ripple") e = new BackEase { Amplitude = 0.8, EasingMode = EasingMode.EaseOut };
                else e = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };
                var gx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds((pureFade ? 10 : 460) / sp))) { EasingFunction = e };
                var gy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds((pureFade ? 10 : 460) / sp))) { EasingFunction = e };""",
"""                bool pureFade = style == "fade";
                IEasingFunction e;
                if (style == "smooth" || pureFade) e = new CubicEase { EasingMode = EasingMode.EaseOut };
                else if (style == "elastic") e = new ElasticEase { Oscillations = 2, Springiness = 3.2, EasingMode = EasingMode.EaseOut };
                else if (style == "bounce") e = new BounceEase { Bounces = 2, Bounciness = 2.4, EasingMode = EasingMode.EaseOut };
                else if (style == "ripple") e = new BackEase { Amplitude = 0.8, EasingMode = EasingMode.EaseOut };
                else e = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };
                if (pureFade) Scale.ScaleX = 1; Scale.ScaleY = 1;
                var gx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds((pureFade ? 10 : 460) / sp))) { EasingFunction = e };
                var gy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds((pureFade ? 10 : 460) / sp))) { EasingFunction = e };""")
io.open(p3, "w", encoding="utf-8").write(s3)
print("fade ok")
