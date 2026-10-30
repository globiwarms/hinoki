// Hinoki — the settings window: glass, sage accent, flower preview in the middle
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

        FlowerMenu _preview;
        int _sel = 0;

        public static SettingsWindow ActiveWindow { get { return _instance; } }

        /// <summary>Is the settings window under this physical screen point? (middle clicks are swallowed there)</summary>
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

        // petals tab controls
        TextBlock _petalTitle;
        GlassTextBox _nameBox;
        WrapPanel _iconPanel;
        GlassDropDown _actionDrop;
        StackPanel _actionFields;

        // general tab
        GlassToggle _tgIntercept, _tgCursor, _tgBlur, _tgAutostart;
        GlassSlider _sizeSlider;
        TextBlock _sizeValue;
        HotkeyBox _hkMute, _hkDeafen;

        Segmented _tabs;
        Grid _pagePetals, _pageGeneral, _pageAbout;
        TextBlock _status;
        Border _chrome;

        public SettingsWindow()
        {
            Title = "Hinoki";
            Width = 1000; Height = 660;
            MinWidth = 880; MinHeight = 580;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanResize;
            StartupLocationManual();
            WindowStartupLocationSet();

            var root = new Grid();
            root.Margin = new Thickness(16);

            _chrome = new Border();
            _chrome.CornerRadius = new CornerRadius(20);
            _chrome.Background = Ui.Solid(Color.FromRgb(0x11, 0x12, 0x16), 0.86);
            _chrome.BorderBrush = Ui.Solid(Colors.White, 0.13);
            _chrome.BorderThickness = new Thickness(1);
            _chrome.ClipToBounds = true;
            var shadow = new DropShadowEffect();
            shadow.Color = Colors.Black; shadow.BlurRadius = 48; shadow.ShadowDepth = 10; shadow.Opacity = 0.55;
            _chrome.Effect = shadow;

            // decorative accent blobs behind the glass
            var blob1 = Blob(Ui.Accent, 260, 0.20);
            blob1.SetValue(Grid.RowSpanProperty, 3);
            blob1.HorizontalAlignment = HorizontalAlignment.Left;
            blob1.VerticalAlignment = VerticalAlignment.Top;
            blob1.Margin = new Thickness(-80, -60, 0, 0);
            var blob2 = Blob(Color.FromRgb(0x6C, 0x7F, 0xA8), 300, 0.16);
            blob2.SetValue(Grid.RowSpanProperty, 3);
            blob2.HorizontalAlignment = HorizontalAlignment.Right;
            blob2.VerticalAlignment = VerticalAlignment.Bottom;
            blob2.Margin = new Thickness(0, 0, -100, -80);

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });   // titlebar
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // content
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });   // footer

            layout.Children.Add(blob1);
            layout.Children.Add(blob2);
            Grid.SetRowSpan(layout, 1);

            // ----- titlebar -----
            var titlebar = new Grid();
            titlebar.Background = Brushes.Transparent;
            titlebar.MouseLeftButtonDown += delegate
            {
                if (Mouse.GetPosition(this).Y < 58) DragMove();
            };

            var brand = new StackPanel();
            brand.Orientation = Orientation.Horizontal;
            brand.VerticalAlignment = VerticalAlignment.Center;
            brand.Margin = new Thickness(20, 0, 0, 0);
            var logo = new Border();
            logo.Width = 30; logo.Height = 30;
            logo.CornerRadius = new CornerRadius(9);
            logo.Child = new Image { Source = FlowerMenu.LogoBitmap, Width = 30, Height = 30 };
            logo.Effect = new DropShadowEffect { Color = Ui.Accent, BlurRadius = 16, ShadowDepth = 0, Opacity = 0.5 };
            brand.Children.Add(logo);
            var bname = new StackPanel();
            bname.VerticalAlignment = VerticalAlignment.Center;
            bname.Margin = new Thickness(11, 0, 0, 0);
            var bt = Glass.Label("Hinoki", 15, false, true);
            var bv = Glass.Label("v1.0.0", 10.5, true);
            bname.Children.Add(bt);
            bname.Children.Add(bv);
            brand.Children.Add(bname);
            titlebar.Children.Add(brand);

            _tabs = new Segmented(new[] { "Лепестки", "Общие", "О программе" }, 0);
            _tabs.HorizontalAlignment = HorizontalAlignment.Center;
            _tabs.VerticalAlignment = VerticalAlignment.Center;
            _tabs.Changed += delegate(int i) { ShowPage(i); };
            titlebar.Children.Add(_tabs);

            var close = MakeCloseButton();
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Center;
            close.Margin = new Thickness(0, 0, 18, 0);
            titlebar.Children.Add(close);

            Grid.SetRow(titlebar, 0);
            layout.Children.Add(titlebar);

            // ----- content -----
            var content = new Grid();
            content.Margin = new Thickness(18, 4, 18, 0);
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(43, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(57, GridUnitType.Star) });

            // left: preview card
            var previewCard = Glass.Card(18, 14, 0.10, 0.04);
            var pgrid = new Grid();
            pgrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            pgrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var vb = new Viewbox();
            vb.Stretch = Stretch.Uniform;
            vb.Margin = new Thickness(0, 8, 0, 0);
            _preview = new FlowerMenu(560);
            _preview.PetalActivated += delegate(int i)
            {
                _sel = i;
                _preview.SetSelected(_sel);
                FillPetalEditor();
                _preview.ReleasePulseAt(i);
            };
            _preview.CenterActivated += delegate { _preview.PlayIn(); };
            vb.Child = _preview;
            Grid.SetRow(vb, 0);
            pgrid.Children.Add(vb);

            var hint = Glass.Label("Клик по лепестку — настроить его. Клик по центру — предпросмотр анимации.", 11.5, true);
            hint.TextWrapping = TextWrapping.Wrap;
            hint.TextAlignment = TextAlignment.Center;
            hint.Margin = new Thickness(18, 4, 18, 14);
            Grid.SetRow(hint, 1);
            pgrid.Children.Add(hint);

            previewCard.Child = pgrid;
            Grid.SetColumn(previewCard, 0);
            content.Children.Add(previewCard);

            // right: pages
            var right = new Grid();
            right.Margin = new Thickness(14, 0, 0, 0);

            _pagePetals = BuildPetalsPage();
            _pageGeneral = BuildGeneralPage();
            _pageAbout = BuildAboutPage();

            right.Children.Add(_pagePetals);
            right.Children.Add(_pageGeneral);
            right.Children.Add(_pageAbout);
            Grid.SetColumn(right, 1);
            content.Children.Add(right);

            Grid.SetRow(content, 1);
            layout.Children.Add(content);

            // ----- footer -----
            var footer = new Grid();
            footer.Margin = new Thickness(20, 6, 20, 0);
            _status = Glass.Label("", 12, true);
            _status.Opacity = 0;
            _status.VerticalAlignment = VerticalAlignment.Center;
            footer.Children.Add(_status);

            var done = new PillButton("Готово", true);
            done.HorizontalAlignment = HorizontalAlignment.Right;
            done.VerticalAlignment = VerticalAlignment.Center;
            done.Clicked += delegate { Close(); };
            footer.Children.Add(done);

            var note = Glass.Label("Изменения сохраняются автоматически", 11.5, true);
            note.VerticalAlignment = VerticalAlignment.Center;
            note.HorizontalAlignment = HorizontalAlignment.Right;
            note.Margin = new Thickness(0, 0, 96, 0);
            footer.Children.Add(note);

            Grid.SetRow(footer, 2);
            layout.Children.Add(footer);

            _chrome.Child = layout;
            root.Children.Add(_chrome);
            Content = root;

            _preview.SetSelected(0);
            FillPetalEditor();
            ShowPage(0);

            Loaded += delegate
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                Native.RoundCorners(hwnd);
                if (App.Config.BlurBackground)
                    Native.ApplyAcrylic(hwnd, 0xE0, 0x10, 0x11, 0x15);
                // entrance
                Opacity = 0;
                var a = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(220)));
                a.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                BeginAnimation(OpacityProperty, a);
                var st = new ScaleTransform(0.96, 0.96);
                _chrome.RenderTransformOrigin = new Point(0.5, 0.5);
                _chrome.RenderTransform = st;
                var sx = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(260)));
                sx.EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut };
                var sy = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(260)));
                sy.EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            };
            Closed += delegate
            {
                _instance = null;
                MouseHook.Suppressed = false;
            };
        }

        void StartupLocationManual() { WindowStartupLocation = WindowStartupLocation.Manual; }
        void WindowStartupLocationSet()
        {
            // center of primary work area
            Left = Math.Max(0, (SystemParameters.WorkArea.Width - Width) / 2 + SystemParameters.WorkArea.Left);
            Top = Math.Max(0, (SystemParameters.WorkArea.Height - Height) / 2 + SystemParameters.WorkArea.Top);
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

        Border MakeCloseButton()
        {
            var b = new Border();
            b.Width = 32; b.Height = 32;
            b.CornerRadius = new CornerRadius(16);
            b.Background = Ui.Solid(Colors.White, 0.07);
            b.BorderBrush = Ui.Solid(Colors.White, 0.10);
            b.BorderThickness = new Thickness(1);
            b.Cursor = Cursors.Hand;
            var t = new TextBlock();
            t.Text = ((char)0xE8BB).ToString();
            t.FontFamily = Ui.Icons;
            t.FontSize = 12;
            t.Foreground = Ui.Solid(Ui.Ink, 0.9);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;
            b.Child = t;
            b.MouseEnter += delegate { b.Background = Ui.Solid(Ui.Danger, 0.9); t.Foreground = Ui.Solid(Colors.White, 1); };
            b.MouseLeave += delegate { b.Background = Ui.Solid(Colors.White, 0.07); t.Foreground = Ui.Solid(Ui.Ink, 0.9); };
            b.MouseLeftButtonDown += delegate { Close(); };
            return b;
        }

        void ShowPage(int i)
        {
            _pagePetals.Visibility = i == 0 ? Visibility.Visible : Visibility.Collapsed;
            _pageGeneral.Visibility = i == 1 ? Visibility.Visible : Visibility.Collapsed;
            _pageAbout.Visibility = i == 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        ScrollViewer Scroller(FrameworkElement content)
        {
            var sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.Content = content;
            sv.Margin = new Thickness(0, 0, -6, 0);
            return sv;
        }

        // ================= PETALS TAB =================

        Grid BuildPetalsPage()
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var scrollHost = new Grid();
            Grid.SetRow(scrollHost, 1);
            g.Children.Add(scrollHost);

            _petalTitle = Glass.Label("Лепесток 1", 19, false, true);
            _petalTitle.Margin = new Thickness(2, 2, 0, 10);
            g.Children.Add(_petalTitle);
            Grid.SetRow(_petalTitle, 0);

            var stack = new StackPanel();

            // name
            stack.Children.Add(Glass.Label("Название", 12, true));
            _nameBox = new GlassTextBox("", "Например: Мут микро", 240);
            _nameBox.Margin = new Thickness(0, 6, 0, 4);
            _nameBox.Box.TextChanged += delegate
            {
                App.Config.Petals[_sel].Label = _nameBox.Value;
                App.SaveConfig();
                _preview.UpdatePetal(_sel);
                Pulse();
            };
            stack.Children.Add(_nameBox);

            // icons
            stack.Children.Add(IconSectionHeader("Иконка"));
            _iconPanel = new WrapPanel();
            _iconPanel.Margin = new Thickness(0, 8, 0, 6);
            stack.Children.Add(_iconPanel);

            // action
            stack.Children.Add(IconSectionHeader("Действие"));
            var actionItems = new List<string>(Actions.Names);
            _actionDrop = new GlassDropDown(actionItems, 0);
            _actionDrop.Margin = new Thickness(0, 8, 0, 6);
            _actionDrop.Changed += delegate
            {
                App.Config.Petals[_sel].Action = Actions.Keys[_actionDrop.SelectedIndex];
                App.SaveConfig();
                _preview.UpdatePetal(_sel);
                FillActionFields();
                Pulse();
            };
            stack.Children.Add(_actionDrop);

            // dynamic action fields
            _actionFields = new StackPanel();
            stack.Children.Add(_actionFields);

            scrollHost.Children.Add(Scroller(stack));
            return g;
        }

        TextBlock IconSectionHeader(string s)
        {
            var t = Glass.Label(s, 12, true);
            t.Margin = new Thickness(0, 12, 0, 0);
            return t;
        }

        void FillPetalEditor()
        {
            if (_nameBox == null) return;
            var p = App.Config.Petals[_sel];
            _petalTitle.Text = "Лепесток " + (_sel + 1) + " — настройка";
            _nameBox.Value = p.Label;

            // icon tiles
            string[] glyphs = { "E720", "E74F", "E767", "E768", "E769", "E8D6", "E71D", "E71B", "E713", "E8B7", "E765", "E774", "E734", "E72E" };
            _iconPanel.Children.Clear();
            IconTile.ActiveGroup = new List<IconTile>();
            foreach (var gl in glyphs)
            {
                var tile = new IconTile(gl);
                tile.Picked += delegate(string g)
                {
                    App.Config.Petals[_sel].Icon = g;
                    App.SaveConfig();
                    _preview.UpdatePetal(_sel);
                    Pulse();
                };
                IconTile.ActiveGroup.Add(tile);
                _iconPanel.Children.Add(tile);
                if (gl.Equals(p.Icon, StringComparison.OrdinalIgnoreCase)) tile.SetSelected(true);
            }

            int ai = Array.IndexOf(Actions.Keys, p.Action);
            _actionDrop.SelectedIndex = ai < 0 ? 0 : ai;
            FillActionFields();
        }

        void FillActionFields()
        {
            var p = App.Config.Petals[_sel];
            _actionFields.Children.Clear();

            switch (p.Action)
            {
                case "app":
                {
                    _actionFields.Children.Add(IconSectionHeader("Путь / команда"));
                    var row = new Grid();
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var tb = new GlassTextBox(p.Target, @"C:\Program Files\...\app.exe или steam://open/games", 220);
                    tb.Margin = new Thickness(0, 8, 8, 0);
                    tb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_sel].Target = tb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    Grid.SetColumn(tb, 0);
                    row.Children.Add(tb);
                    var browse = new PillButton("Обзор…", false);
                    browse.Margin = new Thickness(0, 8, 0, 0);
                    browse.VerticalAlignment = VerticalAlignment.Stretch;
                    browse.Clicked += delegate
                    {
                        var dlg = new Microsoft.Win32.OpenFileDialog();
                        dlg.Title = "Выберите приложение";
                        dlg.Filter = "Приложения (*.exe;*.lnk;*.url)|*.exe;*.lnk;*.url|Все файлы|*.*";
                        if (dlg.ShowDialog() == true)
                        {
                            tb.Value = dlg.FileName;
                            App.Config.Petals[_sel].Target = dlg.FileName;
                            App.SaveConfig(); Pulse();
                        }
                    };
                    Grid.SetColumn(browse, 1);
                    row.Children.Add(browse);
                    _actionFields.Children.Add(row);

                    var argsHeader = IconSectionHeader("Аргументы запуска (необязательно)");
                    _actionFields.Children.Add(argsHeader);
                    var args = new GlassTextBox(p.Args, "--fullscreen", 220);
                    args.Margin = new Thickness(0, 8, 0, 0);
                    args.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_sel].Args = args.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _actionFields.Children.Add(args);

                    _actionFields.Children.Add(HintCard("Можно указать steam:// ссылку (например steam://open/games) или любой .exe — откроется как есть."));
                    break;
                }
                case "url":
                {
                    _actionFields.Children.Add(IconSectionHeader("Ссылка"));
                    var tb = new GlassTextBox(p.Target, "youtube.com", 220);
                    tb.Margin = new Thickness(0, 8, 0, 0);
                    tb.Box.TextChanged += delegate
                    {
                        App.Config.Petals[_sel].Target = tb.Value;
                        App.SaveConfig(); Pulse();
                    };
                    _actionFields.Children.Add(tb);
                    _actionFields.Children.Add(HintCard("Откроется в браузере по умолчанию."));
                    break;
                }
                case "hotkey":
                {
                    _actionFields.Children.Add(IconSectionHeader("Сочетание клавиш"));
                    var hk = new HotkeyBox(p.Target);
                    hk.Margin = new Thickness(0, 8, 0, 0);
                    hk.Changed += delegate
                    {
                        App.Config.Petals[_sel].Target = hk.Combo;
                        App.SaveConfig(); Pulse();
                    };
                    _actionFields.Children.Add(hk);
                    _actionFields.Children.Add(HintCard("Нажмите на поле и задайте комбинацию — она сработает как нажатие клавиш."));
                    break;
                }
                case "discord_mute":
                    _actionFields.Children.Add(HintCard("Активирует окно Discord и отправляет «" + HotkeyBox.PrettyCombo(App.Config.DiscordMuteKeys) + "». Сочетание меняется во вкладке «Общие»."));
                    break;
                case "discord_deafen":
                    _actionFields.Children.Add(HintCard("Активирует окно Discord и отправляет «" + HotkeyBox.PrettyCombo(App.Config.DiscordDeafenKeys) + "». Сочетание меняется во вкладке «Общие»."));
                    break;
                case "volume_mute":
                    _actionFields.Children.Add(HintCard("Переключает общий звук системы (как кнопка M на клавиатуре)."));
                    break;
                case "media":
                    _actionFields.Children.Add(HintCard("Play / Pause для Spotify, YouTube Music и других плееров."));
                    break;
                default:
                    _actionFields.Children.Add(HintCard("Лепесток будет просто подсвечиваться — выберите действие."));
                    break;
            }
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

        // ================= GENERAL TAB =================

        Grid BuildGeneralPage()
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = Glass.Label("Общие", 19, false, true);
            title.Margin = new Thickness(2, 2, 0, 10);
            g.Children.Add(title);
            Grid.SetRow(title, 0);

            var stack = new StackPanel();
            var scrollHost = new Grid();
            Grid.SetRow(scrollHost, 1);
            scrollHost.Children.Add(Scroller(stack));
            g.Children.Add(scrollHost);

            // behavior card
            var card = Glass.Card(16, 16, 0.08, 0.03);
            var cs = new StackPanel();
            cs.Children.Add(Glass.Label("Поведение", 14, false, true));

            _tgIntercept = new GlassToggle(App.Config.InterceptClick);
            _tgIntercept.Changed += delegate { App.Config.InterceptClick = _tgIntercept.IsOn; MouseHook.Intercept = _tgIntercept.IsOn; App.SaveConfig(); Pulse(); };
            cs.Children.Add(Rows.Row("Перехватывать среднюю кнопку", "Hinoki ловит клик колёсиком вместо прокрутки", _tgIntercept));

            _tgCursor = new GlassToggle(App.Config.ShowAtCursor);
            _tgCursor.Changed += delegate { App.Config.ShowAtCursor = _tgCursor.IsOn; App.SaveConfig(); Pulse(); };
            cs.Children.Add(Rows.Row("Меню у курсора", "Цветок появляется там, где сейчас курсор", _tgCursor));

            _tgBlur = new GlassToggle(App.Config.BlurBackground);
            _tgBlur.Changed += delegate { App.Config.BlurBackground = _tgBlur.IsOn; App.SaveConfig(); Pulse(); };
            cs.Children.Add(Rows.Row("Размытие фона", "Мягкий acrylic-блюр за стеклом (выключите, если артефакты)", _tgBlur, false));
            card.Child = cs;
            stack.Children.Add(card);

            // size card
            var sizeCard = Glass.Card(16, 16, 0.08, 0.03);
            sizeCard.Margin = new Thickness(0, 12, 0, 0);
            var ss = new StackPanel();
            var srow = new Grid();
            srow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            srow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var st1 = Glass.Label("Размер меню", 13.5, false, true);
            Grid.SetColumn(st1, 0);
            srow.Children.Add(st1);
            _sizeValue = Glass.Label(App.Config.MenuSizePercent + "%", 13, true);
            Grid.SetColumn(_sizeValue, 1);
            srow.Children.Add(_sizeValue);
            ss.Children.Add(srow);
            _sizeSlider = new GlassSlider(55, 100, App.Config.MenuSizePercent);
            _sizeSlider.Margin = new Thickness(0, 8, 0, 2);
            _sizeSlider.Changed += delegate
            {
                App.Config.MenuSizePercent = (int)Math.Round(_sizeSlider.Value);
                _sizeValue.Text = App.Config.MenuSizePercent + "%";
                App.SaveConfig(); Pulse();
            };
            ss.Children.Add(_sizeSlider);
            sizeCard.Child = ss;
            stack.Children.Add(sizeCard);

            // discord card
            var dc = Glass.Card(16, 16, 0.08, 0.03);
            dc.Margin = new Thickness(0, 12, 0, 0);
            var ds = new StackPanel();
            ds.Children.Add(Glass.Label("Discord", 14, false, true));
            _hkMute = new HotkeyBox(App.Config.DiscordMuteKeys);
            _hkMute.Changed += delegate { App.Config.DiscordMuteKeys = _hkMute.Combo; App.SaveConfig(); Pulse(); };
            ds.Children.Add(Rows.Row("Мут микрофона", "Должно совпадать с биндом в настройках Discord", _hkMute));
            _hkDeafen = new HotkeyBox(App.Config.DiscordDeafenKeys);
            _hkDeafen.Changed += delegate { App.Config.DiscordDeafenKeys = _hkDeafen.Combo; App.SaveConfig(); Pulse(); };
            ds.Children.Add(Rows.Row("Мут звука (деафен)", "Должно совпадать с биндом в настройках Discord", _hkDeafen, false));
            dc.Child = ds;
            stack.Children.Add(dc);

            // system card
            var sc = Glass.Card(16, 16, 0.08, 0.03);
            sc.Margin = new Thickness(0, 12, 0, 0);
            var sys = new StackPanel();
            sys.Children.Add(Glass.Label("Система", 14, false, true));

            _tgAutostart = new GlassToggle(App.Config.Autostart);
            _tgAutostart.Changed += delegate { App.Config.Autostart = _tgAutostart.IsOn; App.ApplyAutostart(); App.SaveConfig(); Pulse(); };
            sys.Children.Add(Rows.Row("Запускать вместе с Windows", "Hinoki будет ждать вас в трее", _tgAutostart));

            var testBtn = new PillButton("Открыть меню (тест)", false);
            testBtn.HorizontalAlignment = HorizontalAlignment.Left;
            testBtn.Margin = new Thickness(0, 4, 0, 0);
            testBtn.Clicked += delegate { App.TestMenu(); };
            sys.Children.Add(testBtn);
            sc.Child = sys;
            stack.Children.Add(sc);

            return g;
        }

        // ================= ABOUT TAB =================

        Grid BuildAboutPage()
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = Glass.Label("О программе", 19, false, true);
            title.Margin = new Thickness(2, 2, 0, 10);
            g.Children.Add(title);
            Grid.SetRow(title, 0);

            var sp = new StackPanel();
            var host = new Grid();
            Grid.SetRow(host, 1);
            host.Children.Add(Scroller(sp));
            g.Children.Add(host);

            var hero = Glass.Card(18, 20, 0.09, 0.04);
            var hs = new StackPanel();
            var logoWrap = new Border();
            logoWrap.Width = 76; logoWrap.Height = 76;
            logoWrap.CornerRadius = new CornerRadius(22);
            logoWrap.Child = new Image { Source = FlowerMenu.LogoBitmap, Width = 76, Height = 76 };
            logoWrap.HorizontalAlignment = HorizontalAlignment.Center;
            logoWrap.Effect = new DropShadowEffect { Color = Ui.Accent, BlurRadius = 26, ShadowDepth = 0, Opacity = 0.45 };
            hs.Children.Add(logoWrap);

            var name = Glass.Label("Hinoki", 24, false, true);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            name.Margin = new Thickness(0, 12, 0, 0);
            hs.Children.Add(name);

            var ver = Glass.Label("Версия 1.0.0 · Windows 10/11", 12, true);
            ver.HorizontalAlignment = HorizontalAlignment.Center;
            ver.Margin = new Thickness(0, 2, 0, 0);
            hs.Children.Add(ver);

            var desc = Glass.Label("Одна средняя кнопка мыши — и всё под рукой: мут в Discord, музыка, любимые приложения. Никаких окон, только стеклянный цветок.", 12.5, true);
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
            var upd = new PillButton("Проверить обновления", false);
            upd.Margin = new Thickness(8, 0, 0, 0);
            upd.Clicked += delegate { ActionEngine.OpenLink("https://github.com/arseniyru90-afk/hinoki/releases/latest"); };
            links.Children.Add(gh);
            links.Children.Add(site);
            links.Children.Add(upd);
            hs.Children.Add(links);

            hero.Child = hs;
            sp.Children.Add(hero);

            var reset = new PillButton("Сбросить все настройки", false);
            reset.HorizontalAlignment = HorizontalAlignment.Center;
            reset.Margin = new Thickness(0, 12, 0, 0);
            reset.Clicked += delegate { App.ResetConfig(this); };
            sp.Children.Add(reset);

            return g;
        }

        // ================= misc =================

        public void Pulse()
        {
            _status.Text = "Сохранено ✓";
            var a = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(120)));
            _status.BeginAnimation(OpacityProperty, a);
            var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(400)));
            fade.BeginTime = TimeSpan.FromMilliseconds(1300);
            _status.BeginAnimation(OpacityProperty, null);
            _status.BeginAnimation(OpacityProperty, a);
            _status.Opacity = 1;
            var t = new System.Windows.Threading.DispatcherTimer();
            t.Interval = TimeSpan.FromMilliseconds(1500);
            t.Tick += delegate
            {
                t.Stop();
                var f = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(500)));
                _status.BeginAnimation(OpacityProperty, f);
            };
            t.Start();
        }
    }
}
