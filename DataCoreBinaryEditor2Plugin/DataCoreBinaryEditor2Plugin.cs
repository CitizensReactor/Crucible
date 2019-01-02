using Crucible;
using Crucible.Filesystem;
using System.Windows.Controls;

namespace DataCoreBinary2
{
    public class DataCoreBinaryEditor2Plugin : IPlugin
    {
        public static void Registration()
        {
            CrucibleApplication.RegisterFileUI(FileType.DataCoreBinary, CreateDatabaseTab);
        }

        internal static MenuItem MenuItem_Plugin_DataCoreBinary2;
        internal static MenuItem MenuItem_UseDataCoreCache;
        internal static MenuItem MenuItem_Version;

        public static void Main()
        {
            CrucibleApplication.FilesystemEntryOpen += FilesystemEntryOpen;

            MenuItem_Plugin_DataCoreBinary2 = new MenuItem();
            MenuItem_Plugin_DataCoreBinary2.Header = "DataCoreBinary";

            MenuItem_UseDataCoreCache = new MenuItem();
            MenuItem_UseDataCoreCache.Header = "Use DataCore Cache";
            MenuItem_UseDataCoreCache.IsCheckable = true;
            //TODO: Bindings?
            MenuItem_UseDataCoreCache.IsChecked = DataCoreBinaryEditor2PluginSettings.Settings.UseDatabaseCache;
            MenuItem_UseDataCoreCache.Checked += MenuItem_UseDataCoreCache_IsChecked_Changed;
            MenuItem_UseDataCoreCache.Unchecked += MenuItem_UseDataCoreCache_IsChecked_Changed;

            MenuItem_Version = new MenuItem();

            MenuItem_Plugin_DataCoreBinary2.Items.Add(MenuItem_UseDataCoreCache);
            MenuItem_Plugin_DataCoreBinary2.Items.Add(new Separator());
            MenuItem_Plugin_DataCoreBinary2.Items.Add(new MenuItem { Header = "Version 2", IsEnabled = false});

            MainWindow.PrimaryWindow.PluginsMenu.Items.Add(MenuItem_Plugin_DataCoreBinary2);
        }

        private static void MenuItem_UseDataCoreCache_IsChecked_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            DataCoreBinaryEditor2PluginSettings.Settings.UseDatabaseCache = MenuItem_UseDataCoreCache.IsChecked;
        }

        private static FilesystemEntryTab CreateDatabaseTab(IFilesystemEntry filesystemEntry)
        {
            MainWindow.SetStatus($"Loading {filesystemEntry.Name}", -1, -1, -1);
            var tab = new FilesystemEntryTab(filesystemEntry);
            tab.Content = new DatabaseFile(filesystemEntry);
            tab.Header = "DataCoreBinary Editor";
            return tab;
        }

        private static void FilesystemEntryOpen(FilesystemEntryOpenEvent e)
        {
            var tab = MainWindow.GetFilesystemEntryTab(e.FilesystemEntry);
        }
    }
}
