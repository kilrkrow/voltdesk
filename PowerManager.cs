using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PowerDesktopApp
{
    public class PowerProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public static class PowerManager
    {
        public static List<PowerProfile> GetProfiles()
        {
            var profiles = new List<PowerProfile>();
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/list",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Output looks like:
            // Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced) *

            var regex = new Regex(@"Power Scheme GUID:\s+([0-9a-f\-]+)\s+\(([^)]+)\)\s*(\*)?");
            foreach (var line in output.Split('\n'))
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    profiles.Add(new PowerProfile
                    {
                        Guid = match.Groups[1].Value,
                        Name = match.Groups[2].Value,
                        IsActive = match.Groups[3].Success // '*' indicates active
                    });
                }
            }

            return profiles;
        }

        public static void SetActiveProfile(string guid)
        {
            // Simple validation to prevent bad arguments
            if (!Regex.IsMatch(guid, @"^[0-9a-f\-]+$"))
                return;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = $"/setactive {guid}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}
