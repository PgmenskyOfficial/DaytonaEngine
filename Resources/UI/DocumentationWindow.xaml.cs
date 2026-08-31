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

namespace DaytonaEngine.Resources.UI
{
    public partial class DocumentationWindow : Window
    {
        public DocumentationWindow()
        {
            InitializeComponent();

            // Synchronize the language dictionary with the main window so the text adapts to the selected language
            if (Application.Current.MainWindow != null)
            {
                this.Resources.MergedDictionaries.Clear();
                foreach (var dictionary in Application.Current.MainWindow.Resources.MergedDictionaries)
                {
                    this.Resources.MergedDictionaries.Add(dictionary);
                }
            }
        }
    }
}