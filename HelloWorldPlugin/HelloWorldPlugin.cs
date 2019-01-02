using Crucible;
using Crucible.Filesystem;
using System;

namespace HelloWorld
{
    public class HelloWorldPlugin : IPlugin
    {
        public static void Registration()
        {
            // Registration occurs before the application loads
            CrucibleApplication.RegisterFileUI(FileType.Generic, CreateTab);
        }

        private static FilesystemEntryTab CreateTab(IFilesystemEntry filesystemEntry)
        {
            try
            {
                // use this to create tab content for files when they open (file capture)
                var tab = new FilesystemEntryTab(filesystemEntry);
                tab.Header = "Hello World";
                tab.Content = "Put your custom WPF contrl here";
                return tab;
            }
            catch(Exception err)
            {
                // display an error message
                MainWindow.SetStatus($"Hello World: Error: {err.Message}");
            }

            /* 
             * if your plugin doesn't support a specific file just return null here
             * and Crucible won't create the tab and just assume your plugin doesn't
             * actually support this file
             */
            return null;
        }
    }
}
