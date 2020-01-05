using Crucible.Filesystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Crucible.CustomControls
{
    /// <summary>
    /// Interaction logic for ClosableTabButton.xaml
    /// </summary>
    internal partial class CustomContextMenuItem : MenuItem
    {
        public enum MenuItemType
        {
            None,
            Save,
            Close,
            CloseAll,
            CloseAllButThis,
            CloseAllToTheLeft,
            CloseAllToTheRight,
            CopyFullPath,
            ShowInExplorer
        }

        public static readonly DependencyProperty ItemTypeProperty = DependencyProperty.RegisterAttached("ItemType", typeof(MenuItemType), typeof(CustomContextMenuItem), new PropertyMetadata(default(MenuItemType)));
        public MenuItemType ItemType { get => (MenuItemType)GetValue(ItemTypeProperty); set => SetValue(ItemTypeProperty, value); }

        public CustomContextMenuItem()
        {
            InitializeComponent();
            this.IsVisibleChanged += CustomContextMenuItem_IsVisibleChanged;
        }

        private void CustomContextMenuItem_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            switch (ItemType)
            {
                case MenuItemType.Save:
                    this.Visibility = Visibility.Collapsed;
                    break;
                case MenuItemType.ShowInExplorer:
                    {
                        TabItem tabItem = CrucibleUtil.FindParent<TabItem>(this);
                        var tabControl = CrucibleUtil.FindParent<TabControl>(tabItem);
                        var dataContext = this.DataContext ?? tabItem?.DataContext;

                        var localFilesystemEntry = dataContext as LocalFilesystemEntry;
                        if (localFilesystemEntry == null)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private static void CloseTabItem(TabControl tabControl, TabItem tabItem)
        {
            tabControl.Items.Remove(tabItem);
            IDisposable disposable = tabItem as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        private enum CloseTabsDirection
        {
            None,
            Left,
            Right
        }

        private static void CloseTabs(TabControl tabControl, TabItem exception = null, CloseTabsDirection closeDirection = CloseTabsDirection.None)
        {
            List<TabItem> tabsToRemove = new List<TabItem>();

            int exceptionIndex = tabControl.Items.IndexOf(exception);
            foreach (TabItem tabItem in tabControl.Items)
            {
                int currentIndex = tabControl.Items.IndexOf(tabItem);

                if (closeDirection != CloseTabsDirection.None && exception == null)
                {
                    throw new ArgumentNullException("exception");
                }

                if (closeDirection == CloseTabsDirection.Left)
                {
                    if (currentIndex >= exceptionIndex)
                    {
                        continue;
                    }
                }
                else if (closeDirection == CloseTabsDirection.Right)
                {
                    if (currentIndex <= exceptionIndex)
                    {
                        continue;
                    }
                }

                if (tabItem != exception)
                {
                    tabsToRemove.Add(tabItem);
                }
            }

            foreach (TabItem tabItem in tabsToRemove)
            {
                IDisposable disposable = tabItem as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }

                tabControl.Items.Remove(tabItem);
            }
        }

        private void Button_Click(object _sender, RoutedEventArgs e)
        {
            TabItem tabItem = CrucibleUtil.FindParent<TabItem>(this);
            var tabControl = CrucibleUtil.FindParent<TabControl>(tabItem);
            var dataContext = this.DataContext ?? tabItem?.DataContext;

            if (tabControl == null)
            {
                throw new Exception("tabControl is null");
            }

            switch (ItemType)
            {
                case MenuItemType.Save:
                    break;
                case MenuItemType.Close:
                    CloseTabItem(tabControl, tabItem);
                    break;
                case MenuItemType.CloseAll:
                    CloseTabs(tabControl);
                    break;
                case MenuItemType.CloseAllButThis:
                    CloseTabs(tabControl, tabItem);
                    break;
                case MenuItemType.CloseAllToTheLeft:
                    CloseTabs(tabControl, tabItem, CloseTabsDirection.Left);
                    break;
                case MenuItemType.CloseAllToTheRight:
                    CloseTabs(tabControl, tabItem, CloseTabsDirection.Right);
                    break;
                case MenuItemType.CopyFullPath:
                    {
                        var filesystemEntry = dataContext as IFilesystemEntry;
                        if (filesystemEntry != null)
                        {
                            switch (filesystemEntry)
                            {
                                case P4KFilesystemEntry p4KFilesystemEntry:
                                    Clipboard.SetText(p4KFilesystemEntry.FullPath);
                                    break;
                                case LocalFilesystemEntry localFilesystemEntry:
                                    Clipboard.SetText(localFilesystemEntry.FileInfo.FullName);
                                    break;
                            }
                        }
                    }
                    break;
                case MenuItemType.ShowInExplorer:
                    {
                        var localFilesystemEntry = dataContext as LocalFilesystemEntry;
                        if (localFilesystemEntry != null)
                        {
                            string args = string.Format("/e, /select, \"{0}\"", localFilesystemEntry.FileInfo.FullName);

                            Process.Start("explorer.exe", args);
                        }
                    }
                    break;
                case MenuItemType.None:
                default:
                    break;
            }



        }
    }
}
