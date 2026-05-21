using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PowerDesktopApp
{
    public class PowerProfileHotkey
    {
        public string Name { get; set; } = string.Empty;
        public string Hotkey { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
    }

    public class AppConfiguration
    {
        public List<PowerProfileHotkey> Profiles { get; set; } = new List<PowerProfileHotkey>();
        public string DesktopToggleHotkey { get; set; } = "Ctrl+Shift+D";
        public bool StartOnWindows { get; set; } = true;
        public bool AddToStartMenu { get; set; } = false;
    }

    public static class Configuration
    {
        private static readonly string ConfigPath = GetConfigPath();

        private static string GetConfigPath()
        {
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoltDesk");
            try
            {
                Directory.CreateDirectory(appDataFolder);
            }
            catch {}
            
            string newPath = Path.Combine(appDataFolder, "appsettings.json");
            string oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                try
                {
                    File.Copy(oldPath, newPath, true);
                    File.Delete(oldPath);
                }
                catch
                {
                    // Fallback: If we can't write to AppData for some reason, use old path
                    return oldPath;
                }
            }

            return newPath;
        }

        public static AppConfiguration Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfiguration>(json) ?? new AppConfiguration();
                }
                catch
                {
                    return new AppConfiguration();
                }
            }
            return new AppConfiguration();
        }

        public static void Save(AppConfiguration config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
    }
}
