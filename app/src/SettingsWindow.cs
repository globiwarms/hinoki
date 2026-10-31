// Hinoki — settings v3: one glass surface, no nested frames, Apple-minimal rail + content
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace Hinoki
{
    public class SettingsWindow : Window
    {
        static SettingsWindow _instance;

        public static void ShowOrFocus()
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }
            _instance = new SettingsWindow();
            _instance.Show();
        }

        public static SettingsWindow ActiveWindow { get { return _instance; } }

        public static bool ContainsPhysicalPoint(int px, int py)
        {
            var w = _instance;
            if (w == null || !w.IsLoaded) return false;
            try
            {
                var src = PresentationSource.FromVisual(w);
                Point pt = new Point(px, py);
                if (src != null && src.CompositionTarget != null)
                    pt = src.CompositionTarget.TransformFromDevice.Transform(pt);
                return new Rect(w.Left, w.Top, w.ActualWidth, w.ActualHeight).Contains(pt);
            }
            catch { return false; }
        }

        int _selPetal = 0;
        StackPanel _petalList;
        StackPanel _editor;
        Border _chrome;
        TextBlock _status;
        FlowerMenu _preview;
        List<Border> _rail = new List<Border>();
        List<ContentControl> _railGlyphs = new List<ContentControl>();
        Dictionary<string, FrameworkElement> _pages = new Dictionary<string, FrameworkElement>();
        ContentControl _pageHost;
        GlassTextBox _iconSearch;
        WrapPanel _iconPanel;
        TextBlock _pageTitle;
        string _page = "petals";
        bool _suppressPulse;

        public SettingsWindow()
        {
            Title = "Hinoki";
            Width = 940; Height = 610;
            MinWidth = 860; MinHeight = 540;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanResize;
            Topmost = true; // settings must be reachable even over fullscreen games
            Left = Math.Max(0, (SystemParameters.WorkArea.Width - Width) / 2 + SystemParameters.WorkArea.Left);
            Top = Math.Max(0, (SystemParameters.WorkArea.Height - Height) / 2 + SystemParameters.WorkArea.Top);
            PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { Close(); e.Handled = true; }
            };

            var root = new Grid();
            root.Margin = new Thickness(14);

            _chrome = new Border();
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
            _chrome.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 44, ShadowDepth = 10, Opacity = 0.5 };

            // one subtle accent breath behind everything — the only decoration
            var blob = new Ellipse();
            blob.Width = 420; blob.Height = 420;
            blob.Fill = Ui.Solid(Ui.Accent, 0.10);
            blob.Effect = new BlurEffect { Radius = 90 };
            blob.IsHitTestVisible = false;
            blob.HorizontalAlignment = HorizontalAlignment.Left;
            blob.VerticalAlignment = VerticalAlignment.Top;
            blob.Margin = new Thickness(-140, -120, 0, 0);

            var layout = new Grid();
            layout.Children.Add(blob);
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(206) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            layout.Children.Add(BuildRail());

            var contentCol = BuildContentColumn();
            Grid.SetColumn(contentCol, 1);
            layout.Children.Add(contentCol);

            _chrome.Child = layout;
            root.Children.Add(_chrome);
            Content = root;

            Loaded += delegate
            {
                // single frame: my own border only — no DWM rounding, no acrylic sheet
                Entrance();
            };
            Closed += delegate
            {
                _instance = null;
                MouseHook.Suppressed = false;
            };
            MouseLeftButtonDown += delegate
            {
                if (Mouse.GetPosition(_chrome).Y < 64) DragMove();
            };
        }

        void Entrance()
        {
            if (!App.Config.AnimationsEnabled) { Opacity = 1; return; }
            Opacity = 0;
            var a = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(260)));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            BeginAnimation(OpacityProperty, a);
            var st = new ScaleTransform(0.97, 0.97);
            _chrome.RenderTransformOrigin = new Point(0.5, 0.5);
            _chrome.RenderTransform = st;
            var sxy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(320)));
            sxy.EasingFunction = new BackEase { Amplitude = 0.28, EasingMode = EasingMode.EaseOut };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, sxy);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, sxy);
        }

        // ================= rail =================

        Grid BuildRail()
        {
            var rail = new Grid();
            rail.Margin = new Thickness(20, 20, 8, 18);
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rail.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var brand = new StackPanel();
            brand.Orientation = Orientation.Horizontal;
            brand.Margin = new Thickness(6, 2, 0, 0);
            var logo = new Border();
            logo.Width = 32; logo.Height = 32;
            logo.CornerRadius = new CornerRadius(9.5);
            var logoImg = new Image { Source = FlowerMenu.LogoBitmap, Width = 32, Height = 32 };
            RenderOptions.SetBitmapScalingMode(logoImg, BitmapScalingMode.Fant);
            logo.Child = logoImg;
            logo.Effect = new DropShadowEffect { Color = Ui.Accent, BlurRadius = 14, ShadowDepth = 0, Opacity = 0.45 };
            brand.Children.Add(logo);
            var bn = new StackPanel();
            bn.VerticalAlignment = VerticalAlignment.Center;
            bn.Margin = new Thickness(10, 0, 0, 0);
            bn.Children.Add(Glass.Label("Hinoki", 15.5, false, true));
            bn.Children.Add(Glass.Label("v1.3.1", 10, true));
            brand.Children.Add(bn);
            Grid.SetRow(brand, 0);
            rail.Children.Add(brand);

            var nav = new StackPanel();
            nav.Margin = new Thickness(0, 26, 0, 0);
            AddRailItem(nav, "settings", "Лепестки", "petals");
            AddRailItem(nav, "palette", "Внешний вид", "look");
            AddRailItem(nav, "volume-2", "Звуки", "sounds");
            AddRailItem(nav, "sliders-horizontal", "Поведение", "behavior");
            AddRailItem(nav, "info", "О программе", "about");
            Grid.SetRow(nav, 1);
            rail.Children.Add(nav);

            _status = Glass.Label("", 11.5, true);
            _status.Opacity = 0;
            _status.Margin = new Thickness(8, 0, 0, 2);
            Grid.SetRow(_status, 3);
            rail.Children.Add(_status);
            return rail;
        }

        string RailIcon(string key)
        {
            switch (key)
            {
                case "look": return "palette";
                case "sounds": return "volume-2";
                case "behavior": return "sliders-horizontal";
                case "about": return "info";
                default: return "settings";
            }
        }

        void AddRailItem(StackPanel nav, string glyph, string label, string key)
        {
            var item = new Border();
            item.CornerRadius = new CornerRadius(10);
            item.Padding = new Thickness(11, 8, 11, 9);
            item.Margin = new Thickness(0, 1, 0, 1);
            item.Cursor = Cursors.Hand;
            item.Background = key == "petals" ? Ui.Solid(Ui.Accent, 0.22) : Brushes.Transparent;

            var sp = new StackPanel();
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
            item.Child = sp;

            var keyCopy = key;
            item.MouseEnter += delegate { if (_page != keyCopy) item.Background = Ui.Solid(Colors.White, 0.05); };
            item.MouseLeave += delegate { if (_page != keyCopy) item.Background = Brushes.Transparent; };
            item.MouseLeftButtonDown += delegate { ShowPage(keyCopy); };

            nav.Children.Add(item);
            _rail.Add(item);
        }

        void ShowPage(string key)
        {
            if (_page == key) return;
            _page = key;
            string[] keys = { "petals", "look", "sounds", "behavior", "about" };
            for (int i = 0; i < _rail.Count; i++)
            {
                bool active = keys[i] == key;
                _rail[i].Background = active ? Ui.Solid(Ui.Accent, 0.22) : Brushes.Transparent;
                _railGlyphs[i].Content = LucideHost.Build(RailIcon(keys[i]), 15, false);
                var host = (ContentControl)((StackPanel)_rail[i].Child).Children[0];
                var pathEl = (System.Windows.Shapes.Path)((Canvas)((Viewbox)host.Content).Child).Children[0];
                pathEl.Stroke = active ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
            }
            if (_pageTitle != null) _pageTitle.Text = PageTitle(key);
            FrameworkElement page;
            if (!_pages.TryGetValue(key, out page))
            {
                if (key == "look") page = BuildLookPage();
                else if (key == "sounds") page = BuildSoundsPage();
                else if (key == "behavior") page = BuildBehaviorPage();
                else if (key == "about") page = BuildAboutPage();
                else page = BuildPetalsPage();
                _pages[key] = page;
                _pageHost.Content = page;
            }
            _pageHost.Content = page;
            if (App.Config.AnimationsEnabled)
            {
                page.Opacity = 0;
                page.BeginAnimation(UIElement.OpacityProperty, null);
                var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(180)));
                fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                page.BeginAnimation(UIElement.OpacityProperty, fade);
                var tt = new TranslateTransform(0, 8);
                page.RenderTransform = tt;
                var ty = new DoubleAnimation(8, 0, new Duration(TimeSpan.FromMilliseconds(220)));
                ty.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                tt.BeginAnimation(TranslateTransform.YProperty, ty);
            }
        }

        // ================= content column =================

        Grid BuildContentColumn()
        {
            var col = new Grid();
            col.Margin = new Thickness(10, 16, 20, 16);
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            col.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var head = new Grid();
            head.Margin = new Thickness(4, 0, 0, 14);
            _pageTitle = Glass.Label(PageTitle("petals"), 22, false, true);
            head.Children.Add(_pageTitle);

            // close button — top right, always visible
            var close = MakeCloseButton();
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Top;
            close.Margin = new Thickness(0, -2, -6, 0);
            head.Children.Add(close);

            Grid.SetRow(head, 0);
            col.Children.Add(head);

            _pageHost = new ContentControl();
            _pages["petals"] = BuildPetalsPage();
            _pageHost.Content = _pages["petals"];
            Grid.SetRow(_pageHost, 1);
            col.Children.Add(_pageHost);
            return col;
        }

        string PageTitle(string key)
        {
            switch (key)
            {
                case "look": return "Внешний вид";
                case "sounds": return "Звуки";
                case "behavior": return "Поведение";
                case "about": return "О программе";
                default: return "Лепестки";
            }
        }

        Border MakeCloseButton()
        {
            var b = new Border();
            b.Width = 30; b.Height = 30;
            b.CornerRadius = new CornerRadius(15);
            b.Background = Brushes.Transparent;
            b.Cursor = Cursors.Hand;
            var t = new TextBlock();
            t.Text = ((char)0xE8BB).ToString();
            t.FontFamily = Ui.Icons;
            t.FontSize = 11;
            t.Foreground = Ui.Solid(Ui.InkDim, 1);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            b.MouseEnter += delegate { b.Background = Ui.Solid(Ui.Danger, 0.9); t.Foreground = Ui.Solid(Colors.White, 1); };
            b.MouseLeave += delegate { b.Background = Brushes.Transparent; t.Foreground = Ui.Solid(Ui.InkDim, 1); };
            b.MouseLeftButtonDown += delegate { Close(); };
            return b;
        }

        // ================= shared builders =================

        TextBlock Section(string text)
        {
            var t = new TextBlock();
            t.Text = text.ToUpperInvariant();
            t.FontFamily = Ui.Text;
            t.FontSize = 10.5;
            t.FontWeight = FontWeights.SemiBold;
            t.Foreground = Ui.Solid(Ui.InkDim, 0.9);
            t.Margin = new Thickness(2, 18, 0, 8);
            return t;
        }

        FrameworkElement Sep()
        {
            var line = new Border();
            line.Height = 1;
            line.Background = Ui.Solid(Colors.White, 0.06);
            line.Margin = new Thickness(0, 10, 0, 2);
            return line;
        }

        TextBlock Note(string text)
        {
            var t = Glass.Label(text, 11.5, true);
            t.TextWrapping = TextWrapping.Wrap;
            t.LineHeight = 16.5;
            t.Margin = new Thickness(2, 8, 8, 0);
            return t;
        }

        FrameworkElement Setting(string title, string sub, FrameworkElement control)
        {
            var grid = new Grid();
            grid.Margin = new Thickness(2, 9, 0, 9);
            var sp = new StackPanel();
            sp.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(Glass.Label(title, 13.5, false, true));
            if (!string.IsNullOrEmpty(sub))
            {
                var s = Glass.Label(sub, 11.5, true);
                s.Margin = new Thickness(0, 2, 14, 0);
                s.TextWrapping = TextWrapping.Wrap;
                sp.Children.Add(s);
            }
            grid.Children.Add(sp);
            if (control != null)
            {
                control.VerticalAlignment = VerticalAlignment.Center;
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(control, 1);
                grid.Children.Add(control);
            }
            return grid;
        }

        ScrollViewer Scroller(FrameworkElement content)
        {
            var sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.Content = content;
            return sv;
        }

        public void Pulse()
        {
            if (_suppressPulse) return;
            _status.Text = "Сохранено ✓";
            var a = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(140)));
            _status.BeginAnimation(OpacityProperty, a);
            var t = new System.Windows.Threading.DispatcherTimer();
            t.Interval = TimeSpan.FromMilliseconds(1400);
            t.Tick += delegate
            {
                t.Stop();
                var f = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(500)));
                _status.BeginAnimation(OpacityProperty, f);
            };
            t.Start();
        }

        // ================= icon library =================

        static readonly string[][] IconLibrary = new string[][]
        {
            new[] { "mic", "микрофон микрофон mic звукозапись" },
            new[] { "mic-off", "микрофон выкл mic-off мут" },
            new[] { "volume-2", "звук громкость volume динамик" },
            new[] { "volume-1", "звук тише volume маленький" },
            new[] { "volume-x", "звук выкл мут volume-x" },
            new[] { "play", "play играть пуск музыка старт" },
            new[] { "pause", "пауза pause стоп" },
            new[] { "music", "музыка нота music трек" },
            new[] { "skip-forward", "следующий трек next вперёд" },
            new[] { "skip-back", "предыдущий трек prev назад" },
            new[] { "app-window", "приложение окно app программа" },
            new[] { "link-2", "ссылка link адрес url цепь" },
            new[] { "settings", "настройки gear шестерёнка settings" },
            new[] { "folder", "папка folder директория" },
            new[] { "folder-open", "папка открыта открыть folder" },
            new[] { "keyboard", "клавиатура keyboard" },
            new[] { "globe", "глобус интернет globe web сеть" },
            new[] { "star", "звезда star избранное лайк" },
            new[] { "lock", "замок lock блокировка закрыть" },
            new[] { "camera", "камера фото camera снимок скриншот" },
            new[] { "image", "изображение картинка image фото" },
            new[] { "crop", "скриншот обрезка crop выделить" },
            new[] { "search", "поиск найти search лупа" },
            new[] { "bell", "уведомление колокольчик bell звонок" },
            new[] { "download", "скачать download загрузка вниз" },
            new[] { "upload", "загрузить upload вверх экспорт" },
            new[] { "copy", "копировать copy" },
            new[] { "clipboard", "буфер clipboard вставить" },
            new[] { "trash-2", "корзина удалить мусор trash bin" },
            new[] { "power", "питание power выключить" },
            new[] { "moon", "луна сон ночь спящий moon" },
            new[] { "monitor", "монитор экран desktop дисплей" },
            new[] { "monitor-off", "монитор выкл погасить экран" },
            new[] { "pin", "закрепить pin поверх булавка" },
            new[] { "refresh-cw", "обновить refresh заново" },
            new[] { "rotate-cw", "перезагрузка rotate поворот" },
            new[] { "calculator", "калькулятор calculator посчитать" },
            new[] { "smile", "смайл эмодзи smile улыбка" },
            new[] { "gamepad-2", "геймпад игра game steam" },
            new[] { "headphones", "наушники headphones гарнитура" },
            new[] { "activity", "диспетчер activity пульс задачи" },
            new[] { "minimize-2", "свернуть minimize окно" },
            new[] { "message-circle", "сообщение чат message discord" },
            new[] { "file-text", "документ файл текст file текстовый" },
            new[] { "grid-2x2", "приложения сетка grid все" },
            new[] { "sliders-horizontal", "ползунки sliders поведение настройки" },
            new[] { "palette", "палитра palette краски цвет" },
            new[] { "info", "информация info сведения" }
        };

        // ================= petals =================

        FrameworkElement BuildPetalsPage()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(228) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ---- left: list + preview ----
            var left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _petalList = new StackPanel();
            Grid.SetRow(_petalList, 0);
            left.Children.Add(_petalList);

            var vb = new Viewbox();
            vb.Stretch = Stretch.Uniform;
            vb.Margin = new Thickness(0, 10, 0, 4);
            _preview = new FlowerMenu(300);
            _preview.PetalActivated += delegate (int i)
            {
                _selPetal = i;
                _preview.SetSelected(_selPetal);
                RefreshPetalList();
                FillEditor();
                _preview.ReleasePulseAt(i);
            };
            _preview.CenterActivated += delegate { _preview.PlayIn(); };
            vb.Child = _preview;
            Grid.SetRow(vb, 1);
            left.Children.Add(vb);

            var btnRow = new StackPanel();
            btnRow.Orientation = Orientation.Horizontal;
            btnRow.Margin = new Thickness(0, 8, 0, 0);
            var add = new PillButton("+ Лепесток", false);
            add.Clicked += delegate
            {
                if (App.Config.Petals.Count >= 8) return;
                App.Config.Petals.Add(new PetalConfig("Новый", "E713", "none", "", ""));
                App.SaveConfig();
                _selPetal = App.Config.Petals.Count - 1;
                _preview.Rebuild();
                _preview.SetSelected(_selPetal);
                RefreshPetalList();
                FillEditor();
                Pulse();
            };
            var del = new PillButton("− Убрать", false);
            del.Margin = new Thickness(8, 0, 0, 0);
            del.Clicked += delegate
            {
                if (App.Config.Petals.Count <= 2) return;
                App.Config.Petals.RemoveAt(_selPetal);
                App.SaveConfig();
                if (_selPetal >= App.Config.Petals.Count) _selPetal = App.Config.Petals.Count - 1;
                _preview.Rebuild();
                _preview.SetSelected(_selPetal);
                RefreshPetalList();
                FillEditor();
                Pulse();
            };
            btnRow.Children.Add(add);
            btnRow.Children.Add(del);
            Grid.SetRow(btnRow, 2);
            left.Children.Add(btnRow);

            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            // ---- right: editor ----
            _editor = new StackPanel();
            var editorScroll = Scroller(_editor);
            Grid.SetColumn(editorScroll, 2);
            grid.Children.Add(editorScroll);

            RefreshPetalList();
            FillEditor();
            return grid;
        }

        void RefreshPetalList()
        {
            _petalList.Children.Clear();
            for (int i = 0; i < App.Config.Petals.Count; i++)
            {
                int idx = i;
                var p = App.Config.Petals[i];
                var row = new Border();
                row.CornerRadius = new CornerRadius(10);
                row.Padding = new Thickness(10, 7, 10, 8);
                row.Margin = new Thickness(0, 1, 0, 1);
                row.Cursor = Cursors.Hand;
                bool sel = idx == _selPetal;
                row.Background = sel ? Ui.Solid(Ui.Accent, 0.22) : Brushes.Transparent;

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var ic = LucideHost.Build(p.Action == "app" && !string.IsNullOrEmpty(p.Target) ? "app-window" : p.Icon, 14, false);
                var icHost = new ContentControl();
                icHost.Content = ic;
                icHost.Width = 15; icHost.Height = 15;
                icHost.VerticalAlignment = VerticalAlignment.Center;
                var icBorder = new Border();
                icBorder.Child = icHost;
                Grid.SetColumn(icBorder, 0);
                g.Children.Add(icBorder);
                System.Action tint = delegate
                {
                    var pathEl = (System.Windows.Shapes.Path)((Canvas)((Viewbox)icHost.Content).Child).Children[0];
                    pathEl.Stroke = sel ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
                };
                tint();
                var t = Glass.Label(string.IsNullOrEmpty(p.Label) ? "Лепесток " + (i + 1) : p.Label, 12.5, false, sel);
                t.Margin = new Thickness(10, 0, 0, 0);
                t.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(t, 1);
                g.Children.Add(t);
                row.Child = g;

                row.MouseEnter += delegate { if (idx != _selPetal) row.Background = Ui.Solid(Colors.White, 0.05); };
                row.MouseLeave += delegate { if (idx != _selPetal) row.Background = Brushes.Transparent; };
                row.MouseLeftButtonDown += delegate
                {
                    _selPetal = idx;
                    _preview.SetSelected(_selPetal);
                    RefreshPetalList();
                    FillEditor();
                };
                _petalList.Children.Add(row);
            }
        }

        void FillEditor()
        {
            var p = App.Config.Petals[_selPetal];
            _editor.Children.Clear();

            _editor.Children.Add(Section("Название"));
            var nameBox = new GlassTextBox(p.Label, "Например: Мут микро", 240);
            nameBox.Box.TextChanged += delegate
            {
                App.Config.Petals[_selPetal].Label = nameBox.Value;
                App.SaveConfig();
                _preview.UpdatePetal(_selPetal);
                RefreshPetalList();
                Pulse();
            };
            _editor.Children.Add(nameBox);

            _editor.Children.Add(Section("Иконка"));
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
                    if (shown < 3) OverlayWindow.Log("TILE " + gl + " hasLucide=" + Lucide.Has(gl));
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
            _iconSearch.Box.TextChanged += delegate { rebuildIcons(_iconSearch.Value); };

            _editor.Children.Add(Section("Действие"));
            var actionItems = new List<string>(Actions.Names);
            var drop = new GlassDropDown(actionItems, Math.Max(0, Array.IndexOf(Actions.Keys, p.Action)));
            drop.Margin = new Thickness(0, 2, 0, 0);
            drop.KeepOpenOnSelect = true;
            drop.Changed += delegate
            {
                App.Config.Petals[_selPetal].Action = Actions.Keys[drop.SelectedIndex];
                App.SaveConfig();
                _preview.UpdatePetal(_selPetal);
                RefreshPetalList();
                FillActionFields();
                Pulse();
            };
            _editor.Children.Add(drop);

            FillActionFields();
        }

        void FillActionFields()
        {
            var p = App.Config.Petals[_selPetal];
            int dropIndex = -1;
            for (int i = _editor.Children.Count - 1; i >= 0; i--)
            {
                if (_editor.Children[i] is GlassDropDown) { dropIndex = i; break; }
            }
            if (dropIndex < 0) return;
            for (int i = _editor.Children.Count - 1; i > dropIndex; i--)
                _editor.Children.RemoveAt(i);

            switch (p.Action)
            {
                case "app":
                {
                    _editor.Children.Add(Section("Путь / команда"));
                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var tb = new GlassTextBox(p.Target, @"C:\...\app.exe или steam://open/games", 220);
                    tb.Margin = new Thickness(0, 2, 8, 0);
                    tb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Target = tb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    Grid.SetColumn(tb, 0);
                    row.Children.Add(tb);
                    var browse = new PillButton("Обзор…", false);
                    browse.Margin = new Thickness(0, 2, 0, 0);
                    browse.Clicked += delegate
                    {
                        var dlg = new Microsoft.Win32.OpenFileDialog();
                        dlg.Title = "Выберите приложение";
                        dlg.Filter = "Приложения (*.exe;*.lnk;*.url)|*.exe;*.lnk;*.url|Все файлы|*.*";
                        if (dlg.ShowDialog() == true)
                        {
                            tb.Value = dlg.FileName;
                            App.Config.Petals[_selPetal].Target = dlg.FileName;
                            App.SaveConfig(); Pulse();
                        }
                    };
                    Grid.SetColumn(browse, 1);
                    row.Children.Add(browse);
                    _editor.Children.Add(row);

                    _editor.Children.Add(Section("Аргументы запуска"));
                    var args = new GlassTextBox(p.Args, "--fullscreen", 220);
                    args.Margin = new Thickness(0, 2, 0, 0);
                    args.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Args = args.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _editor.Children.Add(args);
                    _editor.Children.Add(Note("Работают .exe, .lnk и steam:// ссылки — откроется как обычный ярлык."));
                    break;
                }
                case "url":
                {
                    _editor.Children.Add(Section("Ссылка"));
                    var tb = new GlassTextBox(p.Target, "youtube.com", 220);
                    tb.Margin = new Thickness(0, 2, 0, 0);
                    tb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Target = tb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _editor.Children.Add(tb);
                    _editor.Children.Add(Note("Откроется в браузере по умолчанию."));
                    break;
                }
                case "hotkey":
                {
                    _editor.Children.Add(Section("Сочетание клавиш"));
                    var hk = new HotkeyBox(p.Target);
                    hk.Margin = new Thickness(0, 2, 0, 0);
                    hk.Changed += delegate
                    {
                        App.Config.Petals[_selPetal].Target = hk.Combo;
                        App.SaveConfig(); Pulse();
                    };
                    _editor.Children.Add(hk);
                    _editor.Children.Add(Note("Hinoki нажмёт это сочетание в активном приложении."));
                    break;
                }
                case "discord_mute":
                    _editor.Children.Add(Note("Поднимет Discord и отправит «" + HotkeyBox.PrettyCombo(App.Config.DiscordMuteKeys) + "». Сочетание — в разделе «Поведение»."));
                    break;
                case "discord_deafen":
                    _editor.Children.Add(Note("Поднимет Discord и отправит «" + HotkeyBox.PrettyCombo(App.Config.DiscordDeafenKeys) + "». Сочетание — в разделе «Поведение»."));
                    break;
                case "mic_toggle":
                    _editor.Children.Add(Note("Общий микрофон системы. Нажмите на лепесток — состояние переключится, лепесток подсветится красным, пока микрофон выключен."));
                    break;
                case "volume_mute":
                    _editor.Children.Add(Note("Общий звук системы. Лепесток подсвечивается красным, пока звук выключен."));
                    break;
                case "media":
                    _editor.Children.Add(Note("Play / Pause для Spotify, YouTube Music и других плееров."));
                    break;
                case "screenshot_file":
                    _editor.Children.Add(Note("Снимок всего экрана сохранится в «Изображения»."));
                    break;
                case "screenshot_buffer":
                    _editor.Children.Add(Note("Откроет системные «Ножницы» (Win+Shift+S) — выделите область."));
                    break;
                case "shutdown_pc":
                case "restart_pc":
                case "sleep_pc":
                    _editor.Children.Add(Note("Сработает сразу, без подтверждения."));
                    break;
                default:
                    _editor.Children.Add(Note("Выберите действие — лепесток выполнит его одним нажатием."));
                    break;
            }
        }

        // ================= look =================

        FrameworkElement BuildLookPage()
        {
            var stack = new StackPanel();
            var cfgApp = App.Config;

            stack.Children.Add(Section("Меню"));

            var opVal = Glass.Label(cfgApp.PetalOpacity + "%", 13, true);
            var opSlider = new GlassSlider(30, 100, cfgApp.PetalOpacity);
            opSlider.Changed += delegate
            {
                App.Config.PetalOpacity = (int)Math.Round(opSlider.Value);
                opVal.Text = App.Config.PetalOpacity + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();
            };
            stack.Children.Add(Setting("Прозрачность лепестков", null, AttachSlider(opSlider, opVal)));
            stack.Children.Add(Sep());

            var glowVal = Glass.Label(cfgApp.GlowStrength + "%", 13, true);
            var glowSlider = new GlassSlider(0, 100, cfgApp.GlowStrength);
            glowSlider.Changed += delegate
            {
                App.Config.GlowStrength = (int)Math.Round(glowSlider.Value);
                glowVal.Text = App.Config.GlowStrength + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();
            };
            stack.Children.Add(Setting("Сила свечения", null, AttachSlider(glowSlider, glowVal)));
            stack.Children.Add(Sep());

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
            stack.Children.Add(Sep());

            var sizeVal = Glass.Label(cfgApp.MenuSizePercent + "%", 13, true);
            var sizeSlider = new GlassSlider(50, 100, cfgApp.MenuSizePercent);
            sizeSlider.Changed += delegate
            {
                App.Config.MenuSizePercent = (int)Math.Round(sizeSlider.Value);
                sizeVal.Text = App.Config.MenuSizePercent + "%";
                App.SaveConfig(); Pulse();
            };
            stack.Children.Add(Setting("Размер меню", null, AttachSlider(sizeSlider, sizeVal)));
            stack.Children.Add(Sep());

            var entranceItems = new List<string> { "Пружина (живой)", "Плавно", "Эластик", "Отскок", "Мгновенно" };
            var entranceMap = new[] { "spring", "smooth", "elastic", "bounce", "instant" };
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
            stack.Children.Add(Setting("Скорость анимаций", null, AttachSlider(spSlider, spVal)));

            stack.Children.Add(Section("Стекло"));

            var tgLight = new GlassToggle(cfgApp.LightGlass);
            tgLight.Changed += delegate { App.Config.LightGlass = tgLight.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            stack.Children.Add(Setting("Светлое стекло", "Белые лепестки с тёмными иконками", tgLight));
            stack.Children.Add(Sep());

            var tgLabels = new GlassToggle(cfgApp.ShowLabels);
            tgLabels.Changed += delegate { App.Config.ShowLabels = tgLabels.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            stack.Children.Add(Setting("Показывать названия", "Подписи на лепестках и под кнопкой настроек", tgLabels));
            stack.Children.Add(Sep());

            return WrapPage("Внешний вид", stack);
        }

        FrameworkElement AttachSlider(GlassSlider slider, TextBlock value)
        {
            var host = new Grid();
            host.MinWidth = 240;
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            slider.MinWidth = 170;
            slider.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(slider, 0);
            host.Children.Add(slider);
            Grid.SetColumn(value, 1);
            host.Children.Add(value);
            return host;
        }

        void RefreshAccent()
        {
            // rebuild pages so every accent-tinted element picks up the new color
            _pages.Clear();
            _preview.Rebuild();
            _preview.SetSelected(_selPetal);
            RefreshPetalList();
            FillEditor();
            _pageHost.Content = null;
            FrameworkElement page;
            if (!_pages.TryGetValue(_page, out page))
            {
                if (_page == "look") page = BuildLookPage();
                else if (_page == "sounds") page = BuildSoundsPage();
                else if (_page == "behavior") page = BuildBehaviorPage();
                else if (_page == "about") page = BuildAboutPage();
                else page = BuildPetalsPage();
                _pages[_page] = page;
            }
            _pageHost.Content = page;
        }

        // ================= sounds =================

        FrameworkElement BuildSoundsPage()
        {
            var stack = new StackPanel();
            var cfgApp = App.Config;

            var tgAll = new GlassToggle(cfgApp.SoundEnabled);
            tgAll.Changed += delegate { App.Config.SoundEnabled = tgAll.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Звуки", "Мягкие стеклянные щелчки при работе с меню", tgAll));
            stack.Children.Add(Sep());

            var volVal = Glass.Label(cfgApp.SoundVolume + "%", 13, true);
            var volSlider = new GlassSlider(0, 100, cfgApp.SoundVolume);
            volSlider.Changed += delegate
            {
                App.Config.SoundVolume = (int)Math.Round(volSlider.Value);
                volVal.Text = App.Config.SoundVolume + "%";
                App.SaveConfig(); Pulse();
            };
            stack.Children.Add(Setting("Громкость", null, AttachSlider(volSlider, volVal)));
            stack.Children.Add(Sep());

            var tgOpen = new GlassToggle(cfgApp.SoundOpen);
            tgOpen.Changed += delegate { App.Config.SoundOpen = tgOpen.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Раскрытие меню", "Мягкий двухтональный «блум»", NewRow(tgOpen, PlayBtn("open"))));
            stack.Children.Add(Sep());

            var tgClose = new GlassToggle(cfgApp.SoundClose);
            tgClose.Changed += delegate { App.Config.SoundClose = tgClose.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Закрытие меню", "Обратный тон при сворачивании", NewRow(tgClose, PlayBtn("close"))));
            stack.Children.Add(Sep());

            var tgClick = new GlassToggle(cfgApp.SoundClick);
            tgClick.Changed += delegate { App.Config.SoundClick = tgClick.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Выбор лепестка", "Короткий тик при нажатии", NewRow(tgClick, PlayBtn("click"))));

            return WrapPage("Звуки", stack);
        }

        FrameworkElement PlayBtn(string sound)
        {
            var b = new Border();
            b.Width = 28; b.Height = 28;
            b.CornerRadius = new CornerRadius(14);
            b.Background = Ui.Solid(Colors.White, 0.06);
            b.Cursor = Cursors.Hand;
            b.Margin = new Thickness(10, 0, 0, 0);
            var t = new TextBlock();
            t.Text = ((char)0xE768).ToString();
            t.FontFamily = Ui.Icons;
            t.FontSize = 10;
            t.Foreground = Ui.Solid(Ui.Ink, 0.9);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            b.MouseEnter += delegate { b.Background = Ui.Solid(Ui.Accent, 0.2); };
            b.MouseLeave += delegate { b.Background = Ui.Solid(Colors.White, 0.06); };
            b.MouseLeftButtonDown += delegate { Sounds.Play(sound); };
            return b;
        }

        FrameworkElement NewRow(FrameworkElement a, FrameworkElement b)
        {
            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Children.Add(a);
            if (b != null) sp.Children.Add(b);
            return sp;
        }

        // ================= behavior =================

        FrameworkElement BuildBehaviorPage()
        {
            var stack = new StackPanel();
            var cfgApp = App.Config;

            var tgIntercept = new GlassToggle(cfgApp.InterceptClick);
            tgIntercept.Changed += delegate { App.Config.InterceptClick = tgIntercept.IsOn; MouseHook.Intercept = tgIntercept.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Перехватывать среднюю кнопку", "Колёсико открывает Hinoki вместо прокрутки", tgIntercept));
            stack.Children.Add(Sep());

            var tgCursor = new GlassToggle(cfgApp.ShowAtCursor);
            tgCursor.Changed += delegate { App.Config.ShowAtCursor = tgCursor.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Меню у курсора", "Цветок появляется там, где сейчас курсор", tgCursor));
            stack.Children.Add(Sep());

            var tgClose = new GlassToggle(cfgApp.CloseAfterAction);
            tgClose.Changed += delegate { App.Config.CloseAfterAction = tgClose.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Закрывать после действия", "Меню сворачивается сразу после нажатия лепестка", tgClose));
            stack.Children.Add(Sep());

            var tgOutside = new GlassToggle(cfgApp.CloseOnOutsideClick);
            tgOutside.Changed += delegate { App.Config.CloseOnOutsideClick = tgOutside.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Закрывать по клику мимо", "Клик в любом другом месте сворачивает меню", tgOutside));
            stack.Children.Add(Sep());
            var acVal = Glass.Label(cfgApp.AutoCloseSeconds > 0 ? cfgApp.AutoCloseSeconds + " с" : "выкл", 13, true);
            var acSlider = new GlassSlider(0, 30, cfgApp.AutoCloseSeconds);
            acSlider.Changed += delegate
            {
                App.Config.AutoCloseSeconds = (int)Math.Round(acSlider.Value);
                acVal.Text = App.Config.AutoCloseSeconds > 0 ? App.Config.AutoCloseSeconds + " с" : "выкл";
                App.SaveConfig(); Pulse();
            };
            stack.Children.Add(Setting("Автозакрытие меню", "Само свернётся через N секунд без действий (0 — никогда)", NewRow(AttachSlider(acSlider, acVal), null)));
            stack.Children.Add(Sep());

            var tgAnim = new GlassToggle(cfgApp.AnimationsEnabled);
            tgAnim.Changed += delegate { App.Config.AnimationsEnabled = tgAnim.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Анимации", "Пружинное раскрытие и плавные переходы", tgAnim));

            stack.Children.Add(Section("Сочетания клавиш"));

            var hk = new HotkeyBox(cfgApp.GlobalHotkey);
            hk.Changed += delegate
            {
                App.Config.GlobalHotkey = hk.Combo;
                App.SaveConfig();
                App.ApplyGlobalHotkey();
                Pulse();
            };
            stack.Children.Add(Setting("Хоткей открытия меню", "Клавиши вызывают цветок без мыши. Пусто — выключено", hk));
            stack.Children.Add(Sep());

            var hkMute = new HotkeyBox(cfgApp.DiscordMuteKeys);
            hkMute.Changed += delegate { App.Config.DiscordMuteKeys = hkMute.Combo; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Мут микрофона — Discord", "Должно совпадать с биндом в Discord", hkMute));
            stack.Children.Add(Sep());

            var hkDeafen = new HotkeyBox(cfgApp.DiscordDeafenKeys);
            hkDeafen.Changed += delegate { App.Config.DiscordDeafenKeys = hkDeafen.Combo; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Мут звука — Discord", "Должно совпадать с биндом в Discord", hkDeafen));

            stack.Children.Add(Section("Система"));

            var tgAuto = new GlassToggle(cfgApp.Autostart);
            tgAuto.Changed += delegate { App.Config.Autostart = tgAuto.IsOn; App.ApplyAutostart(); App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Запускать вместе с Windows", "Hinoki будет ждать вас в трее", tgAuto));

            var rowBtns = new StackPanel();
            rowBtns.Orientation = Orientation.Horizontal;
            rowBtns.Margin = new Thickness(0, 4, 0, 0);
            var testBtn = new PillButton("Открыть меню (тест)", false);
            testBtn.Clicked += delegate { App.TestMenu(); };
            rowBtns.Children.Add(testBtn);
            var restart = new PillButton("Перезапустить Hinoki", false);
            restart.Margin = new Thickness(8, 0, 0, 0);
            restart.Clicked += delegate { App.RestartApp(); };
            rowBtns.Children.Add(restart);
            stack.Children.Add(rowBtns);

            return WrapPage("Поведение", stack);
        }

        // ================= about =================

        FrameworkElement BuildAboutPage()
        {
            var stack = new StackPanel();

            var logoWrap = new Border();
            logoWrap.Width = 72; logoWrap.Height = 72;
            logoWrap.CornerRadius = new CornerRadius(21);
            logoWrap.Child = new Image { Source = FlowerMenu.LogoBitmap, Width = 72, Height = 72 };
            logoWrap.HorizontalAlignment = HorizontalAlignment.Center;
            logoWrap.Effect = new DropShadowEffect { Color = Ui.Accent, BlurRadius = 24, ShadowDepth = 0, Opacity = 0.45 };
            stack.Children.Add(logoWrap);

            var name = Glass.Label("Hinoki", 24, false, true);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.Margin = new Thickness(0, 14, 0, 0);
            stack.Children.Add(name);

            var ver = Glass.Label("Версия 1.3.1 · Windows 10/11", 12, true);
            ver.HorizontalAlignment = HorizontalAlignment.Center;
            ver.Margin = new Thickness(0, 3, 0, 0);
            stack.Children.Add(ver);

            var desc = Glass.Label("Средняя кнопка мыши — и всё под рукой: Discord, музыка, приложения. Стеклянный цветок, который всегда рядом.", 12.5, true);
            desc.TextWrapping = TextWrapping.Wrap;
            desc.TextAlignment = TextAlignment.Center;
            desc.LineHeight = 19;
            desc.Margin = new Thickness(30, 12, 30, 0);
            stack.Children.Add(desc);

            var links = new StackPanel();
            links.Orientation = Orientation.Horizontal;
            links.HorizontalAlignment = HorizontalAlignment.Center;
            links.Margin = new Thickness(0, 18, 0, 0);
            var gh = new PillButton("GitHub", false);
            gh.Clicked += delegate { ActionEngine.OpenLink("https://github.com/arseniyru90-afk/hinoki"); };
            var site = new PillButton("Сайт", false);
            site.Margin = new Thickness(8, 0, 0, 0);
            site.Clicked += delegate { ActionEngine.OpenLink("https://arseniyru90-afk.github.io/hinoki/"); };
            var upd = new PillButton("Обновления", false);
            upd.Margin = new Thickness(8, 0, 0, 0);
            upd.Clicked += delegate { ActionEngine.OpenLink("https://github.com/arseniyru90-afk/hinoki/releases/latest"); };
            links.Children.Add(gh);
            links.Children.Add(site);
            links.Children.Add(upd);
            stack.Children.Add(links);

            var reset = new PillButton("Сбросить все настройки", false);
            reset.HorizontalAlignment = HorizontalAlignment.Center;
            reset.Margin = new Thickness(0, 16, 0, 0);
            reset.Clicked += delegate { App.ResetConfig(this); };
            stack.Children.Add(reset);

            return stack;
        }

        FrameworkElement WrapPage(string title, FrameworkElement content)
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            // title lives in the shared header; here only scrollable content
            var sv = Scroller(content);
            Grid.SetRow(sv, 0);
            Grid.SetRowSpan(sv, 2);
            g.Children.Add(sv);
            return g;
        }
    }
}
