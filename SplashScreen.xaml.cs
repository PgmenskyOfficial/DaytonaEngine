using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Linq;
using Serilog;

namespace DaytonaEngine
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            ApplySplashLanguage();
            LoadEngineAsync();
        }

        private void ApplySplashLanguage()
        {
            try
            {
                var settings = EngineSettings.Load();
                string langCode = settings.Language;

                var dictionaries = Application.Current.Resources.MergedDictionaries;
                var oldDict = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Languages", StringComparison.OrdinalIgnoreCase));

                if (oldDict != null)
                {
                    dictionaries.Remove(oldDict);
                }

                string fileName = langCode == "pl" ? "LangPL_pl" : "LangEN_en";
                var newDict = new ResourceDictionary();
                newDict.Source = new Uri($"pack://application:,,,/DaytonaEngine;component/Languages/{fileName}.xaml", UriKind.Absolute);
                dictionaries.Add(newDict);

                Log.Information("SplashScreen language applied: {LangCode}", langCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply language in SplashScreen.");
            }
        }

        private async void LoadEngineAsync()
        {
            // Simulate engine loading (e.g. 2.5 seconds)
            await Task.Delay(2500);

            // Close the splash screen
            this.Close();
        }
    }
}