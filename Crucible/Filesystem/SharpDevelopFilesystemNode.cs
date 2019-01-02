using ICSharpCode.TreeView;
using System;
using System.IO;

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

        public SharpDevelopFilesystemNode(IFilesystemEntry filesystemEntry)
        {
            FilesystemEntry = filesystemEntry;

            if (FilesystemEntry.IsDirectory)
            {
                foreach (var child in FilesystemEntry.Items)
                {
                    Children.Add(new SharpDevelopFilesystemNode(child));
                }
            }
        }
    }
}
