using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Serilog;

namespace DaytonaEngine.Resources.UI
{
    public partial class Settings : Window
    {
        private EngineSettings currentSettings;
        private bool _isInitializing = true; // Flag blocking changed from started

        public Settings()
        {
            InitializeComponent();

            // Load saved settings from AppData
            currentSettings = EngineSettings.Load();
            LoadSettingsToUI();

            _isInitializing = false; // unlocked change lang from user
        }

        private void LoadSettingsToUI()
        {
            // ComboBox lang
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == currentSettings.Language)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }

            // --- loaded auto save  ---
            AutoSaveCheckBox.IsChecked = currentSettings.AutoSave;

            Log.Information("Settings loaded into UI from AppData.");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Safe load cod from lang
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                string? langCode = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(langCode))
                {
                    currentSettings.Language = langCode;
                }
            }
            else
            {
                // Fallback 0 = pl, 1 = en
                currentSettings.Language = (LanguageComboBox.SelectedIndex == 1) ? "en" : "pl";
            }

            // --- auto save check ---
            currentSettings.AutoSave = AutoSaveCheckBox.IsChecked ?? false;

            // Save to AppData/Local/DaytonaEngine/settings.json
            currentSettings.Save();

            Log.Information("User saved settings changes. Saved language is: {Lang}, AutoSave is: {AutoSave}", currentSettings.Language, currentSettings.AutoSave);

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("User cancelled settings changes.");
            this.DialogResult = false;
            this.Close();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // also window loaded , ingore a danger
            if (_isInitializing) return;

            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? langCode = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(langCode))
                {
                    currentSettings.Language = langCode; // Update a object option
                    Log.Information("ComboBox selection changed. Current settings language updated to: {LangCode}", langCode);
                    ChangeLanguage(langCode);
                }
            }
            else if (LanguageComboBox.SelectedIndex >= 0)
            {
                string langCode = LanguageComboBox.SelectedIndex == 1 ? "en" : "pl";
                currentSettings.Language = langCode;
                Log.Information("ComboBox selection changed (fallback index). Updated to: {LangCode}", langCode);
                ChangeLanguage(langCode);
            }
        }

        private void ChangeLanguage(string langCode)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var oldDict = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Languages", StringComparison.OrdinalIgnoreCase));
            if (oldDict != null)
            {
                dictionaries.Remove(oldDict);
            }

            try
            {
                string fileName = langCode == "pl" ? "LangPL_pl" : "LangEN_en";
                var newDict = new ResourceDictionary();
                //  URI
                newDict.Source = new Uri($"pack://application:,,,/DaytonaEngine;component/Languages/{fileName}.xaml", UriKind.Absolute);
                dictionaries.Add(newDict);

                Log.Information("Language switched dynamically to: {LangCode}", langCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load language dictionary: {LangCode}", langCode);
                MessageBox.Show($"Nie udało się załadować języka: {ex.Message}");
            }
        }
    }
}