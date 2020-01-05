using Crucible.Filesystem;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Crucible
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    internal partial class App : Application
    {
        internal static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;
        internal static string PluginsDirectory => Path.Combine(ApplicationDirectory, "Plugins");
        internal static string LibraryDirectory => Path.Combine(ApplicationDirectory, "Lib");

        private static void SearchDirectories(DirectoryInfo directory, List<DirectoryInfo> directoryInfos)
        {
            if (directory.Exists)
            {
                directoryInfos.Add(directory);
                foreach (var childDirectory in directory.GetDirectories())
                {
                    SearchDirectories(childDirectory, directoryInfos);
                }
            }
        }

        private void GetDirectories(string directory, out List<DirectoryInfo> directories)
        {
            directories = new List<DirectoryInfo>();

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            if (directoryInfo.Exists)
            {
                SearchDirectories(directoryInfo, directories);
            }
        }

        private void LoadLibraries(List<DirectoryInfo> libraryDirectories)
        {

        }

        private void LoadPlugins(out List<Assembly> plugins)
        {
            plugins = new List<Assembly>();

            DirectoryInfo pluginDirectoryInfo = new DirectoryInfo(PluginsDirectory);

            if (!pluginDirectoryInfo.Exists)
            {
                return;
            }

            var pluginDLLs = Directory.EnumerateFiles(PluginsDirectory, "*.plugin.dll", SearchOption.AllDirectories).ToList();
            foreach (string pluginFile in pluginDLLs)
            {
                Assembly assembly = Assembly.LoadFile(pluginFile);
                plugins.Add(assembly);
            }
        }

        //private static FileStream ErrorLog = null;

        
        static string GetErrorLogFilepath()
        {
            return Path.Combine(ApplicationDirectory, "error.log");
        }

        private static FileStream _ErrorLog = null;
        static FileStream GetErrorLogFilestream()
        {
            if(_ErrorLog != null)
            {
                return _ErrorLog;
            }

            _ErrorLog = new FileStream(GetErrorLogFilepath(), FileMode.Create, FileAccess.Write, FileShare.Read);

            return _ErrorLog;
        }

        private void RegsiterExceptionHandlers()
        {
            if(File.Exists(GetErrorLogFilepath()))
            {
                File.Delete(GetErrorLogFilepath());
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        List<DirectoryInfo> LibraryDirectories;
        List<DirectoryInfo> PluginDirectories;
        List<Assembly> Plugins;

        private void RegisterPlugins()
        {
            foreach (var assembly in Plugins)
            {
                var plugin = assembly.GetType("Plugin");
                var pluginTypes = assembly.GetExportedTypes().Where(type => typeof(IPlugin).IsAssignableFrom(type));
                foreach (var pluginType in pluginTypes)
                {
                    if (pluginType == null) continue;

                    var initMethod = pluginType.GetMethod("Registration");
                    if (initMethod == null) continue;

                    initMethod.Invoke(null, new object[] { });
                }
            }
        }

        internal struct PluginRegistartionData
        {
#pragma warning disable 0649
            public string Name;
            public List<string> DependsOn;
            public Type Type;
#pragma warning restore 0649
        }
        internal static List<PluginRegistartionData> PluginRegistartionDatas = new List<PluginRegistartionData>();

        public static void RegisterPluginType(string name, Type type)
        {
            PluginRegistartionData data = new PluginRegistartionData();
            data.Name = name;
            data.Type = type;

            PluginRegistartionDatas.Add(data);
        }

        private void InitPlugins()
        {
            foreach (var assembly in Plugins)
            {
                var pluginTypes = assembly.GetExportedTypes().Where(type => typeof(IPlugin).IsAssignableFrom(type));
                foreach (var pluginType in pluginTypes)
                {
                    if (pluginType == null) continue;

                    var initMethod = pluginType.GetMethod("Main");
                    if (initMethod == null) continue;

                    initMethod.Invoke(null, new object[] { });
                }
            }
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            RegsiterExceptionHandlers();

            GetDirectories(LibraryDirectory, out LibraryDirectories);
            GetDirectories(PluginsDirectory, out PluginDirectories);

            LoadPlugins(out Plugins);
            RegisterPlugins();

            MainWindow mainWindow = new MainWindow(e);

            InitPlugins();

            mainWindow.Show();
        }

        private void LogException(Exception exception, string header)
        {
            lock (GetErrorLogFilestream())
            {
                using (StreamWriter writer = new StreamWriter(GetErrorLogFilestream(), Encoding.UTF8, 4096, true))
                {
                    writer.WriteLine(header);
                    writer.WriteLine("Date : " + DateTime.Now.ToString());
                    writer.WriteLine();

                    while (exception != null)
                    {
                        writer.WriteLine(exception.GetType().FullName);
                        writer.WriteLine("Message : " + exception.Message);
                        writer.WriteLine("StackTrace : " + exception.StackTrace);

                        exception = exception.InnerException;
                    }

                    writer.WriteLine();
                    writer.WriteLine();
                }
            }
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            LogException(dispatcherUnhandledExceptionEventArgs.Exception, "----------------------- DispatcherUnhandledException ------------------------");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            switch (unhandledExceptionEventArgs.ExceptionObject)
            {
                case Exception exception:
                    LogException(exception, "----------------------------- UnhandledException ----------------------------");
                    break;
                default:
                    lock (GetErrorLogFilestream())
                    {
                        using (StreamWriter writer = new StreamWriter(GetErrorLogFilestream(), Encoding.UTF8, 4096, true))
                        {
                            writer.WriteLine("----------------------- UnhandledException (Unknown) ------------------------");
                            writer.WriteLine();
                            writer.WriteLine();
                        }
                    }
                    break;

            }
        }

        private void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs firstChanceExceptionEventArgs)
        {
            var exception = firstChanceExceptionEventArgs.Exception;
            LogException(exception, "---------------------------- FirstChanceException ---------------------------");
        }

        private Assembly GetAssembly(string assemblyName, List<DirectoryInfo> directories)
        {
            foreach (var directory in PluginDirectories)
            {
                string folderPath = directory.FullName;
                string assemblyPath = Path.Combine(folderPath, assemblyName);

                if (!File.Exists(assemblyPath)) continue;

                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            }
            return null;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                AppDomain domain = (AppDomain)sender;
                var assemblyName = new AssemblyName(args.Name).Name + ".dll";

                return GetAssembly(assemblyName, PluginDirectories) ?? GetAssembly(assemblyName, LibraryDirectories);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }
    }
}
