using System;
using System.Management;
using System.Runtime.InteropServices;
using Serilog;

namespace DaytonaEngine
{
    public static class SystemInfo
    {
        public static void LogSystemSpecifications()
        {
            try
            {
                Log.Information("=== SYSTEM SPECIFICATIONS ===");
                Log.Information("OS Version: {OS}", RuntimeInformation.OSDescription);
                Log.Information("OS Architecture: {Arch}", RuntimeInformation.OSArchitecture);
                Log.Information("Processor (CPU): {CPU}", GetProcessorName());
                Log.Information("Installed RAM: {RAM} GB", GetTotalMemoryInGB());
                Log.Information("=============================");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve hardware and system specifications.");
            }
        }

        private static string GetProcessorName()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Name from Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                    {
                        var name = item["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            return name.Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not query CPU information via WMI.");
            }
            return "Unknown Processor";
        }

        private static double GetTotalMemoryInGB()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize from Win32_OperatingSystem"))
                {
                    foreach (var item in searcher.Get())
                    {
                        var kbStr = item["TotalVisibleMemorySize"]?.ToString();
                        if (long.TryParse(kbStr, out long totalKb))
                        {
                            // Przeliczamy Kilobajty na Gigabajty i zaokrąglamy do 2 miejsc po przecinku
                            return Math.Round((double)totalKb / (1024 * 1024), 2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not query RAM information via WMI.");
            }
            return 0.0;
        }
    }
}