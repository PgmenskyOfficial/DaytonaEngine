using DaytonaEngine.Resources.UI;
using Fluent;
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
using System.Windows.Threading;
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;
using Serilog;
using System.Linq;

namespace DaytonaEngine
{
    public partial class DE : Fluent.RibbonWindow
    {
        private System.Windows.Controls.TextBox? activeTextBox = null;
        private string? currentFilePath = null;
        private bool isModified = false;

        // --- ZOOM FUNCTIONALITY FOR BINARY TEXTBOX ---
        private double _zoomLevel = 1.0;

        // --- AUTO-SAVE TIMER ---
        private DispatcherTimer _autoSaveTimer = new();

        public DE()
        {
            InitializeComponent();

            Log.Information("Initializing main editor window (DE).");

            var settings = EngineSettings.Load();
            ApplyGlobalLanguage(settings.Language);

            EventManager.RegisterClassHandler(typeof(System.Windows.Controls.TextBox), System.Windows.Controls.TextBox.GotFocusEvent, new RoutedEventHandler((s, e) => {
                if (s is System.Windows.Controls.TextBox tb)
                {
                    AttachTextBox(tb);
                }
            }));

            // --- ICONS ---
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/DE_LOGO_16X.ico", UriKind.Relative));
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/DE_LOGO.ico", UriKind.Relative));

            // --- INITIALIZE AUTO-SAVE ---
            SetupAutoSave();
        }

        // --- ZOOM MOUSE WHEEL HANDLER ---
        private void BinaryTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Check if Ctrl key is pressed to perform zoom
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                {
                    _zoomLevel += 0.1; // Zoom in
                }
                else
                {
                    _zoomLevel -= 0.1; // Zoom out
                    if (_zoomLevel < 0.3) _zoomLevel = 0.3; // Minimum zoom limit (30%)
                }

                // Apply scale transform to the text box
                BinaryTextScaleTransform.ScaleX = _zoomLevel;
                BinaryTextScaleTransform.ScaleY = _zoomLevel;

                // Update zoom percentage text in the header
                ZoomPercentageText.Text = $"{Math.Round(_zoomLevel * 100)}%";

                // Mark event as handled to prevent standard vertical scrolling while zooming
                e.Handled = true;
            }
        }

        private void SetupAutoSave()
        {
            var settings = EngineSettings.Load();

            _autoSaveTimer.Interval = TimeSpan.FromMinutes(3);
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;

            // run timer also a autosave is on a settings
            if (settings.AutoSave)
            {
                _autoSaveTimer.Start();
                Log.Information("Auto-save timer initialized and started (Enabled in settings).");
            }
            else
            {
                Log.Information("Auto-save timer initialized, but disabled in settings.");
            }
        }

        private void RefreshAutoSaveState()
        {
            var settings = EngineSettings.Load();

            if (settings.AutoSave)
            {
                if (!_autoSaveTimer.IsEnabled)
                {
                    _autoSaveTimer.Start();
                    Log.Information("Auto-save enabled via settings update.");
                }
            }
            else
            {
                if (_autoSaveTimer.IsEnabled)
                {
                    _autoSaveTimer.Stop();
                    Log.Information("Auto-save disabled via settings update.");
                }
            }
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            // auto save also a project a modified
            if (isModified && !string.IsNullOrEmpty(currentFilePath))
            {
                Log.Information("Auto-save triggered for path: {FilePath}", currentFilePath);
                SaveToFile(currentFilePath);
            }
        }

        private void AttachTextBox(System.Windows.Controls.TextBox tb)
        {
            if (activeTextBox != tb)
            {
                if (activeTextBox != null)
                {
                    activeTextBox.TextChanged -= TextBox_TextChanged;
                }
                activeTextBox = tb;
                activeTextBox.TextChanged += TextBox_TextChanged;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isModified = true;
        }

        // --- Data Protection SAVE (Dynamic Language Support) ---
        protected override void OnClosing(CancelEventArgs e)
        {
            if (isModified)
            {
                Log.Warning("Attempting to close the editor with unsaved changes.");

                string msgText = Application.Current.TryFindResource("Msg_UnsavedChanges") as string ?? "Masz niezapisane zmiany. Czy chcesz je zapisać przed wyjściem?";
                string msgTitle = Application.Current.TryFindResource("Msg_WarningTitle") as string ?? "Niezapisane zmiany";

                var result = MessageBox.Show(msgText, msgTitle, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Log.Information("User chose to save changes while exiting the application.");
                    Save_Click(this, new RoutedEventArgs());

                    if (isModified)
                    {
                        Log.Information("Application shutdown was aborted (save dialog was canceled).");
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    Log.Information("User canceled editor shutdown.");
                    e.Cancel = true;
                }
                else
                {
                    Log.Information("User closed the editor without saving changes.");
                }
            }
            else
            {
                Log.Information("Closing editor (no unsaved changes).");
            }

            // auto save time stop from app exit
            _autoSaveTimer?.Stop();

            base.OnClosing(e);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Opened 'About' window.");
            var aboutWindow = new AboutDE();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        // --- (Cut, Copy, Paste) ---
        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            activeTextBox?.Cut();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            activeTextBox?.Copy();
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            activeTextBox?.Paste();
        }

        // --- VIEW HIDE UI ---
        private void BtnHierarchy_Checked(object sender, RoutedEventArgs e)
        {
            if (HierarchyPanel != null) HierarchyPanel.Visibility = Visibility.Visible;
        }

        private void BtnHierarchy_Unchecked(object sender, RoutedEventArgs e)
        {
            if (HierarchyPanel != null) HierarchyPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnProperties_Checked(object sender, RoutedEventArgs e)
        {
            if (PropertiesPanel != null) PropertiesPanel.Visibility = Visibility.Visible;
        }

        private void BtnProperties_Unchecked(object sender, RoutedEventArgs e)
        {
            if (PropertiesPanel != null) PropertiesPanel.Visibility = Visibility.Collapsed;
        }

        // --- Settings CLICK ---
        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Opened 'Settings' window.");
            DaytonaEngine.Resources.UI.Settings settingsWindow = new DaytonaEngine.Resources.UI.Settings();
            settingsWindow.Owner = this;

            // also user click save a window settings
            if (settingsWindow.ShowDialog() == true)
            {
                Log.Information("Settings saved. Re-applying global preferences.");

                // load fresh settings .json
                var settings = EngineSettings.Load();

                // refresh language
                ApplyGlobalLanguage(settings.Language);

                // font size
                if (activeTextBox != null)
                {
                    activeTextBox.FontSize = settings.EditorFontSize;
                }

                // refresh auto-save timer state dynamically
                RefreshAutoSaveState();
            }
        }

        // helped method refresh dictionary lang
        private void ApplyGlobalLanguage(string langCode)
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
                // uri
                newDict.Source = new Uri($"pack://application:,,,/DaytonaEngine;component/Languages/{fileName}.xaml", UriKind.Absolute);
                dictionaries.Add(newDict);

                Log.Information("Global language re-applied from main window: {LangCode}", langCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to re-applied global language: {LangCode}", langCode);
            }
        }

        // --- Doc CLICK  ---
        private void MenuDocumentation_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Opened 'Documentation' window.");
            var docWindow = new DocumentationWindow();
            docWindow.Owner = this;
            docWindow.ShowDialog();
        }

        // --- NEW, OPEN, SAVE (MultiLang) ---
        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (isModified)
            {
                string msgTextCheck = Application.Current.TryFindResource("Msg_UnsavedChanges") as string ?? "Masz niezapisane zmiany. Czy chcesz kontynuować?";
                string msgTitleCheck = Application.Current.TryFindResource("Msg_WarningTitle") as string ?? "Ostrzeżenie";
                var checkRes = MessageBox.Show(msgTextCheck, msgTitleCheck, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (checkRes == MessageBoxResult.No)
                {
                    Log.Information("Creation of a new project was canceled due to unsaved changes.");
                    return;
                }
            }

            string msgText = Application.Current.TryFindResource("Msg_NewProject") as string ?? "Czy chcesz utworzyć nowy projekt? Niezapisane zmiany zostaną utracone.";
            string msgTitle = Application.Current.TryFindResource("Msg_NewTitle") as string ?? "Nowy projekt";

            var result = MessageBox.Show(msgText, msgTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                currentFilePath = null;
                if (activeTextBox != null) activeTextBox.Text = string.Empty;
                Title = "DaytonaEngine Editor - Nowy Projekt";
                isModified = false;
                Log.Information("Created a new project (editor state cleared).");
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Daytona Engine Files (*.de)|*.de|Wszystkie pliki (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    currentFilePath = openFileDialog.FileName;
                    string fileContent = File.ReadAllText(currentFilePath);

                    if (activeTextBox != null) activeTextBox.Text = fileContent;

                    Title = $"DaytonaEngine Editor - {System.IO.Path.GetFileName(currentFilePath)}";
                    isModified = false;

                    Log.Information("Successfully opened project file: {FilePath}", currentFilePath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Critical error while opening project file: {FilePath}", openFileDialog.FileName);

                    string errTitle = Application.Current.TryFindResource("Msg_Error") as string ?? "Błąd";
                    string errMsg = Application.Current.TryFindResource("Msg_OpenError") as string ?? "Nie udało się otworzyć pliku: ";
                    MessageBox.Show($"{errMsg}{ex.Message}", errTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveAs();
            }
            else
            {
                SaveToFile(currentFilePath);
            }
        }

        private void SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Daytona Engine Files (*.de)|*.de|Wszystkie pliki (*.*)|*.*";
            saveFileDialog.DefaultExt = "de";
            saveFileDialog.AddExtension = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                currentFilePath = saveFileDialog.FileName;
                SaveToFile(currentFilePath);

                Title = $"DaytonaEngine Editor - {System.IO.Path.GetFileName(currentFilePath)}";
            }
        }

        private void SaveToFile(string path)
        {
            try
            {
                string contentToSave = activeTextBox != null ? activeTextBox.Text : string.Empty;
                File.WriteAllText(path, contentToSave);
                isModified = false;

                Log.Information("Successfully saved file to path: {FilePath}", path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Critical error while saving file to path: {FilePath}", path);

                string errTitle = Application.Current.TryFindResource("Msg_Error") as string ?? "Błąd";
                string errMsg = Application.Current.TryFindResource("Msg_SaveError") as string ?? "Nie udało się zapisać pliku: ";
                MessageBox.Show($"{errMsg}{ex.Message}", errTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}