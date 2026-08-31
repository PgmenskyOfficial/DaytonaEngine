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

            // Synchronizuje słownik językowy z okna głównego, aby tekst dostosował się do wybranego języka
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