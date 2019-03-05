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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataCoreBinary2
{
    /// <summary>
    /// Interaction logic for PointerCreationTypeSelectionWindow.xaml
    /// </summary>
    public partial class PointerCreationTypeSelectionWindow : Window
    {
        private Type _selectedType;
        public Type SelectedType { get => _selectedType; set => _selectedType = value; }

        public PointerCreationTypeSelectionWindow(IEnumerable<Type> types)
        {
            InitializeComponent();

            Icon = Crucible.MainWindow.PrimaryWindow.Icon;

            SelectedType = types.ElementAt(0);
            cbSelectedType.ItemsSource = types;
            cbSelectedType.SelectedIndex = 0;
        }

        private void Titlebar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.ClickCount == 2) MaximizeToggle_Click(sender, e);
                else this.DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
