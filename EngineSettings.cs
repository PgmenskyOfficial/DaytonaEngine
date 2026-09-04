using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Globalization;
using Serilog;

namespace DaytonaEngine
{
    public class EngineSettings
    {
        // Auto OS Lang for DE
        public string Language { get; set; } = GetDefaultLanguage();
        public bool AutoSave { get; set; } = false;
        public double EditorFontSize { get; set; } = 12.0;

        // List of recent project files (.de)
        public List<string> RecentFiles { get; set; } = new List<string>();

        private static readonly string SettingsFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DaytonaEngine"
        );

        private static readonly string SettingsFilePath = Path.Combine(SettingsFolderPath, "settings.json");

        // Method lang OS
        private static string GetDefaultLanguage()
        {
            try
            {
                string systemLang = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;

                // Windows a polish lang a default a en
                if (systemLang.Equals("pl", StringComparison.OrdinalIgnoreCase))
                {
                    return "pl";
                }
            }
            catch
            {

            }

            return "en";
        }

        public static EngineSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<EngineSettings>(json) ?? new EngineSettings();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load configuration file from AppData.");
            }

            return new EngineSettings();
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsFolderPath))
                {
                    Directory.CreateDirectory(SettingsFolderPath);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);

                Log.Information("Application settings successfully saved to: {Path}", SettingsFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save configuration file to AppData.");
            }
        }

        /// <summary>
        /// Adds a file path to the recent projects list and automatically saves settings.
        /// </summary>
        public void AddRecentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            // Remove duplicate if the file is already on the list (to move it to the top)
            RecentFiles.RemoveAll(f => f.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            // Insert at the very beginning of the list
            RecentFiles.Insert(0, filePath);

            // Limit the list to a maximum of 5 items
            if (RecentFiles.Count > 5)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }

            // Automatically save changes via the Save method
            Save();
        }
    }
}