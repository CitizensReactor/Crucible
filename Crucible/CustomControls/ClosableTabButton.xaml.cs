using System;
using System.Windows;
using System.Windows.Controls;

namespace Crucible.CustomControls
{
    /// <summary>
    /// Interaction logic for ClosableTabButton.xaml
    /// </summary>
    internal partial class ClosableTabButton : Button
    {
        public ClosableTabButton()
        {
            InitializeComponent();
        }

        private void Button_Click(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as ClosableTabButton;
            var tabItem = CrucibleUtil.FindParent<TabItem>(this);

            var dataContext = sender?.DataContext ?? tabItem?.DataContext;

            var tabControl = CrucibleUtil.FindParent<TabControl>(tabItem);
            if(tabControl != null)
            {
                tabControl.Items.Remove(tabItem);
                IDisposable disposable = tabItem as IDisposable;
                if(disposable != null)
                {
                    disposable.Dispose();
                }
            }
            else if (tabItem != null)
            {
                MainWindow.PrimaryWindow.CloseTab(tabItem);
            }
            else
            {
                MainWindow.PrimaryWindow.CloseTabByDataContext(dataContext);
            }
        }
    }
}
