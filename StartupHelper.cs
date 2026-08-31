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

        public static void ApplyStartMenuShortcut(bool enable)
        {
            string shortcutPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs), 
                "VoltDesk.lnk"
            );

            try
            {
                if (enable)
                {
                    string appPath = Environment.ProcessPath ?? Application.ExecutablePath;
                    if (!string.IsNullOrEmpty(appPath))
                    {
                        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                        if (shellType != null)
                        {
                            object shell = Activator.CreateInstance(shellType)!;
                            object shortcut = shellType.InvokeMember("CreateShortcut", 
                                System.Reflection.BindingFlags.InvokeMethod, 
                                null, shell, new object[] { shortcutPath })!;
                            
                            Type shortcutType = shortcut.GetType();
                            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { appPath });
                            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { "VoltDesk Quick Access App" });
                            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(appPath)! });
                            shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { appPath });
                            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);

                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
                        }
                    }
                }
                else
                {
                    if (File.Exists(shortcutPath))
                    {
                        File.Delete(shortcutPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error managing Start Menu shortcut: {ex.Message}");
            }
        }
        public static bool IsStartOnWindows()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    return key?.GetValue(AppName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsStartMenuShortcut()
        {
            string shortcutPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "VoltDesk.lnk");
            return File.Exists(shortcutPath);
        }
    }
}
