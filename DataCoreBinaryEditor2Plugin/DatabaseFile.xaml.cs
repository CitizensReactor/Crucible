using Crucible;
using Crucible.Filesystem;
using DataCore2;
using DataCoreBinary2.FieldEditors;
using DataCoreBinary2.Sidebar;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DataCoreBinary2
{
    /// <summary>
    /// Interaction logic for DatabaseFile.xaml
    /// </summary>
    internal partial class DatabaseFile : UserControl
    {
        public IFilesystemEntry File { get; set; }

        public DatabaseFile(IFilesystemEntry file)
        {
            File = file;

            InitializeComponent();

            LoadDatabaseFile();
        }

        List<DatabaseRecordSearchResult> DatabaseRecordSearchResults;
        private void databaseRecordsSearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(databaseRecordsSearchBar.Text))
            {
                // clear all of the items because the control isn't visible
                databaseRecordsSearchResults.ItemsSource = new List<DatabaseRecordSearchResult>();
            }
            else
            {
                List<DatabaseRecordSearchResult> temp_list = new List<DatabaseRecordSearchResult>();
                foreach (var DatabaseRecordSearchResult in DatabaseRecordSearchResults)
                {
                    var isVisible = DatabaseRecordSearchResult.SearchString.IndexOf(databaseRecordsSearchBar.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isVisible)
                    {
                        temp_list.Add(DatabaseRecordSearchResult);
                    }
                }
                temp_list.Sort((emp1, emp2) => emp1.Title.CompareTo(emp2.Title));
                databaseRecordsSearchResults.ItemsSource = temp_list;
            }
        }

        private void ClearView()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                databaseRecordsTreeView.ItemsSource = new List<DatabaseRecordTreeViewItem>();
                databaseRecordsSearchResults.ItemsSource = new List<DatabaseRecordSearchResult>();

                databaseRecordsSearchBar.IsEnabled = false;
            }));
        }

        private void FinishedLoading()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                databaseRecordsSearchBar.IsEnabled = true;

            }));
        }

        DataCoreDatabase Database = null;

        private void LoadDatabaseFile()
        {
#if !DEBUG
            try
            {
#endif
                ClearView();

                MainWindow.SetStatus($"Loading database {File.Name}", 0, 0, -1);

                var startTime = DateTime.Now;
                var statusTask = Task.Factory.StartNew(() =>
                {
#if !DEBUG
                    try
                    {
#endif

                    Task databaseRecordsTask;

                        var gameDatabaseFile = File ?? MainWindow.FilesystemManager.P4KFilesystem["Data\\Game.dcb"];
                        var data = gameDatabaseFile.GetData();

                        var localDirectory = Path.Combine(CrucibleApplication.CurrentProjectDirectory, "datacore");
                        Directory.CreateDirectory(localDirectory);
                        Database = new DataCoreDatabase(
                        MainWindow.FilesystemManager.MajorVersion,
                        MainWindow.FilesystemManager.MinorVersion,
                        MainWindow.FilesystemManager.RevisionVersion,
                        MainWindow.FilesystemManager.BuildVersion,
                        localDirectory,
                        data,
                        DataCoreBinaryEditor2PluginSettings.Settings.UseDatabaseCache ?  DataCoreDatabase.CacheMode.UseCache : DataCoreDatabase.CacheMode.NoCache
                    );

                    // setup database
                    //DatabaseRecordTreeViewItem databaseDefinitionsTreeViewRoot = new DatabaseRecordTreeViewItem(Database, null);
                    DatabaseRecordTreeViewItem databaseRecordsTreeViewRoot = new DatabaseRecordTreeViewItem(Database, null);
                        DatabaseRecordSearchResults = new List<DatabaseRecordSearchResult>();


                        databaseRecordsTask = Task.Factory.StartNew(() =>
                    {
                        Dictionary<Type, DatabaseRecordTreeViewItem> recordTreeStructureGroups = new Dictionary<Type, DatabaseRecordTreeViewItem>();

                    // Database Tree
                    foreach (var recordKeyPair in Database.ManagedGUIDTable)
                        {
                            var record = recordKeyPair.Value;
                            var type = record.StructureType;

                            if (!recordTreeStructureGroups.ContainsKey(type))
                            {
                                DatabaseRecordTreeViewItem databaseMenuItem = new DatabaseRecordTreeViewItem(Database, null);
                                databaseMenuItem.Title = type.Name;
                                databaseRecordsTreeViewRoot.Items.Add(databaseMenuItem);
                                recordTreeStructureGroups[record.StructureType] = databaseMenuItem;
                            }
                        }

                        foreach (var recordKeyPair in Database.ManagedGUIDTable)
                        {
                            var record = recordKeyPair.Value;
                            var rootMenuItem = recordTreeStructureGroups[record.StructureType];

                            DatabaseRecordTreeViewItem databaseMenuItem = new DatabaseRecordTreeViewItem(Database, record);
                            databaseMenuItem.Record = record;
                            rootMenuItem.Items.Add(databaseMenuItem);
                        }

                    // Database Search
                    foreach (var recordKeyPair in Database.ManagedGUIDTable)
                        {
                            var record = recordKeyPair.Value;
                            DatabaseRecordSearchResult DatabaseRecordSearchResult = new DatabaseRecordSearchResult(Database, record);
                            DatabaseRecordSearchResults.Add(DatabaseRecordSearchResult);
                        }

                        databaseRecordsTreeViewRoot.Sort(true);
                    });

                    //Task.WaitAll(new Task[] { databaseTypesTask, databaseRecordsTask });
                    databaseRecordsTask.Wait();


                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                    {
                    //databaseDefinitionsTreeView.ItemsSource = databaseDefinitionsTreeViewRoot.Items;
                    databaseRecordsTreeView.ItemsSource = databaseRecordsTreeViewRoot.Items;
                        databaseRecordsSearchResults.ItemsSource = new List<DatabaseRecordSearchResult>(); // leave this empty, search will populate

                    // Lets collect garbage because code is garbage
                    GC.Collect();
                        GC.WaitForPendingFinalizers();

                        var deltaTime = DateTime.Now - startTime;
                        MainWindow.SetStatus(String.Format($"Finished loading {File.Name} {{0:0.00}}s", deltaTime.TotalSeconds));

                        FinishedLoading();

                    //var character = databaseRecordsTreeViewRoot?.Items?.Where(item => item.Title.ToLowerInvariant() == "acpickupslist")?.FirstOrDefault();
                    //var chatacter_admin = character?.Items.Where(item => item.Title.ToLowerInvariant() == "competitivepickupslist")?.FirstOrDefault();
                    //if (chatacter_admin != null)
                    //{
                    //    OpenDatabaseRecord(chatacter_admin);
                    //}
#if DEBUG
                    var entityclassdefinition = databaseRecordsTreeViewRoot?.Items?.Where(item => item.Title.ToLowerInvariant() == "entityclassdefinition")?.FirstOrDefault();
                    var aegs_idris = entityclassdefinition?.Items.Where(item => item.Title.ToLowerInvariant() == "aegs_idris")?.FirstOrDefault();
                    if (aegs_idris != null)
                    {
                        OpenDatabaseRecord(aegs_idris);
                    }
#endif

                })).Wait();
                        return;
#if !DEBUG
                    }
                    catch (Exception e)
                    {
                        MainWindow.SetStatus($"Failed to load {File.FullPath}! Reason: {e.Message}");
                        FinishedLoading();
                    }
#endif
            });
#if !DEBUG
            }
            catch (Exception e)
            {
                MainWindow.SetStatus($"Failed to load {File.FullPath}! Reason: {e.Message}");
                FinishedLoading();
            }
#endif
        }

        public void OpenDatabaseRecord(DataCoreDatabase dataCore, DataCoreRecord record)
        {
            foreach (TabItem existing_tab in Tabs.Items)
            {
                if (record.ID == existing_tab.DataContext as Guid?)
                {
                    Tabs.SelectedValue = existing_tab;
                    return;
                }
            }

            TabItem tab = new TabItem();

            var name = record.Name;
            tab.Header = name;
            tab.DataContext = record.ID;

            var closable_tab_style = this.FindResource("ClosableTab");
            tab.Style = closable_tab_style as Style;

            tab.Content = new DatabaseStructureView(dataCore, record.Instance, true);

            Tabs.Items.Add(tab);
            Tabs.SelectedItem = tab;
            tab.IsSelected = true;
        }

        private void OpenDatabaseRecord(DatabaseRecordTreeViewItem context)
        {
            if (context.IsRecord)
            {
                OpenDatabaseRecord(context.Database, context.Record);
            }
        }

        private void ContentControl_MouseDoubleClick(object _sender, MouseButtonEventArgs e)
        {
            var sender = _sender as FrameworkElement;
            var context = sender?.DataContext;
            if (context == null) return;

            switch (context)
            {
                case DatabaseRecordTreeViewItem databaseRecordTreeViewItem:
                    if (databaseRecordTreeViewItem.IsRecord)
                        OpenDatabaseRecord(databaseRecordTreeViewItem.Database, databaseRecordTreeViewItem.Record);
                    break;
                case DatabaseRecordSearchResult databaseRecordTreeViewItem:
                    //if (databaseRecordTreeViewItem.IsRecord)
                    OpenDatabaseRecord(databaseRecordTreeViewItem.Database, databaseRecordTreeViewItem.Record);
                    break;
            }



            e.Handled = true;
        }

        private void Temporary_Save_Button(object sender, RoutedEventArgs e)
        {
            MainWindow.SetStatus($"Compiling database...");
            var data = Database.GetDatabaseBinary();

            string filepath = null;
            if (CommonSaveFileDialog.IsPlatformSupported)
            {
                var dialog = new CommonSaveFileDialog();
                dialog.DefaultFileName = "Game.dcb";
                CommonFileDialogResult result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    filepath = dialog.FileName;
                }
            }
            else
            {
                var dialog = new SaveFileDialog();
                dialog.FileName = "Game.dcb";
                if (dialog.ShowDialog() ?? false)
                {
                    filepath = dialog.FileName;
                }
            }

            if (filepath != null)
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {

                    fs.Write(data, 0, data.Length);
                }
                MainWindow.SetStatus($"Saved {filepath}");
            }
            else
            {
                MainWindow.SetStatus($"Failed to save database");
            }

        }
    }
}
