using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crucible.Filesystem
{
    public interface IFilesystemEntry
    {
        #region Filesystem

        IVirtualFilesystem Filesystem { get; }
        string Name { get; set; }
        string Directory { get; set; }
        string FullPath { get; }
        bool IsDirectory { get; }
        bool IsEncrypted { get; }
        ObservableCollection<IFilesystemEntry> Items { get; }
        bool IsRoot { get; }
        long Size { get; }
        DateTime LastModifiedDate { get; }

        IFilesystemEntry this[string index] { get; }

        byte[] GetData();
        bool SetData(byte[] data);
        FileType Type { get; }

        void Sort(bool recursive = false);

        #endregion

        #region WPF
        bool IsExpanded { get; set; }
        string Namespace { get; }
        #endregion
    }
}
