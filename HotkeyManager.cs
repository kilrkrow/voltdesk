using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PowerDesktopApp
{
    public class HotkeyManager : IMessageFilter, IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private int currentId = 1;

        private readonly Dictionary<int, Action> hotkeyActions = new Dictionary<int, Action>();

        public HotkeyManager()
        {
            Application.AddMessageFilter(this);
        }

        // Modifiers: 1 = Alt, 2 = Control, 4 = Shift, 8 = Win
        public bool RegisterHotkey(uint modifiers, Keys key, Action action)
        {
            int id = currentId++;
            // Register for the current thread message queue
            if (RegisterHotKey(IntPtr.Zero, id, modifiers, (uint)key))
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
                UnregisterHotKey(IntPtr.Zero, id);
            }
            hotkeyActions.Clear();
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (hotkeyActions.TryGetValue(id, out var action))
                {
                    action();
                    return true; // Match handled
                }
            }
            return false;
        }

        public void Dispose()
        {
            UnregisterAll();
            Application.RemoveMessageFilter(this);
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
                    // Workarounds for single keys (e.g. D1 for 1)
                    if (p.Length == 1 && char.IsDigit(p[0]))
                    {
                        if (Enum.TryParse("D" + p, true, out Keys dKey))
                            key = dKey;
                    }
                    // Attempt to parse the enum key
                    else if (Enum.TryParse(p, true, out Keys k))
                    {
                        // Prevent purely numeric strings from parsing as underlying integer enum values
                        if (!int.TryParse(p, out _))
                        {
                            key = k;
                        }
                    }
                }
            }

            return key != Keys.None;
        }
    }
}
