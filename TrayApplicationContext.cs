using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerDesktopApp
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private AppConfiguration _config;
        private HotkeyManager _hotkeyManager;

        public TrayApplicationContext()
        {
            _config = Configuration.Load();
            _hotkeyManager = new HotkeyManager();

            _trayIcon = new NotifyIcon()
            {
                Icon = new Icon("appicon.ico"),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "VoltDesk"
            };

            var settingsItem = new ToolStripMenuItem("Settings", null, ShowSettings);
            var exitItem = new ToolStripMenuItem("Exit", null, Exit);
            
            _trayIcon.ContextMenuStrip.Items.Add(settingsItem);
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _trayIcon.ContextMenuStrip.Items.Add(exitItem);

            _trayIcon.DoubleClick += ShowSettings;

            RegisterHotkeys();
        }

        private void RegisterHotkeys()
        {
            _hotkeyManager.UnregisterAll();

            if (!string.IsNullOrEmpty(_config.DesktopToggleHotkey))
            {
                _hotkeyManager.RegisterHotkey(_config.DesktopToggleHotkey, DesktopHelper.ToggleDesktopIcons);
            }

            foreach (var profile in _config.Profiles)
            {
                if (!string.IsNullOrEmpty(profile.Hotkey))
                {
                    // Important: Copy closure variable for lambda
                    string guid = profile.Guid;
                    string name = profile.Name;
                    _hotkeyManager.RegisterHotkey(profile.Hotkey, () => 
                    {
                        PowerManager.SetActiveProfile(guid);
                        ShowNotification("Power Profile Applied", $"Switched to {name}");
                    });
                }
            }
        }

        private void ShowNotification(string title, string text)
        {
            _trayIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
        }

        private void ShowSettings(object sender, EventArgs e)
        {
            using (var form = new SettingsForm(_config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Config was saved inside SettingsForm, so just reload hotkeys
                    _config = Configuration.Load();
                    RegisterHotkeys();
                }
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _hotkeyManager.Dispose();
            Application.Exit();
        }
    }
}
