using ControlzEx.Theming;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using System.IO;
using System;
using System.Linq;

namespace DaytonaEngine
{
    public partial class App : Application
    {
        public App()
        {
            // 1. Serilog Startup
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DaytonaEngine", "Logs", "log-.txt");

            Log.Logger = new LoggerConfiguration()
                      .MinimumLevel.Debug()
                      .WriteTo.File(
                          logPath,
                          rollingInterval: RollingInterval.Day,
                          retainedFileCountLimit: 7,
                          buffered: false
                      )
                      .CreateLogger();

            Log.Information("--- DaytonaEngine BETA VERSION DATE 31.08.2026 ---");

            // collect OS, Procesor , Ram a log
            SystemInfo.LogSystemSpecifications();

            // 2. high contrast no problem
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;

            // 3. load settings and language
            var settings = EngineSettings.Load();
            ApplyLanguage(settings.Language);
        }

        private void ApplyLanguage(string langCode)
        {
            var dictionaries = Current.Resources.MergedDictionaries;

            // delete language also 
            var oldDict = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Resources", StringComparison.OrdinalIgnoreCase));
            if (oldDict != null)
            {
                dictionaries.Remove(oldDict);
            }

            try
            {
                string fileName = langCode == "pl" ? "LangPL_pl" : "LangEN_en";
                var newDict = new ResourceDictionary();
                // URI from resources
                newDict.Source = new Uri($"pack://application:,,,/DaytonaEngine;component/Languages/{fileName}.xaml", UriKind.Absolute);
                dictionaries.Add(newDict);

                Log.Information("Global language applied at startup: {LangCode}", langCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply global language: {LangCode}", langCode);
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // safe theme load
            ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Steel");
            Log.Debug("Theme Loaded");

            // Splash screen start
            SplashScreenWindow splash = new SplashScreenWindow();
            splash.Show();

            // 3 seconds
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                Log.Information("Shutdown Splash. Open Main window ! ");

                // 2. NEW WINDOW
                DE mainWindow = new DE();

                // 3. CURRENT WINDOW
                Application.Current.MainWindow = mainWindow;

                // 4. SHOW
                mainWindow.Show();

                // 5. SPLASH SCREEN CLOSE
                splash.Close();

                // 6. SHUTDOWN
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            };
            timer.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("--- DaytonaEngine EXIT ---");
            // log exit
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}