using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PowerDesktopApp
{
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private int currentId = 1;

        private readonly Dictionary<int, Action> hotkeyActions = new Dictionary<int, Action>();
        private readonly HotkeyWindow _window;

        public HotkeyManager()
        {
            _window = new HotkeyWindow();
            _window.CreateHandle(new CreateParams { Caption = "VoltDeskHotkeys" });
            _window.HotkeyPressed = OnHotkey;
        }

        public bool RegisterHotkey(uint modifiers, Keys key, Action action)
        {
            int id = currentId++;
            if (RegisterHotKey(_window.Handle, id, modifiers, (uint)key))
            {
                hotkeyActions[id] = action;
                return true;
            }
            return false;
        }

        public bool RegisterHotkey(string hotkeyString, Action action)
        {
            if (ParseHotkeyString(hotkeyString, out uint mods, out Keys key))
            {
                return RegisterHotkey(mods, key, action);
            }
            return false;
        }

        public void UnregisterAll()
        {
            foreach (var id in hotkeyActions.Keys)
            {
                UnregisterHotKey(_window.Handle, id);
            }
            hotkeyActions.Clear();
        }

        public void Dispose()
        {
            UnregisterAll();
            _window.DestroyHandle();
        }

        private void OnHotkey(int id)
        {
            if (hotkeyActions.TryGetValue(id, out var action))
                action();
        }

        public static bool ParseHotkeyString(string hotkeyString, out uint modifiers, out Keys key)
        {
            modifiers = 0;
            key = Keys.None;

            if (string.IsNullOrWhiteSpace(hotkeyString))
                return false;

            var parts = hotkeyString.Split('+');
            foreach (var part in parts)
            {
                string p = part.Trim().ToUpper();
                if (p == "CTRL" || p == "CONTROL" || p == "STRG") modifiers |= 2;
                else if (p == "SHIFT") modifiers |= 4;
                else if (p == "ALT") modifiers |= 1;
                else if (p == "WIN" || p == "WINDOWS") modifiers |= 8;
                else
                {
                    if (p.Length == 1 && char.IsDigit(p[0]))
                    {
                        if (Enum.TryParse("D" + p, true, out Keys dKey))
                            key = dKey;
                    }
                    else if (Enum.TryParse(p, true, out Keys k))
                    {
                        if (!int.TryParse(p, out _))
                        {
                            key = k;
                        }
                    }
                }
            }

            return key != Keys.None;
        }

        private sealed class HotkeyWindow : NativeWindow
        {
            public Action<int>? HotkeyPressed;

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                    HotkeyPressed?.Invoke(m.WParam.ToInt32());
                base.WndProc(ref m);
            }
        }
    }
}
