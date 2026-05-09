using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PowerDesktopApp
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private AppConfiguration _config;
        private HotkeyManager _hotkeyManager;
        private ToolStripMenuItem _profilesMenuHeader;

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

            _profilesMenuHeader = new ToolStripMenuItem("Power Profiles");

            var settingsItem = new ToolStripMenuItem("Settings", null, ShowSettings);
            var exitItem = new ToolStripMenuItem("Exit", null, Exit);

            _trayIcon.ContextMenuStrip.Items.Add(_profilesMenuHeader);
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _trayIcon.ContextMenuStrip.Items.Add(settingsItem);
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _trayIcon.ContextMenuStrip.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip.Opening += ContextMenu_Opening;
            _trayIcon.DoubleClick += ShowSettings;

            RegisterHotkeys();
        }

        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            BuildProfileMenuItems();
        }

        private void BuildProfileMenuItems()
        {
            _profilesMenuHeader.DropDownItems.Clear();

            var profiles = PowerManager.GetProfiles();

            foreach (var profile in profiles)
            {
                string guid = profile.Guid;
                string name = profile.Name;

                var item = new ToolStripMenuItem(name)
                {
                    Checked = profile.IsActive,
                    CheckOnClick = false
                };

                item.Click += (s, args) =>
                {
                    PowerManager.SetActiveProfile(guid);
                    ShowNotification("Power Profile Applied", $"Switched to {name}");
                };

                _profilesMenuHeader.DropDownItems.Add(item);
            }
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