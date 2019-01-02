using Crucible.Filesystem;
using System.ComponentModel;
using System.Windows.Controls;

namespace Crucible
{
    public interface IFilesystemTreeViewItemTab
    {
        IFilesystemEntry FilesystemEntry { get; }
    }

    public class FilesystemEntryTab : TabItem, IFilesystemTreeViewItemTab
    {
        public IFilesystemEntry FilesystemEntry { get; }

        public FilesystemEntryTab(IFilesystemEntry filesystemEntry)
        {
            FilesystemEntry = filesystemEntry;

            

            var filesystemEntryNofity = filesystemEntry as INotifyPropertyChanged;
            if (filesystemEntryNofity != null)
            {
                filesystemEntryNofity.PropertyChanged += FilesystemTreeViewItem_PropertyChanged;
            }

            var filename = filesystemEntry.Name.ToLowerInvariant();
            var filetype = Fileconverter.FilenameToFiletype(filename);
            
            Header = filesystemEntry.Name;
            //ToolTip = $"{filesystemEntry.Namespace}:{filesystemEntry.FullPath}";
        }

        private void FilesystemTreeViewItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Name":
                case "FullPath":
                    Header = FilesystemEntry.Name;
                    //ToolTip = $"{FilesystemEntry.Namespace}:{FilesystemEntry.FullPath}";
                    break;
            }
        }
    }
}
