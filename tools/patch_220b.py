"""v2.2.0 settings patch: rail flower icon, entrance dropdown 7, dropdown label color, version."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# rail: petals -> flower-2, look -> paintbrush
s = s.replace('AddRailItem(nav, "settings", "Лепестки", "petals");',
              'AddRailItem(nav, "flower-2", "Лепестки", "petals");')
s = s.replace('AddRailItem(nav, "palette", "Внешний вид", "look");',
              'AddRailItem(nav, "paintbrush", "Внешний вид", "look");')
s = s.replace('''                case "look": return "palette";''',
              '''                case "look": return "paintbrush";''')
s = s.replace('''                default: return "settings";
            }
        }

        void AddRailItem''',
'''                case "petals": return "flower-2";
                default: return "settings";
            }
        }

        void AddRailItem''')

# entrance dropdown: 7 styles
s = s.replace('''            var entranceItems = new List<string> { "Пружина (живой)", "Плавно", "Эластик", "Отскок", "Мгновенно" };
            var entranceMap = new[] { "spring", "smooth", "elastic", "bounce", "instant" };''',
'''            var entranceItems = new List<string> { "Пружина (живой)", "Плавно", "Эластик", "Отскок", "Волна", "Только fade", "Мгновенно" };
            var entranceMap = new[] { "spring", "smooth", "elastic", "bounce", "ripple", "fade", "instant" };''')

# version
s = s.replace('Glass.Label("v2.1.1", 10, true)', 'Glass.Label("v2.2.0", 10, true)')
s = s.replace('Glass.Label("Версия 2.1.1 · Windows 10/11", 12, true)', 'Glass.Label("Версия 2.2.0 · Windows 10/11", 12, true)')

io.open(p, "w", encoding="utf-8").write(s)
print("settings ok")

# ============ UiControls: dropdown label foreground (black text bug) ============
p2 = "app/src/UiControls.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace("""            _label = new TextBlock();
            _label.FontFamily = Ui.Text;
            _label.FontSize = 13.5;
            _label.VerticalAlignment = VerticalAlignment.Center;
            _label.Margin = new Thickness(11, 9, 30, 10);
            _label.TextTrimming = TextTrimming.CharacterEllipsis;
            grid.Children.Add(_label);""",
"""            _label = new TextBlock();
            _label.FontFamily = Ui.Text;
            _label.FontSize = 13.5;
            _label.Foreground = Ui.Solid(Ui.Ink, 0.95);
            _label.VerticalAlignment = VerticalAlignment.Center;
            _label.Margin = new Thickness(11, 9, 30, 10);
            _label.TextTrimming = TextTrimming.CharacterEllipsis;
            grid.Children.Add(_label);""")
io.open(p2, "w", encoding="utf-8").write(s2)
print("dropdown label ok")
