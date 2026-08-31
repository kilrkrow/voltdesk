using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

using Application = Microsoft.UI.Xaml.Application;
using Brush = Microsoft.UI.Xaml.Media.Brush;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Border = Microsoft.UI.Xaml.Controls.Border;

namespace PowerDesktopApp
{
    public sealed class SettingsWindow : Window
    {
        private readonly AppConfiguration _config;
        private readonly Action? _onChanged;
        private readonly ToggleSwitch _startToggle;
        private readonly ToggleSwitch _menuToggle;
        private bool _loading;

        public SettingsWindow(AppConfiguration config, Action? onChanged = null)
        {
            _config = config;
            _onChanged = onChanged;
            Title = "VoltDesk";
            SystemBackdrop = new MicaBackdrop();
            TrySetIcon();
            AppWindow.Resize(new Windows.Graphics.SizeInt32(680, 820));

            var titleBar = new Grid { Height = 48, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var titleText = new TextBlock
            {
                Text = "VoltDesk",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 0)
            };
            Grid.SetColumn(titleText, 1);
            titleBar.Children.Add(titleText);

            var pageTitle = new TextBlock
            {
                Text = "Settings",
                FontSize = 28,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 4, 0, 8)
            };

            var hotkeyHint = new TextBlock
            {
                Text = "Click a shortcut, then press the keys. Backspace clears.",
                Opacity = 0.72,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var desktopBox = CreateHotkeyBox(_config.DesktopToggleHotkey, value =>
            {
                _config.DesktopToggleHotkey = value;
                Persist();
            });
            var desktopCard = MakeCard(
                "Desktop icons",
                "Toggle desktop icon visibility.",
                desktopBox,
                first: true,
                last: true);

            _startToggle = MakeSwitch();
            _startToggle.Toggled += StartWithWindows_Toggled;
            var startCard = MakeCard(
                "Start with Windows",
                "Launch VoltDesk when you sign in.",
                _startToggle,
                first: true,
                last: false);

            _menuToggle = MakeSwitch();
            _menuToggle.Toggled += StartMenu_Toggled;
            var menuCard = MakeCard(
                "Start Menu shortcut",
                "Pin a VoltDesk shortcut under All apps.",
                _menuToggle,
                first: false,
                last: true);

            var body = new StackPanel
            {
                Padding = new Thickness(28, 8, 28, 32),
                Spacing = 8,
                MaxWidth = 720
            };
            body.Children.Add(pageTitle);
            body.Children.Add(SectionHeader("Keyboard"));
            body.Children.Add(hotkeyHint);
            body.Children.Add(desktopCard);
            body.Children.Add(SectionHeader("Power profiles"));
            body.Children.Add(BuildProfileCards());
            body.Children.Add(SectionHeader("Startup"));
            body.Children.Add(startCard);
            body.Children.Add(menuCard);

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(titleBar, 0);
            var scroll = new ScrollViewer { Content = body };
            Grid.SetRow(scroll, 1);
            root.Children.Add(titleBar);
            root.Children.Add(scroll);
            root.RequestedTheme = ElementTheme.Default;
            Content = root;

            try
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(titleBar);
            }
            catch
            {
            }

            _loading = true;
            bool startOn = StartupHelper.IsStartOnWindows();
            bool startMenu = StartupHelper.IsStartMenuShortcut();
            _startToggle.IsOn = startOn;
            _menuToggle.IsOn = startMenu;
            _config.StartOnWindows = startOn;
            _config.AddToStartMenu = startMenu;
            _loading = false;
        }

        private UIElement BuildProfileCards()
        {
            var stack = new StackPanel { Spacing = 1 };
            var profiles = PowerManager.GetProfiles();
            if (profiles.Count == 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "No power plans found.",
                    Opacity = 0.72,
                    Margin = new Thickness(4, 8, 4, 8)
                });
                return stack;
            }

            for (int i = 0; i < profiles.Count; i++)
            {
                var sp = profiles[i];
                var conf = _config.Profiles.FirstOrDefault(p => p.Guid == sp.Guid);
                string initial = conf?.Hotkey ?? "";
                var box = CreateHotkeyBox(initial, value => SetProfileHotkey(sp, value));
                string desc = sp.IsActive ? "Currently active." : "Assign a shortcut to switch to this plan.";
                stack.Children.Add(MakeCard(
                    sp.Name,
                    desc,
                    box,
                    first: i == 0,
                    last: i == profiles.Count - 1));
            }
            return stack;
        }

        private void SetProfileHotkey(PowerProfile sp, string hotkey)
        {
            var conf = _config.Profiles.FirstOrDefault(p => p.Guid == sp.Guid);
            if (string.IsNullOrEmpty(hotkey))
            {
                if (conf != null)
                    _config.Profiles.Remove(conf);
            }
            else
            {
                if (conf == null)
                {
                    conf = new PowerProfileHotkey { Guid = sp.Guid, Name = sp.Name };
                    _config.Profiles.Add(conf);
                }
                conf.Hotkey = hotkey;
                conf.Name = sp.Name;
            }
            Persist();
        }

        private void Persist()
        {
            Configuration.Save(_config);
            _onChanged?.Invoke();
        }

        private void StartWithWindows_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading)
                return;
            bool on = _startToggle.IsOn;
            StartupHelper.ApplyStartOnWindows(on);
            _config.StartOnWindows = on;
            Persist();
        }

        private void StartMenu_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading)
                return;
            bool on = _menuToggle.IsOn;
            StartupHelper.ApplyStartMenuShortcut(on);
            _config.AddToStartMenu = on;
            Persist();
        }

        private static ToggleSwitch MakeSwitch()
        {
            return new ToggleSwitch
            {
                OffContent = "Off",
                OnContent = "On",
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 96
            };
        }

        private static TextBlock SectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 16, 0, 4)
            };
        }

        private Border MakeCard(string header, string description, FrameworkElement content, bool first, bool last)
        {
            var labels = new StackPanel { Spacing = 4 };
            labels.Children.Add(new TextBlock
            {
                Text = header,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrEmpty(description))
            {
                labels.Children.Add(new TextBlock
                {
                    Text = description,
                    Opacity = 0.72,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            var row = new Grid { Padding = new Thickness(16, 14, 16, 14) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(labels, 0);
            Grid.SetColumn(content, 1);
            row.Children.Add(labels);
            row.Children.Add(content);

            double tl = first ? 8 : 4;
            double tr = first ? 8 : 4;
            double br = last ? 8 : 4;
            double bl = last ? 8 : 4;

            var border = new Border
            {
                Child = row,
                CornerRadius = new CornerRadius(tl, tr, br, bl),
                Margin = new Thickness(0, first ? 0 : 2, 0, 0)
            };
            border.Loaded += (_, __) => PaintCard(border);
            border.ActualThemeChanged += (_, __) => PaintCard(border);
            return border;
        }

        private static void PaintCard(Border border)
        {
            try
            {
                bool dark = border.ActualTheme == ElementTheme.Dark;
                border.Background = new SolidColorBrush(dark
                    ? Windows.UI.Color.FromArgb(255, 45, 45, 45)
                    : Windows.UI.Color.FromArgb(230, 255, 255, 255));
            }
            catch
            {
            }
        }

        private Button CreateHotkeyBox(string initial, Action<string> onChange)
        {
            string shown = string.IsNullOrEmpty(initial) ? "None" : initial;
            var box = new Button
            {
                Content = shown,
                MinWidth = 168,
                MaxWidth = 220,
                VerticalAlignment = VerticalAlignment.Center
            };
            box.PreviewKeyDown += (s, e) => OnHotkeyPreviewKeyDown(box, e, onChange);
            box.Click += (_, __) => box.Focus(FocusState.Programmatic);
            return box;
        }

        private static void OnHotkeyPreviewKeyDown(Button box, KeyRoutedEventArgs e, Action<string> onChange)
        {
            e.Handled = true;
            VirtualKey key = e.Key == VirtualKey.None ? e.OriginalKey : e.Key;

            if (key == VirtualKey.Back || key == VirtualKey.Delete)
            {
                box.Content = "None";
                onChange("");
                return;
            }

            if (IsModifier(key))
                return;

            string? chord = FormatChord(key);
            if (string.IsNullOrEmpty(chord) || IsReserved(chord))
                return;

            box.Content = chord;
            onChange(chord);
        }

        private static bool IsModifier(VirtualKey key)
        {
            return key is VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl
                or VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift
                or VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu
                or VirtualKey.LeftWindows or VirtualKey.RightWindows;
        }

        private static bool IsDown(VirtualKey key)
        {
            return InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
        }

        private static string? FormatChord(VirtualKey key)
        {
            var mods = new List<string>();
            if (IsDown(VirtualKey.Control) || IsDown(VirtualKey.LeftControl) || IsDown(VirtualKey.RightControl))
                mods.Add("Ctrl");
            if (IsDown(VirtualKey.Shift) || IsDown(VirtualKey.LeftShift) || IsDown(VirtualKey.RightShift))
                mods.Add("Shift");
            if (IsDown(VirtualKey.Menu) || IsDown(VirtualKey.LeftMenu) || IsDown(VirtualKey.RightMenu))
                mods.Add("Alt");

            string keyName = KeyDisplayName(key);
            if (string.IsNullOrEmpty(keyName))
                return null;

            return mods.Count > 0 ? string.Join("+", mods) + "+" + keyName : keyName;
        }

        private static string KeyDisplayName(VirtualKey key)
        {
            if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
                return ((int)(key - VirtualKey.Number0)).ToString();
            if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
                return ((int)(key - VirtualKey.NumberPad0)).ToString();
            if (key >= VirtualKey.A && key <= VirtualKey.Z)
                return key.ToString();
            if (key >= VirtualKey.F1 && key <= VirtualKey.F24)
                return key.ToString();
            return key.ToString();
        }

        private static bool IsReserved(string chord)
        {
            string n = chord.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
            return n is "WIN+V" or "CTRL+WIN+V" or "WIN+CTRL+V"
                or "CTRL+WIN+P" or "WIN+CTRL+P";
        }

        private void TrySetIcon()
        {
            try
            {
                string ico = Path.Combine(AppContext.BaseDirectory, "appicon.ico");
                if (File.Exists(ico))
                    AppWindow.SetIcon(ico);
            }
            catch
            {
            }
        }
    }
}
