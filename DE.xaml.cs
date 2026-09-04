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
using ICSharpCode.AvalonEdit;

namespace DaytonaEngine
{
    public partial class DE : Fluent.RibbonWindow
    {
        private ICSharpCode.AvalonEdit.TextEditor? activeEditor = null;
        private string? currentFilePath = null;
        private bool _isModified = false;

        private bool isModified
        {
            get => _isModified;
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    UpdateWindowTitle();
                }
            }
        }

        // --- ZOOM FUNCTIONALITY FOR BINARY EDITOR ---
        private double _zoomLevel = 1.0;

        // --- AUTO-SAVE TIMER ---
        private DispatcherTimer _autoSaveTimer = new();

        public DE()
        {
            InitializeComponent();

            Log.Information("Initializing main editor window (DE) with AvalonEdit.");

            var settings = EngineSettings.Load();
            ApplyGlobalLanguage(settings.Language);

            // Hook into AvalonEdit text changed and caret position changed
            if (BinaryEditor != null)
            {
                BinaryEditor.TextChanged += BinaryEditor_TextChanged;
                BinaryEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
                activeEditor = BinaryEditor;

                // Enable file Drag & Drop support for the editor
                BinaryEditor.AllowDrop = true;
                BinaryEditor.PreviewDragOver += BinaryEditor_PreviewDragOver;
                BinaryEditor.Drop += BinaryEditor_Drop;
            }

            // --- ICONS ---
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/DE_LOGO_16X.ico", UriKind.Relative));
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("Resources/DE_LOGO.ico", UriKind.Relative));

            // --- INITIALIZE AUTO-SAVE ---
            SetupAutoSave();

            // --- INITIALIZE RECENT FILES MENU ---
            RefreshRecentFilesMenu();
            UpdateWindowTitle();
        }

        // --- WINDOW TITLE UPDATE (DIRTY MARK ASTERISK) ---
        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(currentFilePath) ? "New Project" : System.IO.Path.GetFileName(currentFilePath);
            string dirtyMark = isModified ? "*" : string.Empty;
            Title = $"DaytonaEngine Editor - {fileName}{dirtyMark}";
        }

        // --- KEYBOARD SHORTCUTS (CTRL+N, CTRL+O, CTRL+S, CTRL+F, CTRL+G, ESC) ---
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.N)
                {
                    New_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.O)
                {
                    Open_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.S)
                {
                    Save_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.F)
                {
                    if (CustomSearchPanel != null)
                    {
                        CustomSearchPanel.Visibility = Visibility.Visible;
                        if (ReplaceGrid != null) ReplaceGrid.Visibility = Visibility.Collapsed;
                        SearchTextBox.Focus();
                        if (!string.IsNullOrEmpty(BinaryEditor?.SelectedText))
                        {
                            SearchTextBox.Text = BinaryEditor.SelectedText;
                        }
                        SearchTextBox.SelectAll();
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.G)
                {
                    if (CustomSearchPanel != null)
                    {
                        CustomSearchPanel.Visibility = Visibility.Visible;
                        if (ReplaceGrid != null) ReplaceGrid.Visibility = Visibility.Visible;
                        SearchTextBox.Focus();
                        if (!string.IsNullOrEmpty(BinaryEditor?.SelectedText))
                        {
                            SearchTextBox.Text = BinaryEditor.SelectedText;
                        }
                        SearchTextBox.SelectAll();
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                if (CustomSearchPanel != null && CustomSearchPanel.Visibility == Visibility.Visible)
                {
                    CustomSearchPanel.Visibility = Visibility.Collapsed;
                    BinaryEditor?.Focus();
                    e.Handled = true;
                }
            }
        }

        // --- CUSTOM SEARCH & REPLACE PANEL EVENT HANDLERS ---
        private void BtnCloseSearch_Click(object sender, RoutedEventArgs e)
        {
            CustomSearchPanel.Visibility = Visibility.Collapsed;
            BinaryEditor?.Focus();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNextText();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CustomSearchPanel.Visibility = Visibility.Collapsed;
                BinaryEditor?.Focus();
                e.Handled = true;
            }
        }

        private void BtnFindNext_Click(object sender, RoutedEventArgs e) => FindNextText();
        private void BtnFindPrev_Click(object sender, RoutedEventArgs e) => FindPreviousText();
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => FindNextText();

        private void BtnReplace_Click(object sender, RoutedEventArgs e)
        {
            if (BinaryEditor == null || string.IsNullOrEmpty(SearchTextBox.Text)) return;

            string query = SearchTextBox.Text;
            string replaceWith = ReplaceTextBox.Text ?? string.Empty;

            if (BinaryEditor.SelectionLength > 0 && string.Equals(BinaryEditor.SelectedText, query, StringComparison.OrdinalIgnoreCase))
            {
                int selectionStart = BinaryEditor.SelectionStart;
                BinaryEditor.Document.Replace(selectionStart, query.Length, replaceWith);
                BinaryEditor.CaretOffset = selectionStart + replaceWith.Length;
            }

            FindNextText();
        }

        private void BtnReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            if (BinaryEditor == null || string.IsNullOrEmpty(SearchTextBox.Text)) return;

            string query = SearchTextBox.Text;
            string replaceWith = ReplaceTextBox.Text ?? string.Empty;
            string text = BinaryEditor.Text;

            if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                string updatedText = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    System.Text.RegularExpressions.Regex.Escape(query),
                    replaceWith,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                BinaryEditor.Text = updatedText;
                Log.Information("Replaced all occurrences of '{Query}' with '{ReplaceWith}'", query, replaceWith);
            }
        }

        private void FindNextText()
        {
            if (BinaryEditor == null || string.IsNullOrEmpty(SearchTextBox.Text)) return;

            string query = SearchTextBox.Text;
            string text = BinaryEditor.Text;
            int startPos = BinaryEditor.CaretOffset;

            int index = text.IndexOf(query, startPos, StringComparison.OrdinalIgnoreCase);
            if (index == -1) index = text.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                BinaryEditor.Select(index, query.Length);
                BinaryEditor.ScrollToLine(BinaryEditor.Document.GetLineByOffset(index).LineNumber);
            }
        }

        private void FindPreviousText()
        {
            if (BinaryEditor == null || string.IsNullOrEmpty(SearchTextBox.Text)) return;

            string query = SearchTextBox.Text;
            string text = BinaryEditor.Text;
            int startPos = Math.Max(0, BinaryEditor.CaretOffset - query.Length - 1);

            int index = text.LastIndexOf(query, startPos, StringComparison.OrdinalIgnoreCase);
            if (index == -1) index = text.LastIndexOf(query, text.Length - 1, StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                BinaryEditor.Select(index, query.Length);
                BinaryEditor.ScrollToLine(BinaryEditor.Document.GetLineByOffset(index).LineNumber);
            }
        }

        // --- CARET POSITION (LINE & COLUMN) ---
        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            if (BinaryEditor?.TextArea?.Caret != null && CaretPositionText != null)
            {
                int line = BinaryEditor.TextArea.Caret.Line;
                int column = BinaryEditor.TextArea.Caret.Column;
                CaretPositionText.Text = $"Ln {line}, Col {column}";
            }
        }

        // --- DRAG & DROP FILE SUPPORT ---
        private void BinaryEditor_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void BinaryEditor_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && File.Exists(files[0]))
                {
                    OpenProjectFromFile(files[0]);
                }
            }
        }

        // --- RECENT FILES LOGIC ---
        private void RefreshRecentFilesMenu()
        {
            if (RecentFilesDropdown == null) return;

            var settings = EngineSettings.Load();
            RecentFilesDropdown.Items.Clear();

            if (settings.RecentFiles.Count == 0)
            {
                RecentFilesDropdown.Items.Add(new System.Windows.Controls.MenuItem { Header = "No recent projects", IsEnabled = false });
                return;
            }

            foreach (var filePath in settings.RecentFiles)
            {
                var menuItem = new System.Windows.Controls.MenuItem
                {
                    Header = System.IO.Path.GetFileName(filePath),
                    ToolTip = filePath
                };
                menuItem.Click += (sender, args) => OpenRecentProject(filePath);
                RecentFilesDropdown.Items.Add(menuItem);
            }
        }

        private void OpenRecentProject(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("The file does not exist or has been deleted.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                var settings = EngineSettings.Load();
                settings.RecentFiles.Remove(filePath);
                settings.Save();
                RefreshRecentFilesMenu();
                return;
            }

            if (isModified)
            {
                string msgTextCheck = Application.Current.TryFindResource("Msg_UnsavedChanges") as string ?? "You have unsaved changes. Do you want to continue?";
                if (MessageBox.Show(msgTextCheck, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
            }

            OpenProjectFromFile(filePath);
        }

        private void OpenProjectFromFile(string filePath)
        {
            try
            {
                currentFilePath = filePath;
                if (BinaryEditor != null) BinaryEditor.Text = File.ReadAllText(currentFilePath);
                if (RootProjectItem != null) RootProjectItem.Header = System.IO.Path.GetFileNameWithoutExtension(filePath);

                isModified = false;
                var settings = EngineSettings.Load();
                settings.AddRecentFile(currentFilePath);
                RefreshRecentFilesMenu();
                Log.Information("Successfully opened project file: {FilePath}", currentFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while opening project file: {FilePath}", filePath);
                MessageBox.Show($"Failed to open file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BinaryEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                _zoomLevel += e.Delta > 0 ? 0.1 : -0.1;
                if (_zoomLevel < 0.3) _zoomLevel = 0.3;

                BinaryTextScaleTransform.ScaleX = _zoomLevel;
                BinaryTextScaleTransform.ScaleY = _zoomLevel;
                ZoomPercentageText.Text = $"{Math.Round(_zoomLevel * 100)}%";
                e.Handled = true;
            }
        }

        private void SetupAutoSave()
        {
            var settings = EngineSettings.Load();
            _autoSaveTimer.Interval = TimeSpan.FromMinutes(3);
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            if (settings.AutoSave) _autoSaveTimer.Start();
        }

        private void RefreshAutoSaveState()
        {
            var settings = EngineSettings.Load();
            if (settings.AutoSave)
            {
                if (!_autoSaveTimer.IsEnabled) _autoSaveTimer.Start();
            }
            else
            {
                if (_autoSaveTimer.IsEnabled) _autoSaveTimer.Stop();
            }
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            if (isModified && !string.IsNullOrEmpty(currentFilePath))
            {
                SaveToFile(currentFilePath);
            }
        }

        private void BinaryEditor_TextChanged(object? sender, EventArgs e) => isModified = true;

        protected override void OnClosing(CancelEventArgs e)
        {
            if (isModified)
            {
                string msgText = Application.Current.TryFindResource("Msg_UnsavedChanges") as string ?? "You have unsaved changes. Do you want to save them before exiting?";
                var result = MessageBox.Show(msgText, "Unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Save_Click(this, new RoutedEventArgs());
                    if (isModified) e.Cancel = true;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
            _autoSaveTimer?.Stop();
            base.OnClosing(e);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e) => new AboutDE { Owner = this }.ShowDialog();
        private void Cut_Click(object sender, RoutedEventArgs e) => BinaryEditor?.Cut();
        private void Copy_Click(object sender, RoutedEventArgs e) => BinaryEditor?.Copy();
        private void Paste_Click(object sender, RoutedEventArgs e) => BinaryEditor?.Paste();

        private void BtnHierarchy_Checked(object sender, RoutedEventArgs e) { if (HierarchyPanel != null) HierarchyPanel.Visibility = Visibility.Visible; }
        private void BtnHierarchy_Unchecked(object sender, RoutedEventArgs e) { if (HierarchyPanel != null) HierarchyPanel.Visibility = Visibility.Collapsed; }
        private void BtnProperties_Checked(object sender, RoutedEventArgs e) { if (PropertiesPanel != null) PropertiesPanel.Visibility = Visibility.Visible; }
        private void BtnProperties_Unchecked(object sender, RoutedEventArgs e) { if (PropertiesPanel != null) PropertiesPanel.Visibility = Visibility.Collapsed; }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new DaytonaEngine.Resources.UI.Settings { Owner = this };
            if (settingsWindow.ShowDialog() == true)
            {
                var settings = EngineSettings.Load();
                ApplyGlobalLanguage(settings.Language);
                if (BinaryEditor != null) BinaryEditor.FontSize = settings.EditorFontSize;
                RefreshAutoSaveState();
            }
        }

        private void ApplyGlobalLanguage(string langCode)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var oldDict = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Languages", StringComparison.OrdinalIgnoreCase));
            if (oldDict != null) dictionaries.Remove(oldDict);

            try
            {
                string fileName = langCode == "pl" ? "LangPL_pl" : "LangEN_en";
                dictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/DaytonaEngine;component/Languages/{fileName}.xaml", UriKind.Absolute) });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply language");
            }
        }

        private void MenuDocumentation_Click(object sender, RoutedEventArgs e) => new DocumentationWindow { Owner = this }.ShowDialog();

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (isModified && MessageBox.Show("You have unsaved changes. Continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;

            if (MessageBox.Show("Do you want to create a new project?", "New project", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                currentFilePath = null;
                if (BinaryEditor != null) BinaryEditor.Text = string.Empty;
                if (RootProjectItem != null) RootProjectItem.Header = "New Project";
                isModified = false;
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Daytona Engine Project (*.depr)|*.depr|All files (*.*)|*.*" };
            if (openFileDialog.ShowDialog() == true) OpenProjectFromFile(openFileDialog.FileName);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath)) SaveAs();
            else SaveToFile(currentFilePath);
        }

        private void SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "Daytona Engine Project (*.depr)|*.depr|All files (*.*)|*.*", DefaultExt = "depr", AddExtension = true };
            if (saveFileDialog.ShowDialog() == true)
            {
                currentFilePath = saveFileDialog.FileName;
                if (RootProjectItem != null) RootProjectItem.Header = System.IO.Path.GetFileNameWithoutExtension(currentFilePath);
                SaveToFile(currentFilePath);
            }
        }

        private void SaveToFile(string path)
        {
            try
            {
                File.WriteAllText(path, BinaryEditor?.Text ?? string.Empty);
                isModified = false;
                var settings = EngineSettings.Load();
                settings.AddRecentFile(path);
                RefreshRecentFilesMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- HIERARCHY TREEVIEW ACTIONS (ADD / DELETE / RENAME / KEY_DELETE) ---
        private void ProjectTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteSelectedHierarchyItem();
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                BeginRenameSelectedHierarchyItem();
                e.Handled = true;
            }
        }

        private void MenuDeleteHierarchyItem_Click(object sender, RoutedEventArgs e) => DeleteSelectedHierarchyItem();

        private void MenuRenameHierarchyItem_Click(object sender, RoutedEventArgs e)
        {
            BeginRenameSelectedHierarchyItem();
        }

        private void MenuAddHierarchyItem_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem targetParent = RootProjectItem;
            if (ProjectTreeView.SelectedItem is TreeViewItem selected)
            {
                targetParent = selected;
            }

            var newItem = new TreeViewItem
            {
                Header = "New Folder",
                Foreground = System.Windows.Media.Brushes.White
            };

            var contextMenu = new System.Windows.Controls.ContextMenu();

            var addMenu = new System.Windows.Controls.MenuItem();
            addMenu.SetResourceReference(System.Windows.Controls.MenuItem.HeaderProperty, "Ctx_AddItem");
            addMenu.Click += MenuAddHierarchyItem_Click;

            var renameMenu = new System.Windows.Controls.MenuItem();
            renameMenu.SetResourceReference(System.Windows.Controls.MenuItem.HeaderProperty, "Ctx_Rename");
            renameMenu.Click += MenuRenameHierarchyItem_Click;

            var deleteMenu = new System.Windows.Controls.MenuItem();
            deleteMenu.SetResourceReference(System.Windows.Controls.MenuItem.HeaderProperty, "Ctx_Delete");
            deleteMenu.Click += MenuDeleteHierarchyItem_Click;

            contextMenu.Items.Add(addMenu);
            contextMenu.Items.Add(renameMenu);
            contextMenu.Items.Add(deleteMenu);
            newItem.ContextMenu = contextMenu;

            targetParent.Items.Add(newItem);
            targetParent.IsExpanded = true;
            newItem.IsSelected = true;

            BeginRenameSelectedHierarchyItem();
        }

        private void BeginRenameSelectedHierarchyItem()
        {
            if (ProjectTreeView.SelectedItem is TreeViewItem item)
            {
                string currentText = item.Header?.ToString() ?? string.Empty;

                var inputBox = new DaytonaEngine.Resources.UI.InputBoxWindow(currentText)
                {
                    Owner = this
                };

                if (inputBox.ShowDialog() == true)
                {
                    if (!string.IsNullOrWhiteSpace(inputBox.AnswerText))
                    {
                        item.Header = inputBox.AnswerText;
                    }
                }
            }
        }

        private void DeleteSelectedHierarchyItem()
        {
            if (ProjectTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem != RootProjectItem)
            {
                var parentItem = FindParentTreeViewItem(selectedItem);
                if (parentItem != null) parentItem.Items.Remove(selectedItem);
                else RootProjectItem.Items.Remove(selectedItem);
            }
        }

        private TreeViewItem? FindParentTreeViewItem(TreeViewItem item)
        {
            DependencyObject parentObj = VisualTreeHelper.GetParent(item);
            while (parentObj != null && !(parentObj is TreeViewItem))
            {
                parentObj = VisualTreeHelper.GetParent(parentObj);
            }
            return parentObj as TreeViewItem;
        }
    }
}