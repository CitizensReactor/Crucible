using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crucible.Filesystem
{
    public partial class LocalFilesystemEntry : BindableBase, IFilesystemEntry
    {
        public LocalVirtualFilesystem LocalFilesystem { get; internal set; }
        public IVirtualFilesystem Filesystem => LocalFilesystem;
        public string Filepath { get; internal set; }
        public bool IsRoot { get; }
        public bool IsDirectory { get; }
        private ObservableCollection<IFilesystemEntry> _Items;
        public ObservableCollection<IFilesystemEntry> Items { get => _Items; internal set => SetProperty(ref _Items, value); }
        public bool IsEncrypted => false;
        public string FullPath => IsDirectory ? Directory : Filepath;
        private bool _IsExpanded;
        public bool IsExpanded { get => _IsExpanded; set => SetProperty(ref _IsExpanded, value); }
        public string Namespace => "local";
        public FileType Type { get; }
        public FileInfo FileInfo { get; internal set; }
        public DirectoryInfo DirectoryInfo { get; internal set; }
        public long Size => (IsDirectory ? -1 : FileInfo?.Length) ?? -1;
        public DateTime LastModifiedDate => (IsDirectory ? DirectoryInfo?.LastWriteTime : FileInfo?.LastWriteTime) ?? new DateTime(0);

        public void Sort(bool recursive)
        {
            if (!IsDirectory) return;

            IEnumerable<IFilesystemEntry> originalItemsOrder = Items.ToArray();
            IEnumerable<IFilesystemEntry> newItemsOrder = originalItemsOrder.ToArray();
            newItemsOrder = newItemsOrder.OrderBy(c => c.Name);
            newItemsOrder = newItemsOrder.OrderBy(c => !c.IsDirectory);

            bool isOutOfOrder = false;
            for (var i = 0; i < newItemsOrder.Count(); i++)
            {
                if (originalItemsOrder.ElementAt(i) != newItemsOrder.ElementAt(i))
                {
                    isOutOfOrder = true;
                    break;
                }
            }

            if (isOutOfOrder)
            {
                Items = new ObservableCollection<IFilesystemEntry>(newItemsOrder);
            }

            if (recursive)
            {
                foreach (var file in this.Items)
                {
                    file.Sort(recursive);
                }
            }
        }

        public string _Directory = null;
        public string Directory
        {
            get => IsDirectory ? _Directory : Path.GetDirectoryName(Filepath);
            set
            {
                if (IsDirectory) _Directory = value;
                else
                {
                    //TODO: Rename directory
                    throw new NotImplementedException();
                }
            }
        }

        public string Name
        {
            get => IsDirectory ? Path.GetFileName(Directory) : Path.GetFileName(Filepath);
            set
            {
                string beforeValue = Name;

                if (IsDirectory)
                {
                    Directory = Path.Combine(Path.GetDirectoryName(Directory), value);
                }
                else Filepath = Path.Combine(Path.GetDirectoryName(Filepath), value);

                string afterValue = Name;
                if (beforeValue != afterValue) this.OnPropertyChanged();
            }
        }

        public IFilesystemEntry this[string filepath]
        {
            get
            {
                string directory = Path.GetDirectoryName(filepath);
                filepath = filepath.Replace('/', '\\');
                var searchParams = filepath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string search = searchParams[0];
                string next_search = searchParams.Length > 1 ? filepath.Substring(search.Length + 1) : null;

                foreach (var entry in Items)
                {
                    if (entry.Name.ToLowerInvariant() == search)
                    {
                        if (entry.IsDirectory)
                        {
                            return entry[next_search];
                        }
                        else
                        {
                            return entry;
                        }
                    }
                }

                return null;
            }
        }

        public static LocalFilesystemEntry CreateRootNode(LocalVirtualFilesystem LocalFilesystem)
        {
            return new LocalFilesystemEntry(LocalFilesystem);
        }

        private LocalFilesystemEntry(LocalVirtualFilesystem LocalFilesystem)
        {
            IsRoot = true;
            IsDirectory = true;

            Directory = "root";
            Items = new ObservableCollection<IFilesystemEntry>();
        }

        void AddItem(LocalFilesystemEntry LocalFilesystemEntry)
        {
            if (!IsDirectory)
            {
                throw new Exception("Can't add items to a filesystem entry");
            }

            Items.Add(LocalFilesystemEntry);
        }

        public byte[] GetData()
        {
            if (IsDirectory)
            {
                throw new Exception("Can't get data on folders");
            }


            var filePath = Path.Combine(LocalFilesystem.LocalDirectoryPath, Filepath);
            
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (MemoryStream ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                var data = ms.ToArray();
                return data;
            }
        }

        public bool SetData(byte[] data)
        {
            if (IsDirectory)
            {
                throw new Exception("Can't set data on folders");
            }

            try
            {
                var filePath = Path.Combine(LocalFilesystem.LocalDirectoryPath, Filepath);

                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(data, 0, data.Length);
                }

                return true;
            }
            catch (IOException ioException)
            {
                MainWindow.SetStatus(ioException.Message);
                return false;
            }
        }

        public LocalFilesystemEntry(LocalVirtualFilesystem localFilesystem, string path, bool isDirectory)
        {
            LocalFilesystem = localFilesystem;
            if (isDirectory)
            {
                IsRoot = false;
                IsDirectory = true;

                Directory = path;
                if (Directory.EndsWith("\\"))
                {
                    Directory = Directory.Substring(0, Directory.Length - 1);
                }

                Items = new ObservableCollection<IFilesystemEntry>();
                DirectoryInfo = new DirectoryInfo(Path.Combine(LocalFilesystem.LocalDirectoryPath, Directory));
            }
            else
            {
                IsRoot = false;
                IsDirectory = false;

                Filepath = path;
                Type = Fileconverter.FilenameToFiletype(Filepath);
                FileInfo = new FileInfo(Path.Combine(LocalFilesystem.LocalDirectoryPath, Filepath));
            }
        }
    }
}
