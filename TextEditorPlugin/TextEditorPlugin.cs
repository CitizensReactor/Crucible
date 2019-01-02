using CefSharp;
using Crucible;
using Crucible.Filesystem;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace TextEditor
{
    public class TextEditorPlugin : IPlugin
    {
        public static string lib;
        public static string browser;
        public static string locales;
        public static string res;
        public static CefLibraryHandle LibraryLoader;

        public static void Registration()
        {
            // Assigning file paths to varialbles
            lib = Path.Combine(CrucibleApplication.PluginsDirectory, @"TextEditorPlugin_Resources\libcef.dll");
            browser = Path.Combine(CrucibleApplication.PluginsDirectory, @"TextEditorPlugin_Resources\CefSharp.BrowserSubprocess.exe");
            locales = Path.Combine(CrucibleApplication.PluginsDirectory, @"TextEditorPlugin_Resources\locales\");
            res = Path.Combine(CrucibleApplication.PluginsDirectory, @"TextEditorPlugin_Resources\");

            LibraryLoader = new CefLibraryHandle(lib);
            bool isValid = !LibraryLoader.IsInvalid;
            Console.WriteLine($"Library is valid: {isValid}");

            CrucibleApplication.RegisterFileUI(FileType.Text, CreateTextFileTab);
            CrucibleApplication.RegisterFileUI(FileType.Configuration, CreateTextFileTab);
            CrucibleApplication.RegisterFileUI(FileType.XML, CreateTextFileTab);
            CrucibleApplication.RegisterFileUI(FileType.Lua, CreateTextFileTab);
            CrucibleApplication.RegisterFileUI(FileType.JSON, CreateTextFileTab);
            CrucibleApplication.RegisterFileUI(FileType.INI, CreateTextFileTab);
        }

        public static void Main()
        {
            CrucibleApplication.FilesystemEntryOpen += FilesystemEntryOpen;
        }

        private static FilesystemEntryTab CreateTextFileTab(IFilesystemEntry filesystemEntry)
        {
            var tab = new FilesystemEntryTab(filesystemEntry);
            tab.Content = new TextFile(filesystemEntry);
            tab.Header = "TextEditor";
            return tab;
        }

        private static void FilesystemEntryOpen(FilesystemEntryOpenEvent e)
        {
            var tab = MainWindow.GetFilesystemEntryTab(e.FilesystemEntry);
        }
    }
}
