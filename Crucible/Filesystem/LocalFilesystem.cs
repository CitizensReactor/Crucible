using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Crucible.Filesystem;

namespace Crucible.Filesystem
{
    public class LocalVirtualFilesystem : IVirtualFilesystem
    {
        [DllImport("shlwapi.dll", EntryPoint = "PathRelativePathTo")]
        protected static extern bool PathRelativePathTo(StringBuilder lpszDst, string from, UInt32 attrFrom, string to, UInt32 attrTo);

        public VirtualFilesystemManager FilesystemManager { get; }

        public string LocalDirectoryPath { get; }
        DirectoryInfo LocalDirectoryInfo { get; }

        public List<IFilesystemEntry> Files { get; internal set; } = new List<IFilesystemEntry>();
        public List<IFilesystemEntry> Folders { get; internal set; } = new List<IFilesystemEntry>();
        public Dictionary<string, IFilesystemEntry> FolderLookup = new Dictionary<string, IFilesystemEntry>(StringComparer.OrdinalIgnoreCase);
        public IFilesystemEntry RootDirectory { get; internal set; } = null;

        public IFilesystemEntry this[string path] => RootDirectory[path];

        FileSystemWatcher FileWatcher = null;
        FileSystemWatcher FolderWatcher = null;

        private static string GetRelativePath(string from, string to, bool toIsDirectory)
        {
            if (!from.EndsWith("\\")) from += "\\";
            if (toIsDirectory)
            {
                if (!to.EndsWith("\\")) to += "\\";
            }

            StringBuilder builder = new StringBuilder(1024);
            PathRelativePathTo(builder, from, 0, to, 0);
            var result = builder.ToString();

            if (result.StartsWith(".\\")) result = result.Substring(2);

            return result;
        }

        public LocalFilesystemEntry IterateDirectory(DirectoryInfo directoryInfo)
        {
            var directoryRelativePath = GetRelativePath(LocalDirectoryPath, directoryInfo.FullName, true);
            LocalFilesystemEntry directory = new LocalFilesystemEntry(this, directoryRelativePath, true);

            foreach (var childDirectoryInfo in directoryInfo.EnumerateDirectories())
            {
                var childDirectory = IterateDirectory(childDirectoryInfo);
                directory.Items.Add(childDirectory);
            }

            foreach (var childFileInfo in directoryInfo.EnumerateFiles())
            {
                var childFileRelativePath = GetRelativePath(LocalDirectoryPath, childFileInfo.FullName, false);
                var childFile = new LocalFilesystemEntry(this, childFileRelativePath, false);
                {
                    var filepath_test = Path.Combine(LocalDirectoryPath, childFile.FullPath);
                    if (!File.Exists(filepath_test))
                    {
                        throw new Exception("Invalid filepaths");
                    }
                }
                directory.Items.Add(childFile);
                Files.Add(childFile);
            }

            Folders.Add(directory);
            FolderLookup[directory.FullPath] = directory;
            return directory;
        }

        public void CreateWatcher()
        {
            if (FileWatcher != null)
            {
                FileWatcher.Dispose();
            }
            FileWatcher = new FileSystemWatcher();
            FileWatcher.NotifyFilter =
                NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.FileName |
                NotifyFilters.LastAccess |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.Security;
            FileWatcher.Path = this.LocalDirectoryPath;
            FileWatcher.Filter = "*.*";
            FileWatcher.Changed += Watcher_Event_File;
            FileWatcher.Created += Watcher_Event_File;
            FileWatcher.Renamed += Watcher_Event_File;
            FileWatcher.Deleted += Watcher_Event_File;
            FileWatcher.EnableRaisingEvents = true;
            FileWatcher.IncludeSubdirectories = true;

            if (FolderWatcher != null)
            {
                FolderWatcher.Dispose();
            }
            FolderWatcher = new FileSystemWatcher();
            FolderWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            FolderWatcher.Path = this.LocalDirectoryPath;
            FolderWatcher.Filter = "*.*";
            FolderWatcher.Changed += Watcher_Event_Folder;
            FolderWatcher.Created += Watcher_Event_Folder;
            FolderWatcher.Renamed += Watcher_Event_Folder;
            FolderWatcher.Deleted += Watcher_Event_Folder;
            FolderWatcher.EnableRaisingEvents = true;
            FolderWatcher.IncludeSubdirectories = true;
        }

        public LocalVirtualFilesystem(VirtualFilesystemManager virtualFilesystemManager, string localDirectoryPath)
        {
            FilesystemManager = virtualFilesystemManager;

            LocalDirectoryPath = localDirectoryPath;

            LocalDirectoryInfo = new DirectoryInfo(LocalDirectoryPath);

            RootDirectory = IterateDirectory(LocalDirectoryInfo);

            CreateWatcher();
        }

        private void Watcher_Event_Ext(object sender, FileSystemEventArgs e, bool isDirectory)
        {
            RenamedEventArgs renamedEventArgs = e as RenamedEventArgs;
            string FullPath = e.ChangeType == WatcherChangeTypes.Renamed ? renamedEventArgs.OldFullPath : e.FullPath;
            string NewFullPath = e.ChangeType == WatcherChangeTypes.Renamed ? renamedEventArgs.FullPath : null;

            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Changed:
                    FileAttributes attr = File.GetAttributes(FullPath);
                    isDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;
                    break;
                case WatcherChangeTypes.Renamed:
                case WatcherChangeTypes.Deleted:
                    // rely on event
                    break;
                default:
                    throw new NotImplementedException();
            }

            var directory = new DirectoryInfo(Path.GetDirectoryName(FullPath)).FullName;
            var relativeDirectory = GetRelativePath(LocalDirectoryPath, directory, true);
            var relativePath = GetRelativePath(LocalDirectoryPath, FullPath, isDirectory);

            if (relativeDirectory.EndsWith("\\"))
            {
                relativeDirectory = relativeDirectory.Substring(0, relativeDirectory.Length - 1);
            }
            if (relativePath.EndsWith("\\"))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - 1);
            }

            // useful helper, can be included or discarded
            var name = Path.GetFileName(e.FullPath);
            var filesystemEntry = new LocalFilesystemEntry(this, relativePath, isDirectory);


            MainWindow.PrimaryWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                lock (this)
                {
                    switch (e.ChangeType)
                    {
                        case WatcherChangeTypes.Created:
                            if (!FolderLookup.ContainsKey(relativeDirectory))
                            {
                                var lastFolder = FolderLookup[""];
                                var currentFolderPath = "";
                                var folderNames = relativeDirectory.Split('\\');

                                foreach(var folderName in folderNames)
                                {
                                    if(string.IsNullOrWhiteSpace(currentFolderPath))
                                    {
                                        currentFolderPath = folderName;
                                    }
                                    else
                                    {
                                        currentFolderPath += $"\\{folderName}";
                                    }

                                    if(FolderLookup.ContainsKey(currentFolderPath))
                                    {
                                        lastFolder = FolderLookup[currentFolderPath];
                                    }
                                    else
                                    {
                                        IFilesystemEntry entry = new LocalFilesystemEntry(this, currentFolderPath, true);
                                        lastFolder.Items.Add(entry);
                                        FolderLookup[entry.FullPath] = entry;
                                        lastFolder = entry;
                                    }
                                }

                                if(!FolderLookup.ContainsKey(relativeDirectory))
                                {
                                    throw new Exception("The fuck?");
                                }
                            }

                            if (FolderLookup.ContainsKey(relativeDirectory))
                            {
                                var folder = FolderLookup[relativeDirectory];

                                if (isDirectory)
                                {
                                    FolderLookup[filesystemEntry.FullPath] = filesystemEntry;
                                    Folders.Add(filesystemEntry);
                                    CreateWatcher();
                                }
                                else
                                {
                                    Files.Add(filesystemEntry);
                                }
                                folder.Items.Add(filesystemEntry);
                            }
                            else throw new Exception("Folder should always be found!");
                            break;
                        case WatcherChangeTypes.Changed:
                            break;
                        case WatcherChangeTypes.Renamed:

                            var newRelativePath = GetRelativePath(LocalDirectoryPath, NewFullPath, isDirectory);
                            var newFilesystemEntry = new LocalFilesystemEntry(this, newRelativePath, isDirectory);

                            if (FolderLookup.ContainsKey(relativeDirectory))
                            {
                                var existingParentFolder = FolderLookup[relativeDirectory];

                                if (isDirectory)
                                {
                                    var existingFolder = FolderLookup[filesystemEntry.FullPath];
                                    FolderLookup.Remove(filesystemEntry.FullPath);
                                    existingFolder.Name = newFilesystemEntry.Name;
                                    FolderLookup[existingFolder.FullPath] = existingFolder;
                                }
                                else
                                {
                                    IFilesystemEntry existingFile = null;
                                    foreach (var entry in existingParentFolder.Items)
                                    {
                                        if (entry.Name == filesystemEntry.Name)
                                        {
                                            existingFile = entry;
                                        }
                                    }
                                    if (existingFile == null) throw new Exception("File should always be found!");


                                }
                            }
                            else throw new Exception("Folder should always be found!");

                            break;
                        case WatcherChangeTypes.Deleted:

                            if (FolderLookup.ContainsKey(relativeDirectory))
                            {
                                var existingParentFolder = FolderLookup[relativeDirectory];

                                if (isDirectory)
                                {
                                    var existingFolder = FolderLookup[filesystemEntry.FullPath];

                                    // TODO: just lookup parent, its faster!
                                    foreach (var folder in Folders)
                                    {
                                        folder.Items.Remove(existingFolder);
                                    }
                                    Folders.Remove(existingFolder);

                                    FolderLookup.Remove(filesystemEntry.FullPath);
                                }
                                else
                                {
                                    IFilesystemEntry existingFile = null;
                                    foreach (var entry in existingParentFolder.Items)
                                    {
                                        if (entry.Name == filesystemEntry.Name)
                                        {
                                            existingFile = entry;
                                        }
                                    }
                                    if (existingFile != null)
                                    {
                                        existingParentFolder.Items.Remove(existingFile);
                                    }
                                    else
                                    {
                                        //TODO: figure out why sometimes files aren't found
                                        //throw new Exception("File should always be found!");
                                    }
                                }
                            }
                            else throw new Exception("Folder should always be found!");

                            break;

                        case WatcherChangeTypes.All:
                            break;
                    }
                }
            }));
        }

        private void Watcher_Event_File(object sender, FileSystemEventArgs e)
        {
            Watcher_Event_Ext(sender, e, false);
        }

        private void Watcher_Event_Folder(object sender, FileSystemEventArgs e)
        {
            Watcher_Event_Ext(sender, e, true);
        }
    }
}
