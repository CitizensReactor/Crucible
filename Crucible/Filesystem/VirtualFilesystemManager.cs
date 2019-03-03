using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crucible.Filesystem
{
    public class VirtualFilesystemManager : IDisposable
    {
        public LocalVirtualFilesystem LocalFilesystem { get; set; }
        public P4KVirtualFilesystem P4KFilesystem { get; set; }

        private Task InitializationTask = null;
        public Task GetInitializationTask()
        {
            return InitializationTask;
        }

        public int CurrentTaskProgress()
        {
            return 0;
        }

        public int CurrentTaskProgressMax()
        {
            return 0;
        }

        public string CurrentTaskString()
        {
            return "";
        }

        public int MajorVersion { get; internal set; }
        public int MinorVersion { get; internal set; }
        public int RevisionVersion { get; internal set; }
        public int BuildVersion { get; internal set; }
        public string DirectoryName { get; internal set; }


        public VirtualFilesystemManager(string localDirectory)
        {
            this.DirectoryName = localDirectory;

            MainWindow.SetStatus($"Checking game version", 0, 0, -1);

            if (!Directory.Exists(DirectoryName))
            {
                throw new DirectoryNotFoundException($"{DirectoryName} was not found");
            }

            var p4kFilepath = System.IO.Path.Combine(DirectoryName, "Data.p4k");
            if (!File.Exists(p4kFilepath))
            {
                throw new FileNotFoundException($"Data.p4k was not found");
            }

            var executableFilepath = System.IO.Path.Combine(DirectoryName, @"Bin64\StarCitizen.exe");
            if (!File.Exists(p4kFilepath))
            {
                throw new FileNotFoundException($"Bin64\\StarCitizen.exe was not found");
            }
            var versionInfo = FileVersionInfo.GetVersionInfo(executableFilepath);
            string version = versionInfo.ProductVersion;


            bool validVersion = false;
            MajorVersion = -1;
            MinorVersion = -1;
            RevisionVersion = -1;
            BuildVersion = -1;
            try
            {
                var version_sections = version.Split('.');
                if (version_sections.Length == 4)
                {
                    int major = int.Parse(version_sections[0]);
                    int minor = int.Parse(version_sections[1]);
                    int revision = int.Parse(version_sections[2]);
                    int build = int.Parse(version_sections[3]);

                    int targetVersion = 3;
                    int targetMinorMin = 0;
                    int targetMinorMax = 4;

                    validVersion = major == targetVersion;
                    validVersion &= minor >= targetMinorMin;
                    validVersion &= minor <= targetMinorMax;

                    MajorVersion = major;
                    MinorVersion = minor;
                    RevisionVersion = revision;
                    BuildVersion = build;
                }
            }
            catch (Exception)
            {
                throw new Exception("Failed to parse version information");
            }

            LocalFilesystem = new LocalVirtualFilesystem(this, DirectoryName);
            P4KFilesystem = new P4KVirtualFilesystem(this, p4kFilepath, MajorVersion, MinorVersion);

            InitializationTask = Task.Factory.StartNew(() =>
            {

                //TODO: Multithread this
                MainWindow.PrimaryWindow.SetTitle($"[{Path.GetFileName(p4kFilepath)}]");
                MainWindow.SetStatus($"Loading {DirectoryName}", 0, 0, -1);

                //TODO: Local filesystem
                MainWindow.SetStatus($"Loading {p4kFilepath}", 0, 0, -1);
                P4KFilesystem.Init(CrucibleSettings.Settings.StartReadOnly);

            });
        }

        public void Dispose()
        {
            P4KFilesystem.Dispose();
        }
    }
}
