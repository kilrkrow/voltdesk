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
    }

    public static class Configuration
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

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
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}
