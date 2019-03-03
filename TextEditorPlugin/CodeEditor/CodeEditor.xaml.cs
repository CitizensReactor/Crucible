using CefSharp;
//using CefSharp.Wpf;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TextEditor;

namespace CodeEditor.Controls
{
    public class CustomWindowsFormsHost : WindowsFormsHost
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class CodeEditor : System.Windows.Controls.UserControl, INotifyPropertyChanged, IDisposable
    {
        CustomWindowsFormsHost Host = null;

        public delegate void CodeEditorSavedEventHandler(CodeEditor sender);
        public CodeEditorSavedEventHandler CodeEditorSaved;
        private void OnCodeEditorSaved()
        {
            if (CodeEditorSaved != null) CodeEditorSaved(this);
        }

        public CodeEditorLanguage? _EditorLanguage = null;
        public CodeEditorLanguage? EditorLanguage
        {
            get => _EditorLanguage;
            set
            {
                SetProperty(ref _EditorLanguage, value);
                SetLanguage();
            }
        }



        public ChromiumWebBrowser Browser { get; internal set; }

        private bool BrowserLoaded = false;



        class JavascriptObject : INotifyPropertyChanged
        {
            public bool _ReadOnly = false;
            public bool ReadOnly
            {
                get => _ReadOnly;
                set => SetProperty(ref _ReadOnly, value);
            }

            private string _SourceCode;
            public string SourceCode
            {
                get => _SourceCode;
                set { if (value != _SourceCode) SetProperty(ref _SourceCode, value); }
            }

            public void callSave()
            {
                if (SaveHotkey != null)
                {
                    SaveHotkey();
                }
            }

            public void setSource(string value)
            {
                if (UpdateText != null)
                {
                    UpdateText(value);
                }
            }

            public delegate void UpdateTextDelegate(string text);
            public event UpdateTextDelegate UpdateText;
            public delegate void SaveHotkeyDelegate();
            public event SaveHotkeyDelegate SaveHotkey;

            #region PropertyChanged


            /// <summary>
            ///     Multicast event for property change notifications.
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            ///     Checks if a property already matches a desired value.  Sets the property and
            ///     notifies listeners only when necessary.
            /// </summary>
            /// <typeparam name="T">Type of the property.</typeparam>
            /// <param name="storage">Reference to a property with both getter and setter.</param>
            /// <param name="value">Desired value for the property.</param>
            /// <param name="propertyName">
            ///     Name of the property used to notify listeners.  This
            ///     value is optional and can be provided automatically when invoked from compilers that
            ///     support CallerMemberName.
            /// </param>
            /// <returns>
            ///     True if the value was changed, false if the existing value matched the
            ///     desired value.
            /// </returns>
            protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
            {
                if (Equals(storage, value)) return false;
                storage = value;
                this.OnPropertyChanged(propertyName);
                return true;
            }

            /// <summary>
            ///     Notifies listeners that a property value has changed.
            /// </summary>
            /// <param name="propertyName">
            ///     Name of the property used to notify listeners.  This
            ///     value is optional and can be provided automatically when invoked from compilers
            ///     that support <see cref="CallerMemberNameAttribute" />.
            /// </param>
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        };

        private JavascriptObject javascriptObject = new JavascriptObject();
        public bool ReadOnly { get => javascriptObject.ReadOnly; set => javascriptObject.ReadOnly = value; }
        public string SourceCode { get => javascriptObject.SourceCode; set => javascriptObject.SourceCode = value; }

        private void JavascriptObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        public CodeEditor()
        {
            javascriptObject.PropertyChanged += JavascriptObject_PropertyChanged;
            javascriptObject.UpdateText += JavascriptObject_UpdateText;
            javascriptObject.SaveHotkey += JavascriptObject_SaveHotkey;

            InitializeComponent();

            Host = new CustomWindowsFormsHost();
            root.Children.Add(Host);

            CefSharpSettings.LegacyJavascriptBindingEnabled = true;
            Browser = new ChromiumWebBrowser($"codeeditor://editor.html");
            Browser.RegisterJsObject("CodeEditor", javascriptObject);
            Browser.FrameLoadEnd += Browser_FrameLoadEnd;
            Host.Child = Browser;

            this.IsVisibleChanged += CodeEditor_IsVisibleChanged;
        }

        private void JavascriptObject_SaveHotkey()
        {
            this.Dispatcher.Invoke(delegate
            {
                OnCodeEditorSaved();
            });
        }

        private void JavascriptObject_UpdateText(string text)
        {
            this.Dispatcher.Invoke(delegate
            {
                javascriptObject.SourceCode = text;
            });
        }

        private volatile bool _CheckForErrors = true;
        public void SetErrorChecking(bool check_for_errors)
        {
            _CheckForErrors = check_for_errors;
            if (!BrowserLoaded) return;

            if (_CheckForErrors)
            {
                Browser.ExecuteScriptAsync($"editor.session.setUseWorker(true);");
            }
            else
            {
                Browser.ExecuteScriptAsync($"editor.session.setUseWorker(false);");
            }

        }

        private void SetLanguage()
        {
            if (!BrowserLoaded) return;

            if (EditorLanguage != null)
            {
                var lang = EditorLanguage.ToString();
                Browser.ExecuteScriptAsync($"editor.session.setMode('ace/mode/{lang}');");
            }
            else Browser.ExecuteScriptAsync($"editor.session.setMode();");
        }

        private void SetReadOnly()
        {
            if (!BrowserLoaded) return;
            var read_only_str = ReadOnly ? "true" : "false";
            Browser.ExecuteScriptAsync($"editor.setReadOnly({read_only_str});");
        }

        private void SetValue()
        {
            if (!BrowserLoaded) return;
            var escaped_source = HttpUtility.JavaScriptStringEncode(SourceCode);
            Browser.ExecuteScriptAsync($"ace.edit('editor').setValue('{escaped_source}', -1);");
        }

        private void RemoveHistory()
        {
            if (!BrowserLoaded) return;
            var escaped_source = HttpUtility.JavaScriptStringEncode(SourceCode);
            Browser.ExecuteScriptAsync($"editor.session.setUndoManager(new ace.UndoManager());");
        }

        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (!BrowserLoaded)
            {
                BrowserLoaded = true;
                //Browser.Invoke((MethodInvoker)delegate
                //{
                //    Browser.Visible = true;
                //});

                // Hok into the change function
                //Browser.ExecuteScriptAsync("editor.session.on('change', function(delta) {CodeEditor.setSource(editor.getValue());});");

                SetLanguage();
                SetReadOnly();
                SetErrorChecking(_CheckForErrors);
            }

            SetValue();
            RemoveHistory();

            this.Dispatcher.BeginInvoke(new Action(delegate () {

                root.Visibility = Visibility.Visible;

            }));
        }

        private void CodeEditor_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            
        }

        public static int Init()
        {
            var settings = new CefSettings();
            settings.BrowserSubprocessPath = TextEditorPlugin.browser;
            settings.LocalesDirPath = TextEditorPlugin.locales;
            settings.ResourcesDirPath = TextEditorPlugin.res;
            settings.CachePath = System.IO.Path.GetTempPath();
            settings.CefCommandLineArgs["--disable-gpu-shader-disk-cache"] = "";
            settings.CefCommandLineArgs["--disable-local-storage"] = "";
            settings.CefCommandLineArgs["--disable-application-cache"] = "";
            settings.CefCommandLineArgs["--disable-cache"] = "";
            settings.CefCommandLineArgs["--disable-gpu-program-cache"] = "";
            settings.CefCommandLineArgs["--disable-gpu-shader-disk-cache"] = "";

#if DEBUG
            settings.RemoteDebuggingPort = 8088;
#else
            settings.CefCommandLineArgs["--log-severity"] = "disable";
#endif
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "codeeditor",
                SchemeHandlerFactory = new CefSharpSchemeHandlerFactory()
            });

            if (!Cef.IsInitialized) Cef.Initialize(settings, false, null);

            return 0;
        }

        #region PropertyChanged


        /// <summary>
        ///     Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Checks if a property already matches a desired value.  Sets the property and
        ///     notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers that
        ///     support CallerMemberName.
        /// </param>
        /// <returns>
        ///     True if the value was changed, false if the existing value matched the
        ///     desired value.
        /// </returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public void Dispose()
        {
            Host.Dispose();
        }
    }

    public class CefSharpSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public const string SchemeName = "custom";

        static CefSharpSchemeHandlerFactory()
        {

        }

        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            try
            {
                ////Notes:
                //// - The 'host' portion is entirely ignored by this scheme handler.
                //// - If you register a ISchemeHandlerFactory for http/https schemes you should also specify a domain name
                //// - Avoid doing lots of processing in this method as it will affect performance.
                //// - Uses the Default ResourceHandler implementation
                var request_uri = request.Url.TrimEnd('/', '\\');

                //var filename = System.IO.Path.GetFullPath(request.Url);
                var resourceName = request.Url.Replace("codeeditor://", "").TrimEnd('/', '\\').Replace('\\', '/').Replace('/', '.');

                var assembly = Assembly.GetExecutingAssembly();

                var extension = System.IO.Path.GetExtension(resourceName);

                using (Stream stream = assembly.GetManifestResourceStream($"TextEditor.CodeEditor.{resourceName}"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    return ResourceHandler.FromString(result, extension);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

    }
}
