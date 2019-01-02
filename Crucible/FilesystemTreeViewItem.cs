using Crucible.Filesystem;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Crucible
{
    internal class FilesystemTreeViewItem : BindableBase
    {
        public bool IsDDS
        {
            get
            {
                var filename = File?.Name?.ToLowerInvariant();
                var extension = System.IO.Path.GetExtension(File.Name)?.ToLowerInvariant();

                bool is_dds = extension == ".dds";
                is_dds |= filename.EndsWith(".dds.a");
                for (int i = 0; i < 10; i++)
                {
                    if (is_dds) break;
                    is_dds |= filename.EndsWith($".dds.{i}");
                    is_dds |= filename.EndsWith($".dds.{i}a");
                }

                return is_dds;
            }
        }

        public string Title => File.Name;
        public IFilesystemEntry File { get; internal set; }
        public bool IsFile => !File.IsDirectory;
        public bool IsFolder => File.IsDirectory;
        public bool IsEncrypted => File.IsEncrypted;

        private bool _IsExpanded = false;
        public bool IsExpanded { get => _IsExpanded; set => SetProperty(ref _IsExpanded, value); }

        public string Directory
        {
            get => File.Directory;
            set => File.Directory = value;
        }

        public string ToolTip
        {
            get
            {
                if (IsFolder)
                {
                    return Directory;
                }
                else if(IsFile)
                {
                    return File?.Name;
                }
                return null;
            }
        }

        public ObservableCollection<FilesystemTreeViewItem> Items { get; internal set; }

        public FilesystemTreeViewItem(IFilesystemEntry file)
        {
            this.File = file;
            this.Items = new ObservableCollection<FilesystemTreeViewItem>();
        }

        public void Sort()
        {
            List<FilesystemTreeViewItem> items = Items.ToList();

            items.Sort((emp1, emp2) => emp1.Title.CompareTo(emp2.Title));
            items.Sort((emp1, emp2) => emp1.IsFile.CompareTo(emp2.IsFile));

            Items = new ObservableCollection<FilesystemTreeViewItem>(items);
        }
    }
}
