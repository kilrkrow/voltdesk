using System;
using System.Runtime.InteropServices;

namespace PowerDesktopApp
{
    public static class DesktopHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_COMMAND = 0x0111;
        private const int ToggleDesktopIconsCommand = 0x7402;

        public static void ToggleDesktopIcons()
        {
            IntPtr hWnd = FindWindow("Progman", "Program Manager");
            IntPtr defView = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            
            if (defView != IntPtr.Zero)
            {
                SendMessage(defView, WM_COMMAND, new IntPtr(ToggleDesktopIconsCommand), IntPtr.Zero);
            }
            else
            {
                SendMessage(hWnd, WM_COMMAND, new IntPtr(ToggleDesktopIconsCommand), IntPtr.Zero);
            }
        }
    }
}
