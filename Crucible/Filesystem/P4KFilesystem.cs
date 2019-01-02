using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using P4KLib;

namespace Crucible.Filesystem
{
    public class P4KVirtualFilesystem : IVirtualFilesystem, IDisposable
    {
        public VirtualFilesystemManager FilesystemManager { get; }
        private volatile P4KVFS _Filesystem = null;
        private P4KVFS Filesystem { get => _Filesystem; set => _Filesystem = value; }
        
        public List<IFilesystemEntry> Files { get; internal set; } = new List<IFilesystemEntry>();
        public List<IFilesystemEntry> Folders { get; internal set; } = new List<IFilesystemEntry>();
        public IFilesystemEntry RootDirectory { get; internal set; } = null;

        public string Path { get; internal set; }
        public bool IsReadOnly => Filesystem.ReadOnly;

        public IFilesystemEntry this[string path] => RootDirectory[path];

        public P4KVirtualFilesystem(VirtualFilesystemManager virtualFilesystemManager, string p4kFilepath, int majorVersion, int minorVersion)
        {
            FilesystemManager = virtualFilesystemManager;
            Path = p4kFilepath;
            Filesystem = new P4KVFS(CrucibleApplication.DecryptionConversionCallback);
        }

        public void Init(bool startReadOnly)
        {
            Filesystem.Initialize(Path, startReadOnly, false);

            RootDirectory = new P4KFilesystemEntry(this, "root");
            Dictionary<string, IFilesystemEntry> folders = new Dictionary<string, IFilesystemEntry>();

            folders[""] = RootDirectory;

            // create the folder structure
            IFilesystemEntry previousFolderMenuItem = null;
            for (int i = 0; i < Filesystem.Count; i++)
            {
                previousFolderMenuItem = RootDirectory;
                var file = Filesystem[i];
                var path = file.Filepath;
                var directory = System.IO.Path.GetDirectoryName(path);

                // create missing folders
                if (!folders.ContainsKey(directory))
                {
                    var directory_sections = directory.Split('\\');
                    var current_path = "";
                    for (int directory_section_index = 0; directory_section_index < directory_sections.Length; directory_section_index++)
                    {
                        var folder_name = directory_sections[directory_section_index];
                        if (directory_section_index == 0)
                        {
                            current_path = folder_name;
                        }
                        else
                        {
                            current_path += $"\\{folder_name}";
                        }


                        IFilesystemEntry folderItem = null;
                        if (folders.ContainsKey(current_path))
                        {
                            folderItem = folders[current_path];
                        }
                        else
                        {
                            folderItem = new P4KFilesystemEntry(this, current_path);
                            folders[current_path] = folderItem;
                            previousFolderMenuItem.Items.Add(folderItem);
                        }
                        previousFolderMenuItem = folderItem;
                    }
                }
            }

            // bulk allocate a bunch of TreeViewItem's
            P4KFilesystemEntry[] fileItems = new P4KFilesystemEntry[Filesystem.Count];
            Parallel.For(0, Filesystem.Count, i => fileItems[i] = new P4KFilesystemEntry(this, Filesystem, Filesystem[i]));

            // populate the file structure
            for (int i = 0; i < Filesystem.Count; i++)
            {
                var file = Filesystem[i];
                var path = file.Filepath;
                var directory = System.IO.Path.GetDirectoryName(path);

                var folder = folders[directory];
                var fileItem = fileItems[i];
                folder.Items.Add(fileItem);
            }

            // sort the file structure
            foreach (var folder in folders)
            {
                //TODO: Implement sorting feature
                //folder.Value.Sort();
            }
        }

        public void Dispose()
        {
            Filesystem?.Dispose();
        }
    }
}
