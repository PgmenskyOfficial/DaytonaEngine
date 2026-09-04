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
    public partial class InputBoxWindow : Window
    {
        public string AnswerText => ResponseTextBox.Text;

        public InputBoxWindow(string defaultValue = "")
        {
            InitializeComponent();
            ResponseTextBox.Text = defaultValue;
            ResponseTextBox.Loaded += (s, e) => {
                ResponseTextBox.Focus();
                ResponseTextBox.SelectAll();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}