using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PowerDesktopApp
{
    public class TrayApplicationContext
    {
        private NotifyIcon _trayIcon;
        private AppConfiguration _config;
        private HotkeyManager _hotkeyManager;
        private ToolStripMenuItem _profilesMenuHeader;
        private SettingsWindow? _settingsWindow;

        public TrayApplicationContext()
        {
            _config = Configuration.Load();
            StartupHelper.ApplyStartOnWindows(_config.StartOnWindows);
            StartupHelper.ApplyStartMenuShortcut(_config.AddToStartMenu);
            _hotkeyManager = new HotkeyManager();

            _trayIcon = new NotifyIcon()
            {
                Icon = LoadAppIcon(),
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

        private static Icon LoadAppIcon()
        {
            string ico = Path.Combine(AppContext.BaseDirectory, "appicon.ico");
            if (File.Exists(ico))
                return new Icon(ico);

            string? exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                Icon? extracted = Icon.ExtractAssociatedIcon(exe);
                if (extracted != null)
                    return extracted;
            }

            throw new FileNotFoundException("VoltDesk appicon.ico is missing.");
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

        public void OpenSettings() => ShowSettings(this, EventArgs.Empty);

        public void ExitFromTray() => Exit(this, EventArgs.Empty);

        private void ShowSettings(object sender, EventArgs e)
        {
            if (_settingsWindow is not null)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(_config, RegisterHotkeys);
            _settingsWindow.Closed += SettingsWindow_Closed;
            _settingsWindow.Activate();
        }

        private void SettingsWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
        {
            if (_settingsWindow is not null)
            {
                _settingsWindow.Closed -= SettingsWindow_Closed;
                _settingsWindow = null;
            }
            _config = Configuration.Load();
            RegisterHotkeys();
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _hotkeyManager.Dispose();
            if (Microsoft.UI.Xaml.Application.Current is App app)
                app.ShutdownFromTray();
            else
                Microsoft.UI.Xaml.Application.Current?.Exit();
        }
    }
}
