using ICSharpCode.TreeView;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Crucible.Filesystem
{
    public class SharpDevelopFilesystemNode : SharpTreeNode
    {
        public override Object Icon
        {
            get
            {
                if (FilesystemEntry.IsDirectory)
                {
                    return IconManager.FindIconForFolder(true, false);
                }
                else
                {
                    return IconManager.FindIconForFilename(FilesystemEntry.Name, true);
                }
            }
        }

        public override object ExpandedIcon
        {
            get
            {
                if (FilesystemEntry.IsDirectory)
                {
                    return IconManager.FindIconForFolder(true, true);
                }
                else
                {
                    return IconManager.FindIconForFilename(FilesystemEntry.Name, true);
                }
            }
        }
        public override Object Text => FileName;
        public string FileName => FilesystemEntry.Name;
        public long FileSize => FilesystemEntry.Size;
        public string FileSizeStr => CrucibleUtil.BytesToString(FileSize);
        public DateTime FileModified => FilesystemEntry.LastModifiedDate;
        public string FileType
        {
            get
            {
                if (FilesystemEntry.IsDirectory)
                {
                    return "File folder";
                }

                if (FilesystemEntry.Type == Crucible.FileType.Generic)
                {
                    var windowsExtensionDescription = IconManager.GetFileTypeDescription(FileName);
                    if (!string.IsNullOrWhiteSpace(windowsExtensionDescription))
                    {
                        return windowsExtensionDescription;
                    }

                    var extension = Path.GetExtension(FilesystemEntry.Name);
                    if (extension.Length > 1)
                    {

                        extension = extension.Substring(1);
                    }
                    extension = extension.Trim();

                    if (!string.IsNullOrWhiteSpace(extension))
                    {
                        extension = extension.ToUpper();
                        return $"{extension} File";
                    }

                    return $"File";
                }

                switch (FilesystemEntry.Type)
                {
                    case Crucible.FileType.DataCoreBinary: return "DataCore Binary";
                    case Crucible.FileType.Text: return "Text Document";
                    case Crucible.FileType.Configuration: return "Configuration File";
                    case Crucible.FileType.XML: return "Configuration File";
                    case Crucible.FileType.Lua: return "LUA Script";
                    case Crucible.FileType.JSON: return "JavaScript Object File";
                    case Crucible.FileType.DDS: return "Direct Draw Surface Texture";
                    case Crucible.FileType.DDSChild: return "Direct Draw Surface Fragment";
                    case Crucible.FileType.BNK: return "Soundbank File";
                    case Crucible.FileType.WEM: return "Encoded Media File";
                    case Crucible.FileType.CrytekGeometryFormat: return "Crytek Geometry Format File";
                    case Crucible.FileType.CrytekGeometryAnimation: return "Crytek Geometry Animation File";
                    case Crucible.FileType.P4K: return "StarCitizen Package";
                    case Crucible.FileType.PAK: return "Package";
                }

                return FilesystemEntry.Type.ToString();
            }
        }
        //TODO: We might just be able to remove this now that we have a indirection?
        public bool Expanded => FilesystemEntry.IsExpanded;

        public IFilesystemEntry FilesystemEntry { get; set; }

        private bool Initialized = false;

        public SharpDevelopFilesystemNode(IFilesystemEntry filesystemEntry, bool isRoot = false)
        {
            FilesystemEntry = filesystemEntry;

            this.PropertyChanged += SharpDevelopFilesystemNode_PropertyChanged;

            //if (isRoot)
            //{
            //    Initialize(1);
            //}
        }

        private void Initialize(int maxDepth = 0)
        {
            if (!Initialized && FilesystemEntry.IsDirectory)
            {
                FilesystemEntry.Sort();

                var propertyChangedObject = FilesystemEntry as INotifyPropertyChanged;
                if (propertyChangedObject == null)
                {
                    throw new Exception("Expected a INotifyPropertyChanged object");
                }

                FilesystemEntry.Items.CollectionChanged += Items_CollectionChanged;
                propertyChangedObject.PropertyChanged += PropertyChangedObject_PropertyChanged;


                Initialized = true;
            }

            UpdateChildren(maxDepth);
        }

        private void SharpDevelopFilesystemNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsExpanded":
                    Initialize(1);
                    break;
            }
        }

        void UpdateChildren(int maxDepth = 0)
        {
            if (FilesystemEntry.IsDirectory)
            {
                SharpDevelopFilesystemNode[] oldEntries;
                {
                    SharpTreeNode[] sharpTreeNodes = new SharpTreeNode[Children.Count];
                    Children.CopyTo(sharpTreeNodes, 0);
                    oldEntries = sharpTreeNodes.Cast<SharpDevelopFilesystemNode>().ToArray();
                }

                // reuse exsting nodes
                var filesystemEntries = FilesystemEntry.Items;
                var filesystemEntriesCount = filesystemEntries.Count;

                bool requiresUpdate = false;
                // check for updates
                {
                    requiresUpdate |= oldEntries.Length != filesystemEntriesCount;
                    if (!requiresUpdate)
                    {
                        for (int i = 0; i < filesystemEntriesCount; i++)
                        {
                            if (oldEntries[i].FilesystemEntry != filesystemEntries[i])
                            {
                                requiresUpdate = true;
                                break;
                            }
                        }
                    }
                }

                // perform update
                SharpDevelopFilesystemNode[] newEntries = oldEntries;
                if (requiresUpdate)
                {
                    newEntries = new SharpDevelopFilesystemNode[filesystemEntriesCount];

                    for (var entryIndex = 0; entryIndex < filesystemEntriesCount; entryIndex++)
                    {
                        var currentFilesystemEntry = FilesystemEntry.Items[entryIndex];

                        // find existing entry
                        var existingEntry = oldEntries.Where(entry => {
                            return entry.FilesystemEntry == currentFilesystemEntry;
                        }).FirstOrDefault();

                        // use existing or create a new entry
                        newEntries[entryIndex] = existingEntry ?? new SharpDevelopFilesystemNode(currentFilesystemEntry);
                    }

                    if (Children.Count > 0)
                    {
                        Children.Clear();
                    }
                    Children.AddRange(newEntries);
                }
                if (maxDepth > 0)
                {
                    maxDepth--;
                    foreach (var entry in newEntries)
                    {
                        entry.Initialize(maxDepth);
                    }
                }
            }
        }

        private void PropertyChangedObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Items")
            {
                FilesystemEntry.Items.CollectionChanged += Items_CollectionChanged;
                FilesystemEntry.Sort();
                UpdateChildren();
            }
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            FilesystemEntry.Sort();
            UpdateChildren();
        }
    }
}
