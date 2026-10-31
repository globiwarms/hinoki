"""v2.2.0: mic/system toggle actions, close_window, rail flower icon, dropdown label fix, new entrances."""
import io

# ============ Config: actions + names ============
p = "app/src/Config.cs"
s = io.open(p, encoding="utf-8").read()
s = s.replace('            "search_start", "notification_center"\n        };',
'''            "search_start", "notification_center",
            "mute_all", "close_window"
        };''')
s = s.replace('''            "Поиск Windows",
            "Центр уведомлений"
        };''',
'''            "Поиск Windows",
            "Центр уведомлений",
            "Тихий режим (микро + звук)",
            "Закрыть активное окно"
        };''')
s = s.replace('                case "notification_center": return "bell";',
'''                case "notification_center": return "bell";
                case "mute_all": return "bell-off";
                case "close_window": return "x";''')
# nicer action text
s = s.replace('"Запустить приложение или steam://",', '"Открыть приложение",')
io.open(p, "w", encoding="utf-8").write(s)
print("config actions ok")

# ============ ActionEngine: impls ============
p2 = "app/src/ActionEngine.cs"
s2 = io.open(p2, encoding="utf-8").read()
s2 = s2.replace('''                case "notification_center": PressCombo("Win+N"); break;''',
'''                case "notification_center": PressCombo("Win+N"); break;
                case "mute_all":
                {
                    bool micMuted = Audio.IsMicMuted();
                    Audio.SetMicMuted(!micMuted);
                    TapKey(0xAD); // toggle system volume
                    if (!micMuted) Report("Hinoki", "Тихий режим: микрофон и звук выключены");
                    break;
                }
                case "close_window":
                {
                    IntPtr fg = Native.GetForegroundWindow();
                    if (fg != IntPtr.Zero) Native.SendMessageW(fg, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                    break;
                }''')
io.open(p2, "w", encoding="utf-8").write(s2)
print("engine ok")

# ============ FlowerMenu: fade + ripple entrances ============
p3 = "app/src/FlowerMenu.cs"
s3 = io.open(p3, encoding="utf-8").read()
s3 = s3.replace('''                IEasingFunction e;
                if (style == "smooth") e = new CubicEase { EasingMode = EasingMode.EaseOut };
                else if (style == "elastic") e = new ElasticEase { Oscillations = 2, Springiness = 3.2, EasingMode = EasingMode.EaseOut };
                else if (style == "bounce") e = new BounceEase { Bounces = 2, Bounciness = 2.4, EasingMode = EasingMode.EaseOut };
                else e = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };
                var gx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(460 / sp))) { EasingFunction = e };
                var gy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(460 / sp))) { EasingFunction = e };
                Scale.BeginAnimation(ScaleTransform.ScaleXProperty, gx);
                Scale.BeginAnimation(ScaleTransform.ScaleYProperty, gy);
                var go = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(220 / sp)));
                BeginAnimation(UIElement.OpacityProperty, go);''',
'''                bool pureFade = style == "fade";
                IEasingFunction e;
                if (style == "smooth" || pureFade) e = new CubicEase { EasingMode = EasingMode.EaseOut };
                else if (style == "elastic") e = new ElasticEase { Oscillations = 2, Springiness = 3.2, EasingMode = EasingMode.EaseOut };
                else if (style == "bounce") e = new BounceEase { Bounces = 2, Bounciness = 2.4, EasingMode = EasingMode.EaseOut };
                else if (style == "ripple") e = new BackEase { Amplitude = 0.8, EasingMode = EasingMode.EaseOut };
                else e = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut };
                var gx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds((pureFade ? 10 : 460) / sp))) { EasingFunction = e };
                var gy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds((pureFade ? 10 : 460) / sp))) { EasingFunction = e };
                Scale.BeginAnimation(ScaleTransform.ScaleXProperty, gx);
                Scale.BeginAnimation(ScaleTransform.ScaleYProperty, gy);
                var go = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds((style == "ripple" ? 340 : 220) / sp)));
                BeginAnimation(UIElement.OpacityProperty, go);''')
# ripple: longer stagger handled by caller? keep; set stagger via style in PlayIn of menu
s3 = s3.replace('''            for (int i = 0; i < _petals.Count; i++)
                _petals[i].PlayIn(TimeSpan.FromMilliseconds((40 + i * 40) / sp));''',
'''            int step = style == "ripple" ? 90 : 40;
            for (int i = 0; i < _petals.Count; i++)
                _petals[i].PlayIn(TimeSpan.FromMilliseconds((55 + i * step) / sp));''')
io.open(p3, "w", encoding="utf-8").write(s3)
print("entrances ok")
