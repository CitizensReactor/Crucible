using Microsoft.Win32;
using Crucible.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text;
using System.Json;

namespace Crucible
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BindableWindow
    {
        private static volatile VirtualFilesystemManager _FilesystemManager = null;
        public static VirtualFilesystemManager FilesystemManager { get => _FilesystemManager; internal set => _FilesystemManager = value; }

        private DispatcherTimer SetStatusDispatcherTimer = new DispatcherTimer();

        public static MainWindow PrimaryWindow = null;

        volatile string previous_extracted_file = "";
        volatile int CurrentExtractedFileCount = 0;

        private bool _Extracting = false;
        public bool Extracting { get => _Extracting; set => SetProperty(ref _Extracting, value); }
        private bool _AllowExtracting = true;
        public bool AllowExtracting { get => _AllowExtracting; set => SetProperty(ref _AllowExtracting, value); }
        public volatile bool CancelExtraction = false;

        public MainWindow(StartupEventArgs startupEventArgs)
        {
            InitializeComponent();

            //TODO: Binding??
            StartReadOnly.IsChecked = CrucibleSettings.Settings.StartReadOnly;
            StartReadOnly.Checked += StartReadOnly_IsChecked_Changed;
            StartReadOnly.Unchecked += StartReadOnly_IsChecked_Changed;

#if DEBUG
            Icon = new BitmapImage(new Uri("pack://application:,,,/crucible_debug.ico"));
#else
            Icon = new BitmapImage(new Uri("pack://application:,,,/crucible.ico"));
#endif

            SetStatusInternal(null, 0, 0);
            SetStatusDispatcherTimer.Tick += ClearStatus;

            if (PrimaryWindow != null)
            {
                throw new Exception("A primary window already exists");
            }
            PrimaryWindow = this;

            if (startupEventArgs.Args.Length == 1)
            {
                OpenStarcitizen(startupEventArgs.Args[0]);
            }
        }

        private void CloseAllTabs()
        {
            if (!Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    CloseAllTabs();
                }));
            }

            foreach (TabItem tab in this.Tabs.Items)
            {
                CloseTab(tab, false);
            }
            this.Tabs.Items.Clear();
        }

        private void ClearView()
        {
            if (!Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    ClearView();
                }));
            }

            this.P4KFilesystemTab.Content = null;
            this.LocalFilesystemTab.Content = null;

            CloseAllTabs();
        }

        public void CloseStarcitizen()
        {
            if (!Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    CloseStarcitizen();
                }));
            }

            ClearView();

            if (FilesystemManager != null)
            {
                FilesystemManager?.Dispose();
                FilesystemManager = null;
            }

            SetTitle();
        }

        public void OpenStarcitizen(string local_directory)
        {
            if (!Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    OpenStarcitizen(local_directory);
                }));
            }

            var startTime = DateTime.Now;

            CloseStarcitizen();

            // legacy: convert p4k paths back to local
            if (!Directory.Exists(local_directory) && File.Exists(local_directory))
            {
                local_directory = System.IO.Path.GetDirectoryName(local_directory);
            }
            FilesystemManager = new VirtualFilesystemManager(local_directory);

            var loadFileSystemTask = FilesystemManager.GetInitializationTask();

            var updateProgressTask = Task.Factory.StartNew(() =>
            {

                bool running = true;
                while (running)
                {
                    switch (loadFileSystemTask.Status)
                    {
                        case TaskStatus.Running:
                            if (FilesystemManager != null)
                            {
                                var current_task_string = FilesystemManager.CurrentTaskString();
                                var current_task_progress = FilesystemManager.CurrentTaskProgress();
                                var current_task_progress_max = FilesystemManager.CurrentTaskProgressMax();
                                SetStatusInternal($"{current_task_string}", current_task_progress, current_task_progress_max, -1);
                            }
                            Thread.Sleep(10);
                            break;
                        case TaskStatus.WaitingToRun:
                            Thread.Sleep(25);
                            break;
                        default:
                            running = false;
                            break;
                    }
                }

            });

            try
            {
                //await updateProgressTask;
                updateProgressTask.Wait();
            }
            catch (Exception exception)
            {
                switch (exception)
                {
                    case FileNotFoundException fileNotFoundException:
                        SetStatusInternal($"Failed to load filesystem {local_directory}! Reason: {exception.Message}");
                        break;
                    case IOException iOException:
                        SetStatusInternal($"Failed to load filesystem {local_directory}! Reason: {exception.Message}");
                        break;
                    default:
                        SetStatusInternal($"Failed to load filesystem {local_directory}! Reason: {exception.Message}");
                        break;
                }
                return;
            }

            if (loadFileSystemTask.IsFaulted)
            {
                var exception = loadFileSystemTask.Exception?.InnerException ?? loadFileSystemTask.Exception;
                FinishedLoading(true);
                SetStatusInternal(exception.Message);
                return;
            }

            if (FilesystemManager == null)
            {
                FinishedLoading(true);
                SetStatusInternal($"Failed to load filesystem! Reason: FilesystemManager is null");
                return;
            }

            ReadPerforceInformation();

            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                this.P4KFilesystemTab.Content = new FilesystemView(FilesystemManager.P4KFilesystem);
                this.LocalFilesystemTab.Content = new FilesystemView(FilesystemManager.LocalFilesystem);
                var tabControl = this.LocalFilesystemTab.Parent as TabControl;
                if (tabControl != null) tabControl.SelectedIndex = 1;

                // Lets collect garbage because code is garbage
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (FilesystemManager.P4KFilesystem.IsReadOnly)
                {
                    SetTitle($"[Data.p4k] (Read Only)");
                }
                else
                {
                    SetTitle($"[Data.p4k]");
                }

                FinishedLoading(false);

                var deltaTime = DateTime.Now - startTime;
                SetStatusInternal(String.Format($"Finished loading filesystem {local_directory} {{0:0.00}}s", deltaTime.TotalSeconds));

            })).Wait();

            return;
        }

        internal class P4Information
        {
            public string Branch;
            public string BuildDateStamp;
            public string BuildTimeStamp;
            public string Config;
            public Int64 RequestedP4ChangeNum;
            public string Shelved_Change;
            public string Tag;
        }
        internal P4Information Perforce = null;

        void ReadPerforceInformation()
        {
            Perforce = null;

            if (FilesystemManager != null)
            {
                // attempt to find a release id
                byte[] data = null;
                data = data ?? FilesystemManager?.LocalFilesystem["f_win_game_client_release.id"]?.GetData();
                data = data ?? FilesystemManager?.P4KFilesystem["c_win_shader.id"]?.GetData();
                data = data ?? FilesystemManager?.LocalFilesystem["c_tools_crash_handler.id"]?.GetData();

                if (data != null)
                {
                    var perforceInformation = new P4Information();

                    var jsonText = Encoding.UTF8.GetString(data);
                    JsonValue jsonRoot = JsonValue.Parse(jsonText);
                    JsonValue jsonData = jsonRoot.ContainsKey("Data") ? jsonRoot["Data"] : null;

                    perforceInformation.Branch = (string)((jsonData?.ContainsKey("Branch") ?? false) ? jsonData["Branch"] : null);
                    perforceInformation.BuildDateStamp = (string)((jsonData?.ContainsKey("BuildDateStamp") ?? false) ? jsonData["BuildDateStamp"] : null);
                    perforceInformation.BuildTimeStamp = (string)((jsonData?.ContainsKey("BuildTimeStamp") ?? false) ? jsonData["BuildTimeStamp"] : null);
                    perforceInformation.Config = (string)((jsonData?.ContainsKey("Config") ?? false) ? jsonData["Config"] : null);
                    perforceInformation.Shelved_Change = (string)((jsonData?.ContainsKey("Shelved_Change") ?? false) ? jsonData["Shelved_Change"] : null);
                    perforceInformation.Tag = (string)((jsonData?.ContainsKey("Tag") ?? false) ? jsonData["Tag"] : null);

                    var requestedP4ChangeNumStr = (string)((jsonData?.ContainsKey("RequestedP4ChangeNum") ?? false) ? jsonData["RequestedP4ChangeNum"] : null);

                    if (Int64.TryParse(requestedP4ChangeNumStr, out Int64 requestedP4ChangeNum))
                    {
                        perforceInformation.RequestedP4ChangeNum = requestedP4ChangeNum;
                    }
                    else
                    {
                        perforceInformation.RequestedP4ChangeNum = 0;
                    }

                    Perforce = perforceInformation;
                }
            }
        }

        internal void SetTitle(string text = "")
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                StringBuilder builder = new StringBuilder();

                builder.Append("Crucible");
#if DEBUG
                builder.Append(" Debug");
#endif

                if (FilesystemManager != null)
                {
                    builder.Append($" | StarCitizen");

                    string versionStr = null;
                    if (Perforce != null)
                    {
                        versionStr += $" {Perforce.Branch} {Perforce.RequestedP4ChangeNum}";
                    }
                    if (versionStr != null)
                    {
                        builder.Append($" {versionStr}");
                    }
                    else
                    {
                        builder.Append($" {FilesystemManager.MajorVersion}.{FilesystemManager.MinorVersion}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    builder.Append($" {text}");
                }

                this.Title = builder.ToString().Trim();
            }));
        }

        #region Status

        public static void SetStatus()
        {
            PrimaryWindow?.SetStatusInternal(null);
        }

        public static void SetStatus(string text)
        {
            PrimaryWindow?.SetStatusInternal(text);
        }

        public static void SetStatus(string text, int time)
        {
            PrimaryWindow?.SetStatusInternal(text, time);
        }

        public static void SetStatus(string text , int value, int max_value)
        {
            PrimaryWindow?.SetStatusInternal(text, value, max_value);
        }

        public static void SetStatus(string text, int value, int max_value, int time)
        {
            PrimaryWindow?.SetStatusInternal(text, value, max_value, time);
        }

        private void SetStatusInternal(string text)
        {
            SetStatusInternal(text, -1, -1, 3);
        }

        private void SetStatusInternal(string text, int time)
        {
            SetStatusInternal(text, -1, -1, time);
        }

        private void SetStatusInternal(string text, int value, int max_value)
        {
            SetStatusInternal(text, value, max_value, 3);
        }

        private void SetStatusInternal(string text, int value, int max_value, int time)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (time > 0)
                {
                    SetStatusDispatcherTimer.Stop();
                    SetStatusDispatcherTimer.Interval = new TimeSpan(0, 0, time);
                    SetStatusDispatcherTimer.Start();
                }

                if (text != null)
                {
                    TextBlockStatus.Visibility = Visibility.Visible;
                    TextBlockStatus.Text = text;
                }

                if (value != 0 || max_value != 0)
                {
                    ProgressBarMain.Visibility = Visibility.Visible;
                    ProgressBarMain.Minimum = 0;
                    ProgressBarMain.Maximum = max_value;
                    ProgressBarMain.Value = value;

                    ProgressBarMain.IsIndeterminate = value == -1 || max_value == -1;
                }

                if (text == null && value <= 0 && max_value <= 0)
                {
                    TextBlockStatus.Visibility = Visibility.Collapsed;
                    ProgressBarMain.Visibility = Visibility.Collapsed;
                }
                else if (value <= 0 && max_value <= 0)
                {
                    ProgressBarMain.Visibility = Visibility.Collapsed;
                }
            }));
        }

        internal void ClearStatus(object sender, EventArgs e)
        {
            SetStatusInternal(null, 0, 0, 0);
            SetStatusDispatcherTimer.Stop();
        }

        #endregion

        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FinishedLoading(bool failure)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (failure)
                {
                    SetTitle();
                }
            }));
        }

        private void Menu_Open_Click(object _sender, RoutedEventArgs e)
        {
            var sender = _sender as MenuItem;
            if (!(sender?.IsEnabled ?? false)) return;

            if (FilesystemManager != null)
            {
                FilesystemManager.Dispose();
                FilesystemManager = null;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Package container files (*.p4k)|*.p4k|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() != true) return;

            OpenStarcitizen(openFileDialog.FileName);
        }

        private void Menu_Close_Click(object sender, RoutedEventArgs e)
        {
            CloseStarcitizen();
        }

        private void Menu_Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
        {

        }

        public void CloseTab(TabItem tab, bool remove = true)
        {
#if DEBUG
            if (!Tabs.Items.Contains(tab))
            {
                throw new Exception("Tab not contained in main window");
            }
#endif
            var disposable = tab as IDisposable;
            disposable?.Dispose();
            if (remove)
            {
                Tabs.Items.Remove(tab);
            }
        }

        public void CloseTabByDataContext(object context)
        {
            foreach (TabItem tab in Tabs.Items)
            {
                if (tab.DataContext == context)
                {
                    CloseTab(tab);
                    return;
                }
            }
        }

        public static TabItem GetFilesystemEntryTab(IFilesystemEntry filesystemEntry)
        {
            return PrimaryWindow?.GetFilesystemEntryTabInternal(filesystemEntry);
        }

        private TabItem GetFilesystemEntryTabInternal(IFilesystemEntry filesystemEntry)
        {
            foreach (TabItem tab in Tabs.Items)
            {
                if (tab.DataContext == filesystemEntry)
                {
                    return tab;
                }
            }

            return null;
        }

        public void OpenFileTab(IFilesystemEntry filesystemEntry)
        {
            PrimaryWindow?.OpenFileTabInternal(filesystemEntry);
        }

        private void OpenFileTabInternal(IFilesystemEntry filesystemEntry)
        {
            try
            {

                if (filesystemEntry == null) return;

                var existingTab = GetFilesystemEntryTab(filesystemEntry);
                if (existingTab != null)
                {
                    Tabs.SelectedItem = existingTab;
                    return;
                }

                if (FileTypeChecker.IsExtensionDDS(filesystemEntry))
                {
                    //TODO
                    MainWindow.SetStatus("Can't open DDS files just yet sorry :(");
                    return;
                }

                var contentItems = CrucibleApplication.GetFileUITabs(filesystemEntry);

                if (contentItems.Count == 1)
                {
                    var tab = contentItems[0];
                    var oldHeader = tab.Header;
                    tab.Header = filesystemEntry.Name;
                    if (string.IsNullOrWhiteSpace(tab.ToolTip as string))
                    {
                        tab.ToolTip = oldHeader;
                    }

                    tab.DataContext = filesystemEntry;
                    Tabs.Items.Add(tab);
                    CrucibleApplication.OnOpenFile(filesystemEntry);
                    FocusManager.SetFocusedElement(this, tab);
                }
                else if (contentItems.Count > 0)
                {
                    TabControl tabControl = new TabControl();

                    foreach (var childTab in contentItems)
                    {
                        tabControl.Items.Add(childTab);
                    }

                    var tab = new FilesystemEntryTab(filesystemEntry);
                    tab.Header = filesystemEntry.Name;
                    tab.Content = tabControl;
                    tab.DataContext = filesystemEntry;

                    Tabs.Items.Add(tab);
                    CrucibleApplication.OnOpenFile(filesystemEntry);
                    FocusManager.SetFocusedElement(this, tab);
                }
                else
                {
                    System.Media.SystemSounds.Beep.Play();
                    SetStatusInternal($"No supported interfaces for {filesystemEntry.FullPath}");
                }
            }
            catch (Exception e)
            {
                this.SetStatusInternal(e.Message);
            }
        }

        public enum ExtractionMode
        {
            Raw,
            Binary,
            Converted
        }

        private void QueueFiles(List<P4KFilesystemEntry> queue, P4KFilesystemEntry entry)
        {
            if (entry.IsDirectory || entry.IsRoot)
            {
                //queue.AddRange(entry.Items.Cast<P4KFilesystemEntry>());
                foreach (P4KFilesystemEntry childEntry in entry.Items)
                {
                    if (childEntry.IsDirectory)
                    {
                        QueueFiles(queue, childEntry);
                    }
                    else
                    {
                        queue.Add(childEntry);
                    }
                }
            }
            else
            {
                queue.Add(entry);
            }
        }

        public void ExtractFiles(IFilesystemEntry sourceEntry, ExtractionMode extractionMode = ExtractionMode.Converted, string path = null)
        {
            var _sourceEntry = sourceEntry as P4KFilesystemEntry;
            if (_sourceEntry == null) return;

            lock (this)
            {
                if (!AllowExtracting)
                {
                    return;
                }
                AllowExtracting = false;
                Extracting = true;
            }

            CancelExtraction = false;

            P4KFilesystemEntry[] queue = null;
            {
                List<P4KFilesystemEntry> queueList = new List<P4KFilesystemEntry>();
                QueueFiles(queueList, _sourceEntry);
                queue = queueList.ToArray();
            }

            int totalFileCount = queue.Length;
            CurrentExtractedFileCount = 0;


#if !DEBUG
            Task extractTask = Task.Factory.StartNew(() =>
            {
                Parallel.ForEach(queue, delegate (P4KFilesystemEntry file)
#endif
#if DEBUG
            Task extractTask = Task.CompletedTask;
            if (true)
            {
                Parallel.ForEach(queue, delegate (P4KFilesystemEntry file)
#endif
                {
                    if (CancelExtraction) return;

                    // skip potentially null files (this shouldn't really happen though)
                    if (file == null)
                    {
                        return;
                    }

                    string fullPath;
                    var baseDirectory = path ?? FilesystemManager.DirectoryName;
                    if (_sourceEntry.IsDirectory)
                    {
                        fullPath = System.IO.Path.Combine(baseDirectory, file.FullPath);
                    }
                    else
                    {
                        fullPath = path ?? System.IO.Path.Combine(baseDirectory, file.FullPath);
                    }
                    var fullDirectory = System.IO.Path.GetDirectoryName(fullPath);

                    byte[] data = null;

                    switch (extractionMode)
                    {
                        case ExtractionMode.Raw:
                            {
                                data = file.GetRawData();
                            }
                            break;
                        case ExtractionMode.Binary:
                            {
                                data = file.GetData();
                            }
                            break;
                        case ExtractionMode.Converted:
                            {
                                switch (file.Type)
                                {
                                    case FileType.DDSChild:
                                        break;
                                    case FileType.DDS:
                                        DDSConverter.SaveDDS(file, path);
                                        break;
                                    default:
                                        {
                                            data = file.GetData();

                                            //TODO: Make optional
                                            var filetype = Fileconverter.FilenameToFiletype(file.FullPath);
                                            data = Fileconverter.ConvertFile(data, filetype);
                                        }
                                        break;
                                }
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    if (data != null)
                    {
                        Directory.CreateDirectory(fullDirectory);
                        using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(data, 0, data.Length);
                        }
                    }

                    CurrentExtractedFileCount++;
                    previous_extracted_file = file.FullPath;

                });
#if !DEBUG
            });
#endif
#if DEBUG
            }
#endif


            Task updateTask = Task.Factory.StartNew(() =>
            {

                try
                {
                    while (!extractTask.IsCompleted)
                    {

                        if (extractTask.IsFaulted) break;

                        if (CancelExtraction)
                        {
                            SetStatusInternal($"Cancelling extraction...", CurrentExtractedFileCount, totalFileCount, -1);
                        }
                        else
                        {
                            SetStatusInternal($"Extracting {previous_extracted_file}", CurrentExtractedFileCount, totalFileCount, -1);
                        }
                        Thread.Sleep(5);
                    }

                    if (extractTask.IsFaulted)
                    {
                        SetStatusInternal($"Extraction update error {extractTask.Exception.InnerException?.Message ?? extractTask.Exception.Message}");
                    }
                    else if (totalFileCount == 1)
                    {
                        SetStatusInternal($"Extracted {_sourceEntry.FullPath}");
                    }
                    else
                    {
                        SetStatusInternal($"Finished extracting {_sourceEntry.FullPath}");
                    }
                }
                catch (Exception e)
                {
                    SetStatusInternal($"Extraction update error {e.Message}");
                }
                finally
                {
                    lock (this)
                    {
                        if (CancelExtraction)
                        {
                            SetStatusInternal($"Extraction cancelled");
                        }

                        AllowExtracting = true;
                        Extracting = false;
                    }
                }
            });
        }

        private void ExtractClick(object sender, RoutedEventArgs e)
        {
            ExtractFiles(FilesystemManager.P4KFilesystem.RootDirectory as P4KFilesystemEntry);
        }

        private void ExtractionCancel(object sender, RoutedEventArgs e)
        {
            CancelExtraction = true;
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
            this.Close();
        }

        private void Goto_Reddit(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.reddit.com/r/CitizensReactor");
        }

        private void Goto_Discord(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/3Hq4uM7");
        }

        private void ManagePlugins(object sender, RoutedEventArgs e)
        {

        }

        private void ViewAbout(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void MenuItem_CloseAllTabs(object sender, RoutedEventArgs e)
        {
            CloseAllTabs();
        }

        private void NewCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (FilesystemManager != null)
            {
                FilesystemManager.Dispose();
                FilesystemManager = null;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Package container files (*.p4k)|*.p4k|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() != true) return;

            OpenStarcitizen(openFileDialog.FileName);
        }

        private void SaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        public static RoutedCommand SaveAllRoutedCommand = new RoutedCommand();

        private void SaveAllCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void StartReadOnly_IsChecked_Changed(object sender, RoutedEventArgs e)
        {
            CrucibleSettings.Settings.StartReadOnly = (sender as MenuItem).IsChecked;
        }

        private void AssociateFiletypes_Click(object sender, RoutedEventArgs e)
        {
            FileAssociations.EnsureAssociationsSet();
        }
    }
}
