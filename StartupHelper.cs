using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PowerDesktopApp
{
    public static class StartupHelper
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "VoltDesk";

        public static void ApplyStartOnWindows(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string appPath = Environment.ProcessPath ?? Application.ExecutablePath;
                            if (!string.IsNullOrEmpty(appPath))
                            {
                                key.SetValue(AppName, $"\"{appPath}\"");
                            }
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Windows startup registry: {ex.Message}");
            }

            try
            {
                if (!enable)
                {
                    string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    string shortcutPath = Path.Combine(startupFolder, "VoltDesk.lnk");
                    if (File.Exists(shortcutPath))
                    {
                        File.Delete(shortcutPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing startup shortcut: {ex.Message}");
            }
        }
    }
}
