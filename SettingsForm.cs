using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerDesktopApp
{
    public class SettingsForm : Form
    {
        private AppConfiguration _config;
        private List<PowerProfile> _systemProfiles;
        
        private ListBox _lstProfiles;
        private TextBox _txtDesktopHotkey;
        private TextBox _txtProfileHotkey;
        private Button _btnSave;
        
        public SettingsForm(AppConfiguration config)
        {
            _config = config;
            _systemProfiles = PowerManager.GetProfiles();
            
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "VoltDesk - Settings";
            this.Size = new Size(400, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lblDesktop = new Label { Text = "Desktop Toggle Hotkey:", Location = new Point(10, 10), AutoSize = true };
            _txtDesktopHotkey = new TextBox { Location = new Point(150, 10), Width = 200, ReadOnly = true };
            _txtDesktopHotkey.KeyDown += HotkeyTextBox_KeyDown;

            var lblProfiles = new Label { Text = "Power Profiles:", Location = new Point(10, 40), AutoSize = true };
            _lstProfiles = new ListBox { Location = new Point(10, 60), Size = new Size(360, 150) };
            _lstProfiles.SelectedIndexChanged += LstProfiles_SelectedIndexChanged;

            var lblProfileHotkey = new Label { Text = "Profile Hotkey:", Location = new Point(10, 220), AutoSize = true };
            _txtProfileHotkey = new TextBox { Location = new Point(150, 220), Width = 200, ReadOnly = true };
            _txtProfileHotkey.KeyDown += HotkeyTextBox_KeyDown;

            // Make the shortcut textbox clearable using Backspace
            var lblHint = new Label { Text = "Press desired keys. Use Backspace to clear.", Location = new Point(150, 245), AutoSize = true, ForeColor = Color.Gray };

            _btnSave = new Button { Text = "Save", Location = new Point(290, 275), Width = 80 };
            _btnSave.Click += BtnSave_Click;

            this.Controls.Add(lblDesktop);
            this.Controls.Add(_txtDesktopHotkey);
            this.Controls.Add(lblProfiles);
            this.Controls.Add(_lstProfiles);
            this.Controls.Add(lblProfileHotkey);
            this.Controls.Add(_txtProfileHotkey);
            this.Controls.Add(lblHint);
            this.Controls.Add(_btnSave);
        }

        private void HotkeyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true; // Prevent ding sound
            
            var tb = (TextBox)sender;
            
            if (e.KeyCode == Keys.Back)
            {
                tb.Text = string.Empty;
                UpdateProfileConfigFromUI(tb);
                return;
            }

            var key = e.KeyCode;
            if (key == Keys.Menu || key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.LWin || key == Keys.RWin)
                return; // only modifiers

            List<string> modifiers = new List<string>();
            if (e.Control) modifiers.Add("Ctrl");
            if (e.Shift) modifiers.Add("Shift");
            if (e.Alt) modifiers.Add("Alt");
            
            string keyStr = key.ToString();
            // Handle number row
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                keyStr = keyStr.Substring(1); // D1 -> 1
            }

            string hotkeyStr = modifiers.Any() ? $"{string.Join("+", modifiers)}+{keyStr}" : keyStr;
            tb.Text = hotkeyStr;
            
            UpdateProfileConfigFromUI(tb);
        }

        private void UpdateProfileConfigFromUI(TextBox sourceTb)
        {
            if (sourceTb == _txtProfileHotkey && _lstProfiles.SelectedIndex >= 0)
            {
                var activeProfile = _systemProfiles[_lstProfiles.SelectedIndex];
                
                var confProfile = _config.Profiles.FirstOrDefault(p => p.Guid == activeProfile.Guid);
                if (confProfile == null)
                {
                    confProfile = new PowerProfileHotkey { Guid = activeProfile.Guid, Name = activeProfile.Name };
                    _config.Profiles.Add(confProfile);
                }
                
                confProfile.Hotkey = sourceTb.Text;
                
                // Remove if empty
                if (string.IsNullOrEmpty(sourceTb.Text))
                {
                    _config.Profiles.Remove(confProfile);
                }

                RefreshProfileList(); // Keeps selection intact
            }
        }

        private void LoadData()
        {
            _txtDesktopHotkey.Text = _config.DesktopToggleHotkey;
            RefreshProfileList();
        }

        private void RefreshProfileList()
        {
            int prevIndex = _lstProfiles.SelectedIndex;
            _lstProfiles.Items.Clear();

            foreach (var sp in _systemProfiles)
            {
                var confProfile = _config.Profiles.FirstOrDefault(p => p.Guid == sp.Guid);
                string hotkey = confProfile != null && !string.IsNullOrEmpty(confProfile.Hotkey) ? confProfile.Hotkey : "None";
                _lstProfiles.Items.Add($"{sp.Name} [{hotkey}]");
            }

            if (prevIndex >= 0 && prevIndex < _lstProfiles.Items.Count)
                _lstProfiles.SelectedIndex = prevIndex;
        }

        private void LstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lstProfiles.SelectedIndex < 0) return;
            
            var sp = _systemProfiles[_lstProfiles.SelectedIndex];
            var confProfile = _config.Profiles.FirstOrDefault(p => p.Guid == sp.Guid);
            _txtProfileHotkey.Text = confProfile?.Hotkey ?? "";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _config.DesktopToggleHotkey = _txtDesktopHotkey.Text;
            Configuration.Save(_config);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
