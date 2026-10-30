// Hinoki — minimal glass settings: side rail + focused pages, everything applies live
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

        // ---- state ----
        int _selPetal = 0;
        StackPanel _petalList;
        StackPanel _editorHost;
        Border _chrome;
        TextBlock _status;
        FlowerMenu _preview;
        List<Border> _railItems = new List<Border>();
        Dictionary<string, FrameworkElement> _pages = new Dictionary<string, FrameworkElement>();
        string _page = "petals";

        public SettingsWindow()
        {
            Title = "Hinoki";
            Width = 960; Height = 620;
            MinWidth = 860; MinHeight = 540;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanResize;
            Left = Math.Max(0, (SystemParameters.WorkArea.Width - Width) / 2 + SystemParameters.WorkArea.Left);
            Top = Math.Max(0, (SystemParameters.WorkArea.Height - Height) / 2 + SystemParameters.WorkArea.Top);

            var root = new Grid();
            root.Margin = new Thickness(14);

            _chrome = new Border();
            _chrome.CornerRadius = new CornerRadius(20);
            _chrome.Background = Ui.Solid(Color.FromRgb(0x11, 0x12, 0x16), 0.86);
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.13);
            _chrome.BorderThickness = new Thickness(1);
            _chrome.ClipToBounds = true;
            var shadow = new DropShadowEffect();
            shadow.Color = Colors.Black; shadow.BlurRadius = 48; shadow.ShadowDepth = 10; shadow.Opacity = 0.55;
            _chrome.Effect = shadow;

            var blob1 = Blob(Ui.Accent, 240, 0.16);
            blob1.HorizontalAlignment = HorizontalAlignment.Left;
            blob1.VerticalAlignment = VerticalAlignment.Top;
            blob1.Margin = new Thickness(-70, -50, 0, 0);
            var blob2 = Blob(Color.FromRgb(0x6C, 0x7F, 0xA8), 280, 0.13);
            blob2.HorizontalAlignment = HorizontalAlignment.Right;
            blob2.VerticalAlignment = VerticalAlignment.Bottom;
            blob2.Margin = new Thickness(0, 0, -90, -70);

            var layout = new Grid();
            layout.Children.Add(blob1);
            layout.Children.Add(blob2);
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(196) });
            layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ---------- rail ----------
            var rail = new Grid();
            rail.Margin = new Thickness(14, 14, 4, 14);
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rail.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rail.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var brand = new StackPanel();
            brand.Orientation = Orientation.Horizontal;
            brand.Margin = new Thickness(6, 4, 0, 0);
            var logo = new Border();
            logo.Width = 30; logo.Height = 30;
            logo.CornerRadius = new CornerRadius(9);
            logo.Child = new Image { Source = FlowerMenu.LogoBitmap, Width = 30, Height = 30 };
            logo.Effect = new DropShadowEffect { Color = Ui.Accent, BlurRadius = 16, ShadowDepth = 0, Opacity = 0.5 };
            brand.Children.Add(logo);
            var bn = new StackPanel();
            bn.VerticalAlignment = VerticalAlignment.Center;
            bn.Margin = new Thickness(10, 0, 0, 0);
            bn.Children.Add(Glass.Label("Hinoki", 15.5, false, true));
            bn.Children.Add(Glass.Label("v1.1.0", 10, true));
            brand.Children.Add(bn);
            Grid.SetRow(brand, 0);
            rail.Children.Add(brand);

            var nav = new StackPanel();
            nav.Margin = new Thickness(0, 20, 0, 0);
            AddRailItem(nav, 0xE713, "Лепестки", "petals");
            AddRailItem(nav, 0xE790, "Внешний вид", "look");
            AddRailItem(nav, 0xE767, "Звуки", "sounds");
            AddRailItem(nav, 0xE70F, "Поведение", "behavior");
            AddRailItem(nav, 0xE946, "О программе", "about");
            Grid.SetRow(nav, 1);
            rail.Children.Add(nav);

            _status = Glass.Label("", 11.5, true);
            _status.Opacity = 0;
            _status.Margin = new Thickness(8, 0, 0, 4);
            Grid.SetRow(_status, 3);
            rail.Children.Add(_status);

            Grid.SetColumn(rail, 0);
            layout.Children.Add(rail);

            // ---------- content ----------
            var contentHost = new Grid();
            contentHost.Margin = new Thickness(6, 14, 14, 14);
            _pages["petals"] = BuildPetalsPage();
            _pages["look"] = BuildLookPage();
            _pages["sounds"] = BuildSoundsPage();
            _pages["behavior"] = BuildBehaviorPage();
            _pages["about"] = BuildAboutPage();
            foreach (var kv in _pages)
            {
                kv.Value.Visibility = kv.Key == "petals" ? Visibility.Visible : Visibility.Collapsed;
                contentHost.Children.Add(kv.Value);
            }
            Grid.SetColumn(contentHost, 1);
            layout.Children.Add(contentHost);

            _chrome.Child = layout;
            root.Children.Add(_chrome);
            Content = root;

            Loaded += delegate
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                Native.RoundCorners(hwnd);
                if (App.Config.BlurBackground)
                    Native.ApplyAcrylic(hwnd, 0xE0, 0x10, 0x11, 0x15);
                Opacity = 0;
                var a = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(240)));
                a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                BeginAnimation(OpacityProperty, a);
                var st = new ScaleTransform(0.96, 0.96);
                _chrome.RenderTransformOrigin = new Point(0.5, 0.5);
                _chrome.RenderTransform = st;
                var sx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(300)));
                sx.EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut };
                var sy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(300)));
                sy.EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            };
            Closed += delegate
            {
                _instance = null;
                MouseHook.Suppressed = false;
            };
            MouseLeftButtonDown += delegate
            {
                if (Mouse.GetPosition(this).Y < 22) DragMove();
            };
        }

        Ellipse Blob(Color c, double d, double op)
        {
            var e = new Ellipse();
            e.Width = d; e.Height = d;
            e.Fill = Ui.Solid(c, op);
            e.Effect = new BlurEffect { Radius = 70 };
            e.IsHitTestVisible = false;
            return e;
        }

        void AddRailItem(StackPanel nav, int glyph, string label, string key)
        {
            var item = new Border();
            item.CornerRadius = new CornerRadius(11);
            item.Padding = new Thickness(11, 8, 11, 9);
            item.Margin = new Thickness(0, 2, 0, 2);
            item.Cursor = Cursors.Hand;
            item.Background = key == "petals" ? Ui.Solid(Colors.White, 0.10) : Brushes.Transparent;

            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            var ic = new TextBlock();
            ic.Text = ((char)glyph).ToString();
            ic.FontFamily = Ui.Icons;
            ic.FontSize = 14.5;
            ic.Foreground = key == "petals" ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
            ic.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(ic);
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
            _railItems.Add(item);
        }

        void ShowPage(string key)
        {
            _page = key;
            foreach (var kv in _pages)
                kv.Value.Visibility = kv.Key == key ? Visibility.Visible : Visibility.Collapsed;
            for (int i = 0; i < _railItems.Count; i++)
            {
                var it = _railItems[i];
                string itemKey = i == 0 ? "petals" : i == 1 ? "look" : i == 2 ? "sounds" : i == 3 ? "behavior" : "about";
                bool active = itemKey == key;
                it.Background = active ? Ui.Solid(Colors.White, 0.10) : Brushes.Transparent;
                var ic = (TextBlock)((StackPanel)it.Child).Children[0];
                ic.Foreground = active ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
            }
        }

        ScrollViewer Scroller(FrameworkElement content)
        {
            var sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.Content = content;
            return sv;
        }

        FrameworkElement Page(string title, FrameworkElement content)
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var t = Glass.Label(title, 21, false, true);
            t.Margin = new Thickness(2, 0, 0, 14);
            Grid.SetRow(t, 0);
            g.Children.Add(t);
            Grid.SetRow(content, 1);
            g.Children.Add(content);
            return g;
        }

        FrameworkElement HintCard(string text)
        {
            var b = new Border();
            b.CornerRadius = new CornerRadius(12);
            b.Background = Ui.Solid(Ui.Accent, 0.09);
            b.BorderBrush = Ui.Solid(Ui.Accent, 0.25);
            b.BorderThickness = new Thickness(1);
            b.Padding = new Thickness(12, 9, 12, 10);
            b.Margin = new Thickness(0, 12, 0, 0);
            var t = Glass.Label(text, 12, false);
            t.Foreground = Ui.Solid(Ui.Accent, 1);
            t.TextWrapping = TextWrapping.Wrap;
            t.LineHeight = 17;
            b.Child = t;
            return b;
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

        // ================= PETALS =================

        FrameworkElement BuildPetalsPage()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(236) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _petalList = new StackPanel();
            var listCard = new Border();
            listCard.CornerRadius = new CornerRadius(15);
            listCard.Background = Ui.Solid(Colors.White, 0.04);
            listCard.BorderBrush = Ui.Solid(Colors.White, 0.08);
            listCard.BorderThickness = new Thickness(1);
            listCard.Padding = new Thickness(7);
            listCard.Child = _petalList;
            Grid.SetRow(listCard, 0);
            left.Children.Add(listCard);

            var previewWrap = new Border();
            previewWrap.CornerRadius = new CornerRadius(15);
            previewWrap.Background = Ui.Solid(Colors.White, 0.03);
            previewWrap.BorderBrush = Ui.Solid(Colors.White, 0.07);
            previewWrap.BorderThickness = new Thickness(1);
            previewWrap.Margin = new Thickness(0, 12, 0, 0);
            var vb = new Viewbox();
            vb.Stretch = Stretch.Uniform;
            vb.Margin = new Thickness(8, 4, 8, 8);
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
            previewWrap.Child = vb;
            Grid.SetRow(previewWrap, 1);
            left.Children.Add(previewWrap);

            var btnRow = new StackPanel();
            btnRow.Orientation = Orientation.Horizontal;
            btnRow.Margin = new Thickness(0, 12, 0, 0);
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

            _editorHost = new StackPanel();
            var scroll = Scroller(_editorHost);
            Grid.SetColumn(scroll, 2);
            grid.Children.Add(scroll);

            RefreshPetalList();
            FillEditor();
            return Page("Лепестки", grid);
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
                row.Padding = new Thickness(9, 7, 9, 8);
                row.Margin = new Thickness(0, 2, 0, 2);
                row.Cursor = Cursors.Hand;
                bool sel = idx == _selPetal;
                row.Background = sel ? Ui.Solid(Ui.Accent, 0.16) : Brushes.Transparent;

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var ic = new TextBlock();
                ic.Text = Ui.Glyph(p.Icon).ToString();
                ic.FontFamily = Ui.Icons;
                ic.FontSize = 14;
                ic.Foreground = sel ? Ui.Solid(Ui.Accent, 1) : Ui.Solid(Ui.InkDim, 1);
                ic.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(ic, 0);
                g.Children.Add(ic);
                var t = Glass.Label(string.IsNullOrEmpty(p.Label) ? "Лепесток " + (i + 1) : p.Label, 12.5, false, sel);
                t.Margin = new Thickness(9, 0, 0, 0);
                t.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(t, 1);
                g.Children.Add(t);
                row.Child = g;

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
            _editorHost.Children.Clear();

            var title = Glass.Label("Лепесток " + (_selPetal + 1), 17, false, true);
            _editorHost.Children.Add(title);

            _editorHost.Children.Add(SubHeader("Название"));
            var nameBox = new GlassTextBox(p.Label, "Например: Мут микро", 240);
            nameBox.Margin = new Thickness(0, 8, 0, 4);
            nameBox.Box.TextChanged += delegate
            {
                App.Config.Petals[_selPetal].Label = nameBox.Value;
                App.SaveConfig();
                _preview.UpdatePetal(_selPetal);
                RefreshPetalList();
                Pulse();
            };
            _editorHost.Children.Add(nameBox);

            _editorHost.Children.Add(SubHeader("Иконка"));
            var iconPanel = new WrapPanel();
            iconPanel.Margin = new Thickness(0, 8, 0, 6);
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
            _editorHost.Children.Add(iconPanel);

            _editorHost.Children.Add(SubHeader("Действие"));
            var actionItems = new List<string>(Actions.Names);
            var drop = new GlassDropDown(actionItems, Math.Max(0, Array.IndexOf(Actions.Keys, p.Action)));
            drop.Margin = new Thickness(0, 8, 0, 6);
            drop.Changed += delegate
            {
                App.Config.Petals[_selPetal].Action = Actions.Keys[drop.SelectedIndex];
                App.SaveConfig();
                _preview.UpdatePetal(_selPetal);
                RefreshPetalList();
                FillActionFields();
                Pulse();
            };
            _editorHost.Children.Add(drop);

            FillActionFields();
        }

        TextBlock SubHeader(string s)
        {
            var t = Glass.Label(s, 12, true);
            t.Margin = new Thickness(0, 14, 0, 0);
            return t;
        }

        void FillActionFields()
        {
            var p = App.Config.Petals[_selPetal];
            int dropIndex = -1;
            for (int i = _editorHost.Children.Count - 1; i >= 0; i--)
            {
                if (_editorHost.Children[i] is GlassDropDown) { dropIndex = i; break; }
            }
            if (dropIndex < 0) return;
            for (int i = _editorHost.Children.Count - 1; i > dropIndex; i--)
                _editorHost.Children.RemoveAt(i);

            switch (p.Action)
            {
                case "app":
                {
                    _editorHost.Children.Add(SubHeader("Путь / команда"));
                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var tb = new GlassTextBox(p.Target, @"C:\...\app.exe или steam://open/games", 220);
                    tb.Margin = new Thickness(0, 8, 8, 0);
                    tb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Target = tb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    Grid.SetColumn(tb, 0);
                    row.Children.Add(tb);
                    var browse = new PillButton("Обзор…", false);
                    browse.Margin = new Thickness(0, 8, 0, 0);
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
                    _editorHost.Children.Add(row);

                    _editorHost.Children.Add(SubHeader("Аргументы запуска"));
                    var args = new GlassTextBox(p.Args, "--fullscreen", 220);
                    args.Margin = new Thickness(0, 8, 0, 0);
                    args.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Args = args.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _editorHost.Children.Add(args);
                    _editorHost.Children.Add(HintCard("Поддерживаются steam:// ссылки, .exe, .lnk — откроется как обычный ярлык."));
                    break;
                }
                case "url":
                {
                    _editorHost.Children.Add(SubHeader("Ссылка"));
                    var tb = new GlassTextBox(p.Target, "youtube.com", 220);
                    tb.Margin = new Thickness(0, 8, 0, 0);
                    tb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_selPetal].Target = tb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _editorHost.Children.Add(tb);
                    _editorHost.Children.Add(HintCard("Откроется в браузере по умолчанию."));
                    break;
                }
                case "hotkey":
                {
                    _editorHost.Children.Add(SubHeader("Сочетание клавиш"));
                    var hk = new HotkeyBox(p.Target);
                    hk.Margin = new Thickness(0, 8, 0, 0);
                    hk.Changed += delegate
                    {
                        App.Config.Petals[_selPetal].Target = hk.Combo;
                        App.SaveConfig(); Pulse();
                    };
                    _editorHost.Children.Add(hk);
                    _editorHost.Children.Add(HintCard("Hinoki нажмёт это сочетание за вас в активном приложении."));
                    break;
                }
                case "discord_mute":
                    _editorHost.Children.Add(HintCard("Активирует Discord и отправляет «" + HotkeyBox.PrettyCombo(App.Config.DiscordMuteKeys) + "». Сочетание — во вкладке «Поведение»."));
                    break;
                case "discord_deafen":
                    _editorHost.Children.Add(HintCard("Активирует Discord и отправляет «" + HotkeyBox.PrettyCombo(App.Config.DiscordDeafenKeys) + "». Сочетание — во вкладке «Поведение»."));
                    break;
                case "media":
                    _editorHost.Children.Add(HintCard("Play / Pause для Spotify, YouTube Music и других плееров."));
                    break;
                case "screenshot_file":
                    _editorHost.Children.Add(HintCard("Снимок всего экрана сохраняется в «Изображения»."));
                    break;
                case "screenshot_buffer":
                    _editorHost.Children.Add(HintCard("Запускает системные «Ножницы» (Win+Shift+S) — выделите область."));
                    break;
                case "shutdown_pc":
                case "restart_pc":
                case "sleep_pc":
                    _editorHost.Children.Add(HintCard("Действие сработает сразу — без подтверждения."));
                    break;
                case "open_steam":
                    _editorHost.Children.Add(HintCard("Открывает библиотеку Steam. Если Steam не установлен — ничего не произойдёт."));
                    break;
                default:
                    _editorHost.Children.Add(HintCard("Выберите действие — лепесток сделает его одним нажатием."));
                    break;
            }
        }

        // ================= LOOK =================

        FrameworkElement BuildLookPage()
        {
            var stack = new StackPanel();
            var cfgApp = App.Config;

            stack.Children.Add(SubHeader("Акцентный цвет"));
            var swatches = new StackPanel();
            swatches.Orientation = Orientation.Horizontal;
            swatches.Margin = new Thickness(0, 8, 0, 4);
            string[] palette = { "A8B899", "7FD8BE", "A8B4E8", "E8B4A0", "E89E9E", "9ED0F0", "E8D5A0", "C9A0E8" };
            foreach (var hex in palette)
            {
                var sw = new Border();
                sw.Width = 34; sw.Height = 34;
                sw.CornerRadius = new CornerRadius(17);
                sw.Margin = new Thickness(0, 0, 10, 0);
                sw.Cursor = Cursors.Hand;
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                sw.Background = Ui.Solid(Color.FromRgb(r, g, b), 1);
                bool cur = cfgApp.AccentHex.Equals(hex, StringComparison.OrdinalIgnoreCase);
                sw.BorderBrush = cur ? Ui.Solid(Colors.White, 0.95) : Ui.Solid(Colors.White, 0.15);
                sw.BorderThickness = new Thickness(cur ? 3 : 1.5);
                var hexCopy = hex;
                sw.MouseLeftButtonDown += delegate
                {
                    App.Config.AccentHex = hexCopy;
                    App.SaveConfig();
                    RebuildAll();
                    Pulse();
                };
                swatches.Children.Add(sw);
            }
            stack.Children.Add(swatches);
            stack.Children.Add(HintCard("Акцент подсвечивает выбор, тогглы и рамки — как в macOS."));

            var card1 = Glass.Card(16, 16, 0.08, 0.03);
            card1.Margin = new Thickness(0, 14, 0, 0);
            var s1 = new StackPanel();

            var opRow = new Grid();
            opRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            opRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            opRow.Children.Add(Glass.Label("Прозрачность лепестков", 13.5, false, true));
            var opVal = Glass.Label(cfgApp.PetalOpacity + "%", 13, true);
            Grid.SetColumn(opVal, 1);
            opRow.Children.Add(opVal);
            s1.Children.Add(opRow);
            var opSlider = new GlassSlider(30, 100, cfgApp.PetalOpacity);
            opSlider.Margin = new Thickness(0, 8, 0, 2);
            opSlider.Changed += delegate
            {
                App.Config.PetalOpacity = (int)Math.Round(opSlider.Value);
                opVal.Text = App.Config.PetalOpacity + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();
            };
            s1.Children.Add(opSlider);

            var glowRow = new Grid();
            glowRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            glowRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            glowRow.Children.Add(Glass.Label("Сила свечения", 13.5, false, true));
            var glowVal = Glass.Label(cfgApp.GlowStrength + "%", 13, true);
            Grid.SetColumn(glowVal, 1);
            glowRow.Children.Add(glowVal);
            s1.Children.Add(glowRow);
            var glowSlider = new GlassSlider(0, 100, cfgApp.GlowStrength);
            glowSlider.Margin = new Thickness(0, 8, 0, 2);
            glowSlider.Changed += delegate
            {
                App.Config.GlowStrength = (int)Math.Round(glowSlider.Value);
                glowVal.Text = App.Config.GlowStrength + "%";
                App.SaveConfig();
                _preview.Rebuild();
                Pulse();
            };
            s1.Children.Add(glowSlider);

            var sizeRow = new Grid();
            sizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            sizeRow.Children.Add(Glass.Label("Размер меню", 13.5, false, true));
            var sizeVal = Glass.Label(cfgApp.MenuSizePercent + "%", 13, true);
            Grid.SetColumn(sizeVal, 1);
            sizeRow.Children.Add(sizeVal);
            s1.Children.Add(sizeRow);
            var sizeSlider = new GlassSlider(50, 100, cfgApp.MenuSizePercent);
            sizeSlider.Margin = new Thickness(0, 8, 0, 2);
            sizeSlider.Changed += delegate
            {
                App.Config.MenuSizePercent = (int)Math.Round(sizeSlider.Value);
                sizeVal.Text = App.Config.MenuSizePercent + "%";
                App.SaveConfig(); Pulse();
            };
            s1.Children.Add(sizeSlider);

            card1.Child = s1;
            stack.Children.Add(card1);

            var card2 = Glass.Card(16, 16, 0.08, 0.03);
            card2.Margin = new Thickness(0, 12, 0, 0);
            var s2 = new StackPanel();

            var tgLight = new GlassToggle(cfgApp.LightGlass);
            tgLight.Changed += delegate { App.Config.LightGlass = tgLight.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            s2.Children.Add(Rows.Row("Светлое стекло", "Белые лепестки с тёмными иконками", tgLight));

            var tgLabels = new GlassToggle(cfgApp.ShowLabels);
            tgLabels.Changed += delegate { App.Config.ShowLabels = tgLabels.IsOn; App.SaveConfig(); _preview.Rebuild(); Pulse(); };
            s2.Children.Add(Rows.Row("Показывать названия", "Подписи на лепестках и под кнопкой настроек", tgLabels));

            var tgBlur = new GlassToggle(cfgApp.BlurBackground);
            tgBlur.Changed += delegate { App.Config.BlurBackground = tgBlur.IsOn; App.SaveConfig(); Pulse(); };
            s2.Children.Add(Rows.Row("Размытие окна настроек", "Acrylic-фон этого окна (выключите, если артефакты)", tgBlur, false));

            card2.Child = s2;
            stack.Children.Add(card2);

            return Page("Внешний вид", Scroller(stack));
        }

        // ================= SOUNDS =================

        FrameworkElement BuildSoundsPage()
        {
            var stack = new StackPanel();
            var cfgApp = App.Config;

            var card = Glass.Card(16, 16, 0.08, 0.03);
            var s1 = new StackPanel();

            var tgAll = new GlassToggle(cfgApp.SoundEnabled);
            tgAll.Changed += delegate { App.Config.SoundEnabled = tgAll.IsOn; App.SaveConfig(); Pulse(); };
            s1.Children.Add(Rows.Row("Звуки", "Мягкие стеклянные щелчки при работе с меню", tgAll));

            var volRow = new Grid();
            volRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            volRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            volRow.Children.Add(Glass.Label("Громкость", 13.5, false, true));
            var volVal = Glass.Label(cfgApp.SoundVolume + "%", 13, true);
            Grid.SetColumn(volVal, 1);
            volRow.Children.Add(volVal);
            s1.Children.Add(volRow);
            var volSlider = new GlassSlider(0, 100, cfgApp.SoundVolume);
            volSlider.Margin = new Thickness(0, 8, 0, 2);
            volSlider.Changed += delegate
            {
                App.Config.SoundVolume = (int)Math.Round(volSlider.Value);
                volVal.Text = App.Config.SoundVolume + "%";
                App.SaveConfig(); Pulse();
            };
            s1.Children.Add(volSlider);

            var tgOpen = new GlassToggle(cfgApp.SoundOpen);
            tgOpen.Changed += delegate { App.Config.SoundOpen = tgOpen.IsOn; App.SaveConfig(); Pulse(); };
            s1.Children.Add(Rows.Row("Раскрытие меню", "Мягкий двухтональный «блум»", tgOpen));

            var tgClose = new GlassToggle(cfgApp.SoundClose);
            tgClose.Changed += delegate { App.Config.SoundClose = tgClose.IsOn; App.SaveConfig(); Pulse(); };
            s1.Children.Add(Rows.Row("Закрытие меню", "Обратный тон при сворачивании", tgClose));

            var tgClick = new GlassToggle(cfgApp.SoundClick);
            tgClick.Changed += delegate { App.Config.SoundClick = tgClick.IsOn; App.SaveConfig(); Pulse(); };
            s1.Children.Add(Rows.Row("Выбор лепестка", "Короткий тик при нажатии", tgClick, false));

            card.Child = s1;
            stack.Children.Add(card);

            var test = new PillButton("Прослушать", false);
            test.Margin = new Thickness(0, 12, 0, 0);
            test.HorizontalAlignment = HorizontalAlignment.Left;
            test.Clicked += delegate { Sounds.Play("open"); };
            stack.Children.Add(test);

            return Page("Звуки", Scroller(stack));
        }

        // ================= BEHAVIOR =================

        FrameworkElement BuildBehaviorPage()
        {
            var stack = new StackPanel();
            var cfgApp = App.Config;

            var card1 = Glass.Card(16, 16, 0.08, 0.03);
            var s1 = new StackPanel();

            var tgIntercept = new GlassToggle(cfgApp.InterceptClick);
            tgIntercept.Changed += delegate { App.Config.InterceptClick = tgIntercept.IsOn; MouseHook.Intercept = tgIntercept.IsOn; App.SaveConfig(); Pulse(); };
            s1.Children.Add(Rows.Row("Перехватывать среднюю кнопку", "Колёсико открывает Hinoki вместо прокрутки", tgIntercept));

            var tgCursor = new GlassToggle(cfgApp.ShowAtCursor);
            tgCursor.Changed += delegate { App.Config.ShowAtCursor = tgCursor.IsOn; App.SaveConfig(); Pulse(); };
            s1.Children.Add(Rows.Row("Меню у курсора", "Цветок появляется там, где сейчас курсор", tgCursor));

            var tgClose = new GlassToggle(cfgApp.CloseAfterAction);
            tgClose.Changed += delegate { App.Config.CloseAfterAction = tgClose.IsOn; App.SaveConfig(); Pulse(); };
            s1.Children.Add(Rows.Row("Закрывать после действия", "Меню сворачивается сразу после нажатия лепестка", tgClose, false));

            card1.Child = s1;
            stack.Children.Add(card1);

            var card2 = Glass.Card(16, 16, 0.08, 0.03);
            card2.Margin = new Thickness(0, 12, 0, 0);
            var s2 = new StackPanel();

            var hk = new HotkeyBox(cfgApp.GlobalHotkey);
            hk.Changed += delegate
            {
                App.Config.GlobalHotkey = hk.Combo;
                App.SaveConfig();
                App.ApplyGlobalHotkey();
                Pulse();
            };
            s2.Children.Add(Rows.Row("Хоткей открытия меню", "Клавиши вызывают цветок без мыши. Пустое поле — выключено", hk));

            var hkMute = new HotkeyBox(cfgApp.DiscordMuteKeys);
            hkMute.Changed += delegate { App.Config.DiscordMuteKeys = hkMute.Combo; App.SaveConfig(); Pulse(); };
            s2.Children.Add(Rows.Row("Мут микрофона — Discord", "Должно совпадать с биндом в настройках Discord", hkMute));

            var hkDeafen = new HotkeyBox(cfgApp.DiscordDeafenKeys);
            hkDeafen.Changed += delegate { App.Config.DiscordDeafenKeys = hkDeafen.Combo; App.SaveConfig(); Pulse(); };
            s2.Children.Add(Rows.Row("Мут звука — Discord", "Должно совпадать с биндом в настройках Discord", hkDeafen, false));

            card2.Child = s2;
            stack.Children.Add(card2);

            var card3 = Glass.Card(16, 16, 0.08, 0.03);
            card3.Margin = new Thickness(0, 12, 0, 0);
            var s3 = new StackPanel();

            var tgAuto = new GlassToggle(cfgApp.Autostart);
            tgAuto.Changed += delegate { App.Config.Autostart = tgAuto.IsOn; App.ApplyAutostart(); App.SaveConfig(); Pulse(); };
            s3.Children.Add(Rows.Row("Запускать вместе с Windows", "Hinoki будет ждать вас в трее", tgAuto));

            var testBtn = new PillButton("Открыть меню (тест)", false);
            testBtn.HorizontalAlignment = HorizontalAlignment.Left;
            testBtn.Margin = new Thickness(0, 4, 0, 0);
            testBtn.Clicked += delegate { App.TestMenu(); };
            s3.Children.Add(testBtn);

            card3.Child = s3;
            stack.Children.Add(card3);

            return Page("Поведение", Scroller(stack));
        }

        // ================= ABOUT =================

        FrameworkElement BuildAboutPage()
        {
            var stack = new StackPanel();

            var hero = Glass.Card(18, 20, 0.09, 0.04);
            var hs = new StackPanel();
            var logoWrap = new Border();
            logoWrap.Width = 72; logoWrap.Height = 72;
            logoWrap.CornerRadius = new CornerRadius(21);
            logoWrap.Child = new Image { Source = FlowerMenu.LogoBitmap, Width = 72, Height = 72 };
            logoWrap.HorizontalAlignment = HorizontalAlignment.Center;
            logoWrap.Effect = new DropShadowEffect { Color = Ui.Accent, BlurRadius = 26, ShadowDepth = 0, Opacity = 0.45 };
            hs.Children.Add(logoWrap);

            var name = Glass.Label("Hinoki", 24, false, true);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.Margin = new Thickness(0, 12, 0, 0);
            hs.Children.Add(name);

            var ver = Glass.Label("Версия 1.1.0 · Windows 10/11", 12, true);
            ver.HorizontalAlignment = HorizontalAlignment.Center;
            ver.Margin = new Thickness(0, 2, 0, 0);
            hs.Children.Add(ver);

            var desc = Glass.Label("Средняя кнопка мыши — и всё под рукой: Discord, музыка, приложения. Стеклянный цветок, который всегда рядом.", 12.5, true);
            desc.TextWrapping = TextWrapping.Wrap;
            desc.TextAlignment = TextAlignment.Center;
            desc.LineHeight = 19;
            desc.Margin = new Thickness(6, 10, 6, 0);
            hs.Children.Add(desc);

            var links = new StackPanel();
            links.Orientation = Orientation.Horizontal;
            links.HorizontalAlignment = HorizontalAlignment.Center;
            links.Margin = new Thickness(0, 14, 0, 0);
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
            hs.Children.Add(links);

            hero.Child = hs;
            stack.Children.Add(hero);

            var reset = new PillButton("Сбросить все настройки", false);
            reset.HorizontalAlignment = HorizontalAlignment.Center;
            reset.Margin = new Thickness(0, 12, 0, 0);
            reset.Clicked += delegate { App.ResetConfig(this); };
            stack.Children.Add(reset);

            return Page("О программе", Scroller(stack));
        }

        void RebuildAll()
        {
            _preview.Rebuild();
            _preview.SetSelected(_selPetal);
            RefreshPetalList();
        }
    }
}
