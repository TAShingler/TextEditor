using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;

namespace TextEditor {
    /// <summary>
    /// Interaction logic for MainWindowTemplate.xaml
    /// </summary>
    public partial class MainWindowTemplate : Window {
        public MainWindowTemplate() {
            InitializeComponent();
        }

        //private void SetUpLines(string data) {
        //    int noLines = string.IsNullOrEmpty(data) ? 0 : data.Split('\n').Length;
        //    StringBuilder sb = new StringBuilder();
        //    for (int i = 1; i <= noLines; i++) {
        //        sb.AppendLine(i.ToString());
        //    }

        //    TextBoxLineNumbers.Text = sb.ToString();
        //    TextBoxResult.Text = data ?? "";
        //}

        //private void TextBox_ScrollChanged(object sender, ScrollChangedEventArgs e) {
        //    TextBoxLineNumbers.ScrollToVerticalOffset(TextBoxResult.VerticalOffset);

        //    SetUpLines(TextBoxResult.Text);
        //}

        
    }
}
