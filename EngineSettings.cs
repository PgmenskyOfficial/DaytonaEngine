using System;
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

                //Windows a polish lang a defualt a en
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
    }
}