using Crucible.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Crucible
{
    public static class CrucibleApplication
    {
        public delegate void FilesystemEntryOpenEventHandler(FilesystemEntryOpenEvent e);

        private static Dictionary<FileType, List<Func<IFilesystemEntry, FilesystemEntryTab>>> FileUIHandlers = new Dictionary<FileType, List<Func<IFilesystemEntry, FilesystemEntryTab>>>();
        public static Func<byte[], byte[]> DecryptionConversionCallback { get; internal set; }
        public static event FilesystemEntryOpenEventHandler FilesystemEntryOpen;
        public static string ApplicationDirectory = App.ApplicationDirectory;
        public static string PluginsDirectory = App.PluginsDirectory;
        public static string LibraryDirectory = App.LibraryDirectory;
        public static string CurrentProjectDirectory => Path.Combine(MainWindow.FilesystemManager?.LocalFilesystem?.LocalDirectoryPath ?? "", "crucible_project");
        

        public static void RegisterPlugin(string name, Type type)
        {
            App.RegisterPluginType(name, type);
        }

        [Obsolete("Plugin dependencies is not ready yet and will be released in a future release. Please contact Unknown44 on Discord for more information.")]
        public static void RegisterPlugin(string name, List<string> dependsOn, Type type)
        {
            App.RegisterPluginType(name, type);
        }

        public static void RegisterFileUI(FileType fileType, Func<IFilesystemEntry, FilesystemEntryTab> filesystemEntryOpenCallback)
        {
            List<Func<IFilesystemEntry, FilesystemEntryTab>> list;
            if (!FileUIHandlers.ContainsKey(fileType))
            {
                list = new List<Func<IFilesystemEntry, FilesystemEntryTab>>();
                FileUIHandlers[fileType] = list;
            }
            else
            {
                list = FileUIHandlers[fileType];
            }

            list.Add(filesystemEntryOpenCallback);
        }


        public static void _RegisterDecryption(Func<byte[], byte[]> decryptionConversionCallback)
        {
            DecryptionConversionCallback = decryptionConversionCallback;
        }


        public static void RegisterCodec(FileType fileType, Func<byte[], byte[]> decode, Func<byte[], byte[]> encode)
        {
            Fileconverter.RegisterCodec(fileType, decode, encode);
        }

        public static List<TabItem> GetFileUITabs(IFilesystemEntry filesystemEntry)
        {
            var tabs = new List<TabItem>();

            if (!FileUIHandlers.ContainsKey(filesystemEntry.Type))
            {
                return tabs;
            }

            foreach (var cb in FileUIHandlers[filesystemEntry.Type])
            {
                var tab = cb(filesystemEntry);
                tabs.Add(tab);
            }

            return tabs.Where(tab => tab != null).ToList();
        }

        internal static void OnOpenFile(Filesystem.IFilesystemEntry filesystemEntry)
        {
            if (FilesystemEntryOpen != null)
            {
                FilesystemEntryOpen(new FilesystemEntryOpenEvent { FilesystemEntry = filesystemEntry });
            }
        }
    }
}
