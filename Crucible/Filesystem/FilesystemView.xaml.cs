using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Crucible.Filesystem
{
    /// <summary>
    /// Interaction logic for FilesystemView.xaml
    /// </summary>
    internal partial class FilesystemView : BindableUserControl
    {
        private IVirtualFilesystem _filesystem;
        public IVirtualFilesystem Filesystem { get => _filesystem; internal set => SetProperty(ref _filesystem, value); }
        private ObservableCollection<IFilesystemEntry> _items;
        public ObservableCollection<IFilesystemEntry> Items { get => _items; internal set => SetProperty(ref _items, value); }

        public FilesystemView(IVirtualFilesystem filesystem)
        {
            Filesystem = filesystem;
            Items = filesystem.RootDirectory.Items;
            filesystem.RootDirectory.Sort();

            InitializeComponent();

            var root = new SharpDevelopFilesystemNode(filesystem.RootDirectory);
            treeView2.Root = root;
            treeView2.ShowRoot = false;

            switch (filesystem)
            {
                case P4KVirtualFilesystem p4KVirtualFilesystem:
#if DEBUG
                    // Example testing code, please don't commit this but its useful for automatically
                    // opening a specific tab from the P4K in DEBUG mode. Saves lots of time on
                    // repetitive tasks etc. - Unknown44

                    //var dcb = p4KVirtualFilesystem[@"Data\Game.dcb"];
                    //if (dcb != null)
                    //{
                    //    MainWindow.PrimaryWindow.OpenFileTab(dcb);
                    //}

                    //var bnkFile = p4KVirtualFilesystem[@"data\Sounds\wwise\WPMT_RSI_IRI337_Series.bnk"];
                    //if (bnkFile != null)
                    //{
                    //    MainWindow.PrimaryWindow.OpenFileTab(bnkFile);
                    //}

                    //var cgf = p4KVirtualFilesystem[@"data\Objects\animals\fish\Fish_clean_prop_animal_01.cgf"];
                    //if (cgf != null)
                    //{
                    //    MainWindow.PrimaryWindow.OpenFileTab(cgf);
                    //}
#endif
                    break;
            }

            var data = filesystem.RootDirectory.Items.Where(item => string.Equals(item.Name, "Data", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (data != null)
            {
                data.IsExpanded = true;
            }
        }


        FilesystemTreeViewItem CreateFilesystemTree(IFilesystemEntry entry)
        {
            FilesystemTreeViewItem filesystemTreeViewItem = new FilesystemTreeViewItem(entry);

            if (entry.IsDirectory)
            {
                foreach (var childFilesystemEntry in entry.Items)
                {
                    var childTreeViewItem = CreateFilesystemTree(childFilesystemEntry);
                    filesystemTreeViewItem.Items.Add(childTreeViewItem);
                }
            }

            return filesystemTreeViewItem;
        }


        FilesystemTreeViewItem CreateFilesystemTree(IVirtualFilesystem filesystem)
        {
            var filesystemTreeViewRoot = CreateFilesystemTree(filesystem.RootDirectory);

            //// sort the file structure
            //foreach (var folder in folders)
            //{
            //    folder.Value.Sort();
            //}

            return filesystemTreeViewRoot;
        }

        private void OpenFilesystemEntry(IFilesystemEntry filesystemEntry)
        {
            if (filesystemEntry.IsDirectory) return;
            else
            {
                MainWindow.PrimaryWindow.OpenFileTab(filesystemEntry);
            }
        }


        private void FilesystemItemDoubleClick(object _sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            FrameworkElement frameworkElement = _sender as FrameworkElement;
            if (frameworkElement == null) return;
            if (frameworkElement.DataContext == null) return;

            switch (frameworkElement.DataContext)
            {
                case IFilesystemEntry filesystemEntry:
                    OpenFilesystemEntry(filesystemEntry);
                    break;
                case SharpDevelopFilesystemNode filesystemNode:
                    OpenFilesystemEntry(filesystemNode.FilesystemEntry);
                    break;
                default:
                    throw new NotImplementedException();
            }

            e.Handled = true;
        }

        private void FileContextMenu_Click_Open(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as dynamic;
            var filesystemEntry = sender?.DataContext as IFilesystemEntry;
            if (filesystemEntry == null) return;

            if (filesystemEntry.IsDirectory)
            {
                filesystemEntry.IsExpanded = !filesystemEntry.IsExpanded;
            }
            else
            {
                MainWindow.PrimaryWindow.OpenFileTab(filesystemEntry);
            }
        }

        private void Extract(IFilesystemEntry filesystemEntry, MainWindow.ExtractionMode mode)
        {
            MainWindow.PrimaryWindow.ExtractFiles(filesystemEntry as P4KFilesystemEntry, mode);
        }

        private void FileContextMenu_Click_CustomExtract(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as FrameworkElement;
            if (sender == null) return;
            var filesystemEntry = sender.DataContext as IFilesystemEntry;
            if (filesystemEntry == null) return;

            Extract(filesystemEntry, MainWindow.ExtractionMode.Raw);
        }

        private void FileContextMenu_Click_Extract(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as FrameworkElement;
            if (sender == null) return;
            var filesystemEntry = sender.DataContext as IFilesystemEntry;
            if (filesystemEntry == null) return;

            Extract(filesystemEntry, MainWindow.ExtractionMode.Converted);
        }

        private bool ExtractTo(IFilesystemEntry filesystemEntry, MainWindow.ExtractionMode mode)
        {
            if (!filesystemEntry.IsDirectory)
            {
                string filepath = null;
                if (CommonSaveFileDialog.IsPlatformSupported)
                {
                    var dialog = new CommonSaveFileDialog();
                    dialog.DefaultFileName = filesystemEntry.Name;
                    CommonFileDialogResult result = dialog.ShowDialog();
                    if (result == CommonFileDialogResult.Ok)
                    {
                        filepath = dialog.FileName;
                    }
                }
                else
                {
                    var dialog = new SaveFileDialog();
                    dialog.FileName = filesystemEntry.Name;
                    if (dialog.ShowDialog() ?? false)
                    {
                        filepath = dialog.FileName;
                    }
                }

                if (filepath != null)
                {
                    MainWindow.PrimaryWindow.ExtractFiles(filesystemEntry as P4KFilesystemEntry, mode, filepath);
                }
                else
                {
                    MainWindow.SetStatus($"Failed to save {filesystemEntry.Name}");
                }

                return true;
            }
            if (filesystemEntry.IsDirectory)
            {
                string directory = null;
                if (CommonOpenFileDialog.IsPlatformSupported)
                {
                    var dialog = new CommonOpenFileDialog();
                    dialog.IsFolderPicker = true;
                    CommonFileDialogResult result = dialog.ShowDialog();
                    if (result == CommonFileDialogResult.Ok)
                    {
                        directory = dialog.FileName;
                    }
                }
                else
                {
                    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                        if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
                        {
                            directory = System.IO.Path.GetDirectoryName(dialog.SelectedPath);
                        }
                    }
                }

                if (directory != null && Directory.Exists(directory))
                {
                    //var files = filesystemTreeViewItem.GetP4KFiles();
                    MainWindow.PrimaryWindow.ExtractFiles(filesystemEntry as P4KFilesystemEntry, mode, directory);
                }
                else
                {
                    MainWindow.SetStatus($"Failed to save {filesystemEntry.Name}");
                }

                return true;
            }
            return false;
        }

        private void FileContextMenu_Click_Extract_To(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as FrameworkElement;
            if (sender == null) return;
            var filesystemEntry = sender.DataContext as IFilesystemEntry;
            if (filesystemEntry == null) return;

            e.Handled = ExtractTo(filesystemEntry, MainWindow.ExtractionMode.Converted);
        }

        private void FileContextMenu_Click_CustomExtract_To(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as FrameworkElement;
            if (sender == null) return;
            var filesystemEntry = sender.DataContext as IFilesystemEntry;
            if (filesystemEntry == null) return;

            e.Handled = ExtractTo(filesystemEntry, MainWindow.ExtractionMode.Raw);
        }


        // fixes crazy tooltip in treeviewitems template
        private void TreeViewItem_MouseMove(object _sender, MouseEventArgs e)
        {
            var sender = _sender as TreeViewItem;

            if (sender.ToolTip != null)
            {
                switch (sender.ToolTip)
                {
                    case ToolTip tooltip:
                        tooltip.IsOpen = false;
                        break;
                    case string tooltip:
                        ToolTip new_tooltip = new ToolTip();
                        new_tooltip.Content = tooltip;
                        sender.ToolTip = new_tooltip;
                        new_tooltip.IsOpen = false;
                        break;
                }
            }
        }

        private void TreeViewItem_Expanded(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as TreeViewItem;
            var file = sender.DataContext as IFilesystemEntry;
            if (file.IsDirectory)
            {
                file.Sort();
                file.IsExpanded = true;
            }
        }

        private void ItemInitialized(object _sender, EventArgs e)
        {
            var sender = _sender as StackPanel;
            var file = sender.DataContext as IFilesystemEntry;

            switch (file)
            {
                case INotifyPropertyChanged filePropertyChangedObject:
                    filePropertyChangedObject.PropertyChanged += FilePropertyChangedObject_PropertyChanged;
                    break;
            }
        }

        private TreeViewItem FindTreeViewItemFromGenerator(ItemContainerGenerator generator, object context)
        {
            if (generator.Items.Contains(context))
            {
                return generator.ContainerFromItem(context) as TreeViewItem;
            }
            foreach (object generatorItem in generator.Items)
            {
                var treeViewItem = generator.ContainerFromItem(generatorItem) as TreeViewItem;
                if (treeViewItem != null)
                {
                    var result = FindTreeViewItemFromGenerator(treeViewItem.ItemContainerGenerator, context);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        private void FilePropertyChangedObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsExpanded")
            {
                var file = sender as IFilesystemEntry;
                var isExpanded = file?.GetType()?.GetProperty("IsExpanded")?.GetValue(sender);

                if (isExpanded != null)
                {
                    //var treeViewItem = filesystemTreeView.ItemContainerGenerator.ContainerFromItem(file) as TreeViewItem;
                    var treeViewItem = FindTreeViewItemFromGenerator(filesystemTreeView.ItemContainerGenerator, file);
                    treeViewItem.IsExpanded = (bool)isExpanded;
                }
            }
        }

        private void SharpTreeNodeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
