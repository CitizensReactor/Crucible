using P4KLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Crucible.Filesystem
{
    internal partial class P4KFilesystemEntry : BindableBase, IFilesystemEntry
    {
        private P4KVFS InternalVirtualFilesystem = null;
        private P4KFile InternalVirtualFile = null;
        public P4KVirtualFilesystem P4KFilesystem { get; internal set; }
        public IVirtualFilesystem Filesystem => P4KFilesystem;
        public bool IsRoot { get; }
        public bool IsDirectory { get; }
        private ObservableCollection<IFilesystemEntry> _Items;
        public ObservableCollection<IFilesystemEntry> Items { get => _Items; internal set => SetProperty(ref _Items, value); }
        public bool IsEncrypted => InternalVirtualFile?.centralDirectory?.Extra?.IsAesCrypted ?? false;
        public string FullPath => IsDirectory ? Directory : InternalVirtualFile.Filepath;
        private bool _IsExpanded;
        public bool IsExpanded { get => _IsExpanded; set => SetProperty(ref _IsExpanded, value); }
        public string Namespace => "p4k";
        public FileType Type { get; }
        public long Size
        {
            get
            {
                if (IsDirectory)
                {
                    return -1;
                }
                switch (InternalVirtualFile.FileStructure.CompressionMode)
                {
                    case FileStructure.FileCompressionMode.Uncompressed:
                        return InternalVirtualFile.FileStructure.UncompressedSize;
                    case FileStructure.FileCompressionMode.ZStd:
                        return InternalVirtualFile.FileStructure.Extra.UncompressedFileLength;
                }
                return -1;
            }
        }

        public DateTime LastModifiedDate
        {
            get
            {
                if(this.IsDirectory)
                {
                    return new DateTime(0);
                }

                UInt16 dosTime = InternalVirtualFile.FileStructure.ModificationTime;
                UInt16 dosDate = InternalVirtualFile.FileStructure.ModificationDate;

                if (dosTime == 0 && dosDate == 0)
                {
                    return new DateTime(0);
                }

                var result = CrucibleUtil.ConvertDosDateTime(dosDate, dosTime);
                return result;
            }
        }

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
            get => IsDirectory ? _Directory : Path.GetDirectoryName(InternalVirtualFile.Filepath);
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
            get => IsDirectory ? Path.GetFileName(Directory) : InternalVirtualFile.Filename;
            set
            {
                string beforeValue = Name;

                if (IsDirectory)
                {
                    Directory = Path.Combine(Path.GetDirectoryName(Directory), value);
                }
                else InternalVirtualFile.Filename = value;

                string afterValue = Name;
                if (beforeValue != afterValue) this.OnPropertyChanged();
            }
        }

        public IFilesystemEntry this[string filepath]
        {
            get
            {
                ////string directory = Path.GetDirectoryName(filepath);
                //filepath = filepath.Replace('/', '\\');
                //var searchQueries = filepath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                //string search = searchQueries[0];
                //string next_search = searchQueries.Length > 1 ? filepath.Substring(search.Length + 1) : null;

                string directory = Path.GetDirectoryName(filepath);
                filepath = filepath.Replace('/', '\\');
                var searchParams = filepath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string search = searchParams[0];
                string next_search = searchParams.Length > 1 ? filepath.Substring(search.Length + 1) : null;

                foreach (var entry in Items)
                {
                    if (string.Equals(entry.Name, search, StringComparison.OrdinalIgnoreCase))
                    {
                        if (entry.IsDirectory)
                        {
                            if (next_search == null)
                            {
                                return entry;
                            }
                            else
                            {
                                return entry[next_search];
                            }
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

        public static P4KFilesystemEntry CreateRootNode(P4KVirtualFilesystem p4kFilesystem)
        {
            return new P4KFilesystemEntry(p4kFilesystem);
        }

        private P4KFilesystemEntry(P4KVirtualFilesystem p4kFilesystem)
        {
            IsRoot = true;
            IsDirectory = true;

            Directory = "root";
            Items = new ObservableCollection<IFilesystemEntry>();
        }

        public P4KFilesystemEntry(P4KVirtualFilesystem p4kFilesystem, string directoryname)
        {
            IsRoot = false;
            IsDirectory = true;

            Directory = directoryname;
            Items = new ObservableCollection<IFilesystemEntry>();
        }

        void AddItem(P4KFilesystemEntry p4KFilesystemEntry)
        {
            if (!IsDirectory)
            {
                throw new Exception("Can't add items to a filesystem entry");
            }

            Items.Add(p4KFilesystemEntry);
        }

        public byte[] GetRawData()
        {
            if (IsDirectory)
            {
                throw new Exception("Can't set data on folders");
            }

            return InternalVirtualFile.GetRawData();
        }

        public byte[] GetData()
        {
            return GetData(true);
        }

        public byte[] GetData(bool decrypt = true)
        {
            if (IsDirectory)
            {
                throw new Exception("Can't set data on folders");
            }

            return InternalVirtualFile.GetData(decrypt);
        }

        public bool SetData(byte[] data)
        {
            if (IsDirectory)
            {
                throw new Exception("Can't set data on folders");
            }

            //TODO: Replace Data
            //throw new NotImplementedException();
            return false;
        }

        public P4KFilesystemEntry(P4KVirtualFilesystem p4kFilesystem, P4KVFS p4kVFS, P4KFile file)
        {
            IsRoot = false;
            IsDirectory = false;

            P4KFilesystem = p4kFilesystem;
            InternalVirtualFilesystem = p4kVFS;
            InternalVirtualFile = file;
            Type = Fileconverter.FilenameToFiletype(this.Name);
        }
    }
}
