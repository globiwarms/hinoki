// Hinoki — tray icon & context menu
using System;
using System.Windows;
using WF = System.Windows.Forms;

namespace Hinoki
{
    internal static class Tray
    {
        static WF.NotifyIcon _icon;
        static WF.ToolStripMenuItem _autoItem;

        public static bool HasIcon
        {
            get { return _icon != null; }
        }

        public static void Init()
        {
            _icon = new WF.NotifyIcon();
            using (var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Hinoki.hinoki.ico"))
                _icon.Icon = new System.Drawing.Icon(s);
            _icon.Text = "Hinoki — средняя кнопка мыши";
            _icon.Visible = true;

            var menu = new WF.ContextMenuStrip();

            var open = new WF.ToolStripMenuItem("Настройки");
            open.Font = new System.Drawing.Font(open.Font, System.Drawing.FontStyle.Bold);
            open.Click += delegate { App.OpenSettings(); };
            menu.Items.Add(open);

            var test = new WF.ToolStripMenuItem("Показать меню");
            test.Click += delegate { App.TestMenu(); };
            menu.Items.Add(test);

            menu.Items.Add(new WF.ToolStripSeparator());

            _autoItem = new WF.ToolStripMenuItem("Запускать с Windows");
            _autoItem.Click += delegate
            {
                App.Config.Autostart = !App.Config.Autostart;
                App.ApplyAutostart();
                App.SaveConfig();
                Sync();
            };
            menu.Items.Add(_autoItem);

            menu.Items.Add(new WF.ToolStripSeparator());

            var exit = new WF.ToolStripMenuItem("Выход");
            exit.Click += delegate { App.ExitApp(); };
            menu.Items.Add(exit);

            _icon.ContextMenuStrip = menu;
            _icon.DoubleClick += delegate { App.OpenSettings(); };
            Sync();
        }

        public static void Sync()
        {
            if (_autoItem != null) _autoItem.Checked = App.Config.Autostart;
        }

        public static void Balloon(string title, string text)
        {
            Toast.Show(title, text, "bell");
        }

        public static void Dispose()
        {
            if (_icon != null)
            {
                _icon.Visible = false;
                _icon.Dispose();
                _icon = null;
            }
        }
    }
}
