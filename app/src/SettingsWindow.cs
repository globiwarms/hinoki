// Hinoki — Apple-style settings
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
            if (_instance != null) { _instance.Activate(); return; }
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
        StackPanel _petalList, _editor;
        Border _chrome;
        TextBlock _status, _pageTitle, _pageSub;
        FlowerMenu _preview;
        List<Border> _rail = new List<Border>();
        List<ContentControl> _railGlyphs = new List<ContentControl>();
        Dictionary<string, FrameworkElement> _pages = new Dictionary<string, FrameworkElement>();
        ContentControl _pageHost;
        string _page = "petals";
        GlassTextBox _iconSearch;
        WrapPanel _iconPanel;
        bool _expandedIcons;
        string _expandLabel = "";
        System.Windows.Threading.DispatcherTimer _rebuildDebounce;

        public SettingsWindow()
        {
            Title = "Hinoki";
            Width = 1020; Height = 660;
            MinWidth = 900; MinHeight = 580;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanResize;
            Topmost = true;
            Left = Math.Max(0, (SystemParameters.WorkArea.Width - Width) / 2 + SystemParameters.WorkArea.Left);
            Top = Math.Max(0, (SystemParameters.WorkArea.Height - Height) / 2 + SystemParameters.WorkArea.Top);
            PreviewKeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { Close(); e.Handled = true; }
            };

            var root = new Grid();
            root.Margin = new Thickness(16);

            _chrome = new Border();
            _chrome.CornerRadius = new CornerRadius(20);
            var fill = new LinearGradientBrush();
            fill.StartPoint = new Point(0, 0);
            fill.EndPoint = new Point(0.7, 1);
            fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x1C, 0x22), 0));
            fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x12, 0x13, 0x18), 0.55));
            fill.GradientStops.Add(new GradientStop(Color.FromRgb(0x0E, 0x0F, 0x13), 1));
            fill.Freeze();
            _chrome.Background = fill;
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.09);
            _chrome.BorderThickness = new Thickness(1);
            _chrome.ClipToBounds = true;
            _chrome.SizeChanged += delegate(object snd, SizeChangedEventArgs ev)
            {
                _chrome.Clip = new RectangleGeometry(new Rect(0, 0, ev.NewSize.Width, ev.NewSize.Height), 20, 20);
            };

            var layout = new Grid();
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            layout.Children.Add(BuildRail());
            var contentCol = BuildContentColumn();
            Grid.SetColumn(contentCol, 1);
            layout.Children.Add(contentCol);

            _chrome.Child = layout;
            root.Children.Add(_chrome);
            Content = root;

            Loaded += delegate { Entrance(); };
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
            Opacity = 0;
            var a = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(280)));
            a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            BeginAnimation(OpacityProperty, a);
            var st = new ScaleTransform(0.97, 0.97);
            _chrome.RenderTransformOrigin = new Point(0.5, 0.5);
            _chrome.RenderTransform = st;
            var sxy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(340)));
            sxy.EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, sxy);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, sxy);
        }

        // ================= RAIL =================

        Grid BuildRail()
        {
            var rail = new Grid();
            rail.Margin = new Thickness(16, 16, 8, 16);
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rail.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var brand = new StackPanel();
            brand.Orientation = Orientation.Horizontal;
            brand.Margin = new Thickness(6, 2, 0, 0);
            var logo = new Border();
            logo.Width = 34; logo.Height = 34;
            logo.CornerRadius = new CornerRadius(10);
            var logoImg = new Image { Source = FlowerMenu.LogoBitmap, Width = 34, Height = 34 };
            RenderOptions.SetBitmapScalingMode(logoImg, BitmapScalingMode.Fant);
            logo.Child = logoImg;
            logo.Effect = new DropShadowEffect { Color = Ui.Accent, BlurRadius = 14, ShadowDepth = 0, Opacity = 0.4 };
            brand.Children.Add(logo);
            var bn = new StackPanel();
            bn.VerticalAlignment = VerticalAlignment.Center;
            bn.Margin = new Thickness(11, 0, 0, 0);
            bn.Children.Add(Glass.Label("Hinoki", 16, false, true));
            bn.Children.Add(Glass.Label("v1.2", 10, true));
            brand.Children.Add(bn);
            Grid.SetRow(brand, 0);
            rail.Children.Add(brand);

            var nav = new StackPanel();
            nav.Margin = new Thickness(0, 24, 0, 0);
            AddRailItem(nav, "flower-2", "Лепестки", "petals");
            AddRailItem(nav, "paintbrush", "Внешний вид", "look");
            AddRailItem(nav, "volume-2", "Звуки", "sounds");
            AddRailItem(nav, "sliders-horizontal", "Поведение", "behavior");
            AddRailItem(nav, "info", "О программе", "about");
            Grid.SetRow(nav, 1);
            rail.Children.Add(nav);

            _status = Glass.Label("", 11, true);
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
                case "look": return "paintbrush";
                case "sounds": return "volume-2";
                case "behavior": return "sliders-horizontal";
                case "about": return "info";
                default: return "flower-2";
            }
        }

        void AddRailItem(StackPanel nav, string glyph, string label, string key)
        {
            var item = new Border();
            item.CornerRadius = new CornerRadius(10);
            item.Padding = new Thickness(12, 8, 12, 9);
            item.Margin = new Thickness(0, 1, 0, 1);
            item.Cursor = Cursors.Hand;
            item.Background = key == "petals" ? Ui.Solid(Ui.Accent, 0.18) : Brushes.Transparent;

            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            var ic = LucideHost.Build(glyph, 16, false);
            var icHost = new ContentControl();
            icHost.Content = ic;
            icHost.Width = 17; icHost.Height = 17;
            icHost.VerticalAlignment = VerticalAlignment.Center;
            _railGlyphs.Add(icHost);
            sp.Children.Add(icHost);
            var t = Glass.Label(label, 13.5, false, true);
            t.Margin = new Thickness(12, 0, 0, 0);
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
            _page = key;
            string[] keys = { "petals", "look", "sounds", "behavior", "about" };
            for (int i = 0; i < _rail.Count; i++)
            {
                bool active = keys[i] == key;
                _rail[i].Background = active ? Ui.Solid(Ui.Accent, 0.18) : Brushes.Transparent;
                var pt = _railGlyphs[i].Content as System.Windows.Shapes.Path;
                if (pt != null) pt.Stroke = active ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
            }
            if (_pageTitle != null) _pageTitle.Text = PageTitle(key);
            if (_pageSub != null) _pageSub.Text = PageSub(key);

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
            if (App.Config != null && App.Config.AnimationsEnabled)
            {
                page.Opacity = 0;
                page.BeginAnimation(UIElement.OpacityProperty, null);
                var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)));
                fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                page.BeginAnimation(UIElement.OpacityProperty, fade);
                var tt = new TranslateTransform(0, 6);
                page.RenderTransform = tt;
                var ty = new DoubleAnimation(6, 0, new Duration(TimeSpan.FromMilliseconds(240)));
                ty.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                tt.BeginAnimation(TranslateTransform.YProperty, ty);
            }
        }

        // ================= CONTENT =================

        Grid BuildContentColumn()
        {
            var col = new Grid();
            col.Margin = new Thickness(8, 16, 24, 16);
            col.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            col.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var head = new Grid();
            head.Margin = new Thickness(4, 0, 0, 16);
            var headStack = new StackPanel();
            _pageTitle = Glass.Label(PageTitle("petals"), 24, false, true);
            headStack.Children.Add(_pageTitle);
            _pageSub = Glass.Label(PageSub("petals"), 12.5, true);
            _pageSub.Margin = new Thickness(1, 5, 0, 0);
            headStack.Children.Add(_pageSub);
            head.Children.Add(headStack);

            var close = MakeCloseButton();
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Top;
            close.Margin = new Thickness(0, -4, -4, 0);
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

        string PageSub(string key)
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
            b.MouseEnter += delegate { b.Background = Ui.Solid(Ui.Danger, 0.85); t.Foreground = Ui.Solid(Colors.White, 1); };
            b.MouseLeave += delegate { b.Background = Brushes.Transparent; t.Foreground = Ui.Solid(Ui.InkDim, 1); };
            b.MouseLeftButtonDown += delegate { Close(); };
            return b;
        }

        // ================= BUILDERS =================

        TextBlock Section(string text)
        {
            var t = new TextBlock();
            t.Text = text.ToUpperInvariant();
            t.FontFamily = Ui.Text;
            t.FontSize = 11;
            t.FontWeight = FontWeights.SemiBold;
            t.Foreground = Ui.Solid(Ui.InkDim, 0.85);
            t.Margin = new Thickness(2, 20, 0, 10);
            return t;
        }

        FrameworkElement Sep()
        {
            var line = new Border();
            line.Height = 1;
            line.Background = Ui.Solid(Colors.White, 0.05);
            line.Margin = new Thickness(0, 12, 0, 4);
            return line;
        }

        TextBlock Note(string text)
        {
            var t = Glass.Label(text, 11.5, true);
            t.TextWrapping = TextWrapping.Wrap;
            t.LineHeight = 16;
            t.Margin = new Thickness(2, 8, 8, 0);
            return t;
        }

        FrameworkElement Setting(string title, string sub, FrameworkElement control)
        {
            var grid = new Grid();
            grid.Margin = new Thickness(2, 10, 0, 10);
            var sp = new StackPanel();
            sp.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(Glass.Label(title, 14, false, true));
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

        FrameworkElement AttachSlider(GlassSlider slider, TextBlock value)
        {
            var host = new Grid();
            host.MinWidth = 220;
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            slider.MinWidth = 150;
            slider.Margin = new Thickness(0, 0, 12, 0);
            value.Width = 52;
            value.HorizontalAlignment = HorizontalAlignment.Right;
            value.TextAlignment = TextAlignment.Right;
            Grid.SetColumn(slider, 0);
            Grid.SetColumn(value, 1);
            host.Children.Add(slider);
            host.Children.Add(value);
            return host;
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

        static readonly string[][] IconLibrary = new string[][]
        {
            new[] { "mic", "микрофон микрофон mic" },
            new[] { "mic-off", "микрофон выкл mic-off" },
            new[] { "volume-2", "звук громкость volume" },
            new[] { "volume-1", "звук тише volume" },
            new[] { "volume-x", "звук выкл мут volume" },
            new[] { "play", "play играть пуск" },
            new[] { "pause", "пауза pause" },
            new[] { "music", "музыка нота music" },
            new[] { "skip-forward", "следующий next" },
            new[] { "skip-back", "предыдущий prev" },
            new[] { "app-window", "приложение окно app" },
            new[] { "link-2", "ссылка link адрес" },
            new[] { "settings", "настройки gear" },
            new[] { "folder", "папка folder" },
            new[] { "globe", "глобус интернет globe" },
            new[] { "star", "звезда star лайк" },
            new[] { "lock", "замок lock закрыть" },
            new[] { "camera", "камера фото camera" },
            new[] { "search", "поиск search найти" },
            new[] { "bell", "уведомление bell звонок" },
            new[] { "download", "скачать download вниз" },
            new[] { "copy", "копировать copy" },
            new[] { "clipboard", "буфер clipboard" },
            new[] { "trash-2", "корзина trash удалить" },
            new[] { "power", "питание power" },
            new[] { "moon", "луна moon сон" },
            new[] { "monitor", "монитор monitor" },
            new[] { "pin", "закрепить pin" },
            new[] { "refresh-cw", "обновить refresh" },
            new[] { "rotate-cw", "перезагрузка rotate" },
            new[] { "calculator", "калькулятор calculator" },
            new[] { "gamepad-2", "геймпад игра game" },
            new[] { "headphones", "наушники headphones" },
            new[] { "info", "информация info" }
        };

        // ================= PETALS PAGE =================

        FrameworkElement BuildPetalsPage()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // LEFT: petal list + preview + buttons
            var left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _petalList = new StackPanel();
            var listCard = new Border();
            listCard.CornerRadius = new CornerRadius(12);
            listCard.Background = Ui.Solid(Colors.White, 0.03);
            listCard.Padding = new Thickness(6);
            listCard.Child = _petalList;
            Grid.SetRow(listCard, 0);
            left.Children.Add(listCard);

            var previewWrap = new Border();
            previewWrap.CornerRadius = new CornerRadius(14);
            previewWrap.Background = Ui.Solid(Colors.White, 0.02);
            previewWrap.Margin = new Thickness(0, 12, 0, 0);
            previewWrap.VerticalAlignment = VerticalAlignment.Center;
            var pvb = new Viewbox();
            pvb.Stretch = Stretch.Uniform;
            pvb.Margin = new Thickness(4);
            pvb.MaxHeight = 280;
            _preview = new FlowerMenu(240);
            _preview.PetalActivated += delegate (int i)
            {
                _selPetal = i;
                _preview.SetSelected(_selPetal);
                RefreshPetalList();
                FillEditor();
            };
            _preview.CenterActivated += delegate { _preview.PlayIn(); };
            pvb.Child = _preview;
            previewWrap.Child = pvb;
            Grid.SetRow(previewWrap, 1);
            left.Children.Add(previewWrap);

            var btnRow = new StackPanel();
            btnRow.Orientation = Orientation.Horizontal;
            btnRow.Margin = new Thickness(0, 10, 0, 0);
            var add = new PillButton("+ Лепесток", true);
            add.Clicked += delegate
            {
                if (App.Config.Petals.Count >= 8) return;
                App.Config.Petals.Add(new PetalConfig("Новый", "settings", "none", "", ""));
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

            // RIGHT: editor
            _editor = new StackPanel();
            var editorScroll = Scroller(_editor);
            Grid.SetColumn(editorScroll, 1);
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
                row.CornerRadius = new CornerRadius(8);
                row.Padding = new Thickness(8, 6, 8, 7);
                row.Margin = new Thickness(0, 1, 0, 1);
                row.Cursor = Cursors.Hand;
                bool sel = idx == _selPetal;
                row.Background = sel ? Ui.Solid(Ui.Accent, 0.15) : Brushes.Transparent;

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var ic = LucideHost.Build(string.IsNullOrEmpty(p.Icon) ? "settings" : p.Icon, 14, false);
                var icHost = new ContentControl();
                icHost.Content = ic;
                icHost.Width = 15; icHost.Height = 15;
                icHost.VerticalAlignment = VerticalAlignment.Center;
                var icBorder = new Border();
                icBorder.Child = icHost;
                Grid.SetColumn(icBorder, 0);
                g.Children.Add(icBorder);
                var t = Glass.Label(string.IsNullOrEmpty(p.Label) ? "Лепесток " + (i + 1) : p.Label, 12.5, false, sel);
                t.Margin = new Thickness(8, 0, 0, 0);
                t.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(t, 1);
                g.Children.Add(t);
                row.Child = g;

                row.MouseEnter += delegate { if (idx != _selPetal) row.Background = Ui.Solid(Colors.White, 0.04); };
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
            var nameBox = new GlassTextBox(p.Label, "Название лепестка", 240);
            nameBox.Margin = new Thickness(0, 2, 0, 0);
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
            _iconSearch = new GlassTextBox("", "Поиск…", 240);
            _iconSearch.Margin = new Thickness(0, 2, 0, 8);
            _editor.Children.Add(_iconSearch);

            _iconPanel = new WrapPanel();
            _iconPanel.Margin = new Thickness(0, 0, 0, 0);
            _iconPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            var iconScroll = new ScrollViewer();
            iconScroll.MaxHeight = 176;
            iconScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            iconScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            iconScroll.Content = _iconPanel;
            _editor.Children.Add(iconScroll);

            IconTile.ActiveGroup = new List<IconTile>();
            Action<string> rebuildIcons = null;
            rebuildIcons = delegate(string query)
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
                _expandLabel = "Все иконки (" + shown + ")";
                if (!_expandedIcons)
                {
                    int keep = 9;
                    for (int i = _iconPanel.Children.Count - 1; i >= keep; i--)
                        _iconPanel.Children.RemoveAt(i);
                }
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
                    _editor.Children.Add(Section("Путь к приложению"));
                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var tb = new GlassTextBox(p.Target, @"C:\...\app.exe", 220);
                    tb.Margin = new Thickness(0, 2, 8, 0);
                    tb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Target = tb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    Grid.SetColumn(tb, 0);
                    row.Children.Add(tb);
                    var browse = new PillButton("Обзор", false);
                    browse.Margin = new Thickness(0, 2, 0, 0);
                    browse.Clicked += delegate
                    {
                        var dlg = new Microsoft.Win32.OpenFileDialog();
                        dlg.Title = "Выберите приложение";
                        dlg.Filter = "Приложения (*.exe;*.lnk)|*.exe;*.lnk";
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
                    break;
                case "url":
                    _editor.Children.Add(Section("Ссылка"));
                    var ub = new GlassTextBox(p.Target, "youtube.com", 220);
                    ub.Margin = new Thickness(0, 2, 0, 0);
                    ub.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Target = ub.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _editor.Children.Add(ub);
                    break;
                case "run_cmd":
                    _editor.Children.Add(Section("Команда CMD"));
                    var cb = new GlassTextBox(p.Target, "echo hello", 220);
                    cb.Margin = new Thickness(0, 2, 0, 0);
                    cb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Target = cb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _editor.Children.Add(cb);
                    break;
                case "open_folder":
                    _editor.Children.Add(Section("Путь к папке"));
                    var fb = new GlassTextBox(p.Target, @"C:\Users\...", 220);
                    fb.Margin = new Thickness(0, 2, 0, 0);
                    fb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Target = fb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _editor.Children.Add(fb);
                    break;
            }
        }

        // ================= LOOK PAGE =================

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
                DebouncedRebuild();
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
                DebouncedRebuild();
                Pulse();
            };
            stack.Children.Add(Setting("Сила свечения", null, AttachSlider(glowSlider, glowVal)));
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

            var tgLabels = new GlassToggle(cfgApp.ShowLabels);
            tgLabels.Changed += delegate { App.Config.ShowLabels = tgLabels.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            stack.Children.Add(Setting("Показывать названия", "Подписи на лепестках", tgLabels));
            stack.Children.Add(Sep());

            var tgLight = new GlassToggle(cfgApp.LightGlass);
            tgLight.Changed += delegate { App.Config.LightGlass = tgLight.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            stack.Children.Add(Setting("Светлое стекло", "Белые лепестки с тёмными иконками", tgLight));

            stack.Children.Add(Section("Центр"));

            var cVal = Glass.Label(cfgApp.CenterOpacity + "%", 13, true);
            var cSlider = new GlassSlider(30, 100, cfgApp.CenterOpacity);
            cSlider.Changed += delegate
            {
                App.Config.CenterOpacity = (int)Math.Round(cSlider.Value);
                cVal.Text = App.Config.CenterOpacity + "%";
                App.SaveConfig();
                DebouncedRebuild();
                Pulse();
            };
            stack.Children.Add(Setting("Прозрачность шестерёнки", null, AttachSlider(cSlider, cVal)));
            stack.Children.Add(Sep());

            var tgCenter = new GlassToggle(cfgApp.ShowCenterButton);
            tgCenter.Changed += delegate { App.Config.ShowCenterButton = tgCenter.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            stack.Children.Add(Setting("Шестерёнка в центре", "Кнопка настроек в середине цветка", tgCenter));

            stack.Children.Add(Section("Анимации"));

            var spVal = Glass.Label(cfgApp.AnimSpeed + "%", 13, true);
            var spSlider = new GlassSlider(50, 200, cfgApp.AnimSpeed);
            spSlider.Changed += delegate
            {
                App.Config.AnimSpeed = (int)Math.Round(spSlider.Value);
                spVal.Text = App.Config.AnimSpeed + "%";
                App.SaveConfig(); Pulse();
            };
            stack.Children.Add(Setting("Скорость анимаций", null, AttachSlider(spSlider, spVal)));
            stack.Children.Add(Sep());

            var entranceItems = new List<string> { "Пружина", "Плавно", "Эластик", "Отскок", "Волна", "Fade", "Мгновенно" };
            var entranceMap = new[] { "spring", "smooth", "elastic", "bounce", "ripple", "fade", "instant" };
            int curEntr = Array.IndexOf(entranceMap, cfgApp.EntranceStyle);
            var entranceDrop = new GlassDropDown(entranceItems, curEntr < 0 ? 0 : curEntr);
            entranceDrop.MinWidth = 180;
            entranceDrop.Changed += delegate
            {
                App.Config.EntranceStyle = entranceMap[entranceDrop.SelectedIndex];
                App.SaveConfig(); Pulse();
            };
            stack.Children.Add(Setting("Появление меню", "Как раскрывается цветок", entranceDrop));

            stack.Children.Add(Section("Окно настроек"));

            var tgDeep = new GlassToggle(cfgApp.DeepSettingsTheme);
            tgDeep.Changed += delegate
            {
                App.Config.DeepSettingsTheme = tgDeep.IsOn;
                App.SaveConfig();
                _chrome.Background = MakeChromeFill();
                Pulse();
            };
            stack.Children.Add(Setting("Глубокая тема", "Ещё более тёмный фон", tgDeep));

            return WrapPage(stack);
        }

        Brush MakeChromeFill()
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

        // ================= SOUNDS PAGE =================

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

            return WrapPage(stack);
        }

        FrameworkElement PlayBtn(string sound)
        {
            var b = new Border();
            b.Width = 28; b.Height = 28;
            b.CornerRadius = new CornerRadius(14);
            b.Background = Ui.Solid(Colors.White, 0.05);
            b.Cursor = Cursors.Hand;
            b.Margin = new Thickness(10, 0, 0, 0);
            var t = new TextBlock();
            t.Text = ((char)0xE768).ToString();
            t.FontFamily = Ui.Icons;
            t.FontSize = 10;
            t.Foreground = Ui.Solid(Ui.Ink, 0.8);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            b.MouseEnter += delegate { b.Background = Ui.Solid(Ui.Accent, 0.15); };
            b.MouseLeave += delegate { b.Background = Ui.Solid(Colors.White, 0.05); };
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

        // ================= BEHAVIOR PAGE =================

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

            var tgRight = new GlassToggle(cfgApp.CloseOnRightClick);
            tgRight.Changed += delegate { App.Config.CloseOnRightClick = tgRight.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Закрывать правой кнопкой", "ПКМ по стеклу тоже сворачивает меню", tgRight));
            stack.Children.Add(Sep());

            var tgConfirm = new GlassToggle(cfgApp.ConfirmPower);
            tgConfirm.Changed += delegate { App.Config.ConfirmPower = tgConfirm.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Подтверждать выключение", "Стеклянный диалог перед сном, перезагрузкой и выключением", tgConfirm));
            stack.Children.Add(Sep());

            var tgNotif = new GlassToggle(cfgApp.ShowNotifications);
            tgNotif.Changed += delegate { App.Config.ShowNotifications = tgNotif.IsOn; App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Уведомления", "Всплывающие подсказки у часов", tgNotif));
            stack.Children.Add(Sep());

            var tgAuto = new GlassToggle(cfgApp.Autostart);
            tgAuto.Changed += delegate { App.Config.Autostart = tgAuto.IsOn; App.ApplyAutostart(); App.SaveConfig(); Pulse(); };
            stack.Children.Add(Setting("Запускать вместе с Windows", "Hinoki будет ждать вас в трее", tgAuto));

            stack.Children.Add(Section("Глобальный хоткей"));

            var hk = new HotkeyBox(cfgApp.GlobalHotkey);
            hk.Changed += delegate
            {
                App.Config.GlobalHotkey = hk.Combo;
                App.SaveConfig();
                App.ApplyGlobalHotkey();
                Pulse();
            };
            stack.Children.Add(Setting("Хоткей открытия меню", "Клавиши вызывают цветок без мыши", hk));

            var testRow = new StackPanel();
            testRow.Orientation = Orientation.Horizontal;
            testRow.Margin = new Thickness(2, 16, 0, 0);
            var testBtn = new PillButton("Открыть меню", false);
            testBtn.Clicked += delegate { App.TestMenu(); };
            testRow.Children.Add(testBtn);
            var restart = new PillButton("Перезапуск", false);
            restart.Margin = new Thickness(8, 0, 0, 0);
            restart.Clicked += delegate { App.RestartApp(); };
            testRow.Children.Add(restart);
            stack.Children.Add(testRow);

            return WrapPage(stack);
        }

        // ================= ABOUT PAGE =================

        FrameworkElement BuildAboutPage()
        {
            var stack = new StackPanel();

            var logoWrap = new Border();
            logoWrap.Width = 68; logoWrap.Height = 68;
            logoWrap.CornerRadius = new CornerRadius(20);
            var logoImg = new Image { Source = FlowerMenu.LogoBitmap, Width = 68, Height = 68 };
            RenderOptions.SetBitmapScalingMode(logoImg, BitmapScalingMode.Fant);
            logoWrap.Child = logoImg;
            logoWrap.HorizontalAlignment = HorizontalAlignment.Center;
            logoWrap.Effect = new DropShadowEffect { Color = Ui.Accent, BlurRadius = 22, ShadowDepth = 0, Opacity = 0.4 };
            stack.Children.Add(logoWrap);

            var name = Glass.Label("Hinoki", 22, false, true);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.Margin = new Thickness(0, 14, 0, 0);
            stack.Children.Add(name);

            var ver = Glass.Label("Версия 1.2 · Windows 10/11", 12, true);
            ver.HorizontalAlignment = HorizontalAlignment.Center;
            ver.Margin = new Thickness(0, 3, 0, 0);
            stack.Children.Add(ver);

            var desc = Glass.Label("Стеклянный цветок по средней кнопке мыши. Микрофон, звук, скриншоты, приложения — всё под рукой.", 12.5, true);
            desc.TextWrapping = TextWrapping.Wrap;
            desc.TextAlignment = TextAlignment.Center;
            desc.LineHeight = 18;
            desc.Margin = new Thickness(30, 12, 30, 0);
            stack.Children.Add(desc);

            var gh = new PillButton("GitHub", false);
            gh.HorizontalAlignment = HorizontalAlignment.Center;
            gh.Margin = new Thickness(0, 18, 0, 0);
            gh.Clicked += delegate { ActionEngine.OpenLink("https://github.com/globiwarms/hinoki"); };
            stack.Children.Add(gh);

            var reset = new PillButton("Сбросить все настройки", false);
            reset.HorizontalAlignment = HorizontalAlignment.Center;
            reset.Margin = new Thickness(0, 12, 0, 0);
            reset.Clicked += delegate { App.ResetConfig(this); };
            stack.Children.Add(reset);

            return stack;
        }

        // ================= SHARED =================

        FrameworkElement WrapPage(FrameworkElement content)
        {
            return Scroller(content);
        }

        void DebouncedRebuild()
        {
            if (_rebuildDebounce == null)
            {
                _rebuildDebounce = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
                _rebuildDebounce.Tick += delegate
                {
                    _rebuildDebounce.Stop();
                    _preview.Rebuild();
                    _preview.SetSelected(_selPetal);
                };
            }
            _rebuildDebounce.Stop();
            _rebuildDebounce.Start();
        }
    }
}
