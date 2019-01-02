using CodeEditor.Controls;
using Crucible;
using Crucible.Filesystem;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TextEditor
{
    /// <summary>
    /// Interaction logic for TextFile.xaml
    /// </summary>
    internal partial class TextFile : UserControl
    {
        private FileType Type => File.Type;
        public IFilesystemEntry File { get; set; }

        public TextFile(IFilesystemEntry file)
        {
            File = file;

            InitializeComponent();
            Editor.SetErrorChecking(false);

            var filedata = Fileconverter.GetConvertedData(file);
            var text = "";
            switch (file.Type)
            {
                case FileType.XML:
                    text = Encoding.UTF8.GetString(filedata);
                    Editor.EditorLanguage = CodeEditorLanguage.xml;
                    break;
                case FileType.Lua:
                    text = Encoding.UTF8.GetString(filedata);
                    Editor.EditorLanguage = CodeEditorLanguage.lua;
                    break;
                case FileType.JSON:
                    text = Encoding.UTF8.GetString(filedata);
                    Editor.EditorLanguage = CodeEditorLanguage.json;
                    break;
                case FileType.INI:
                    text = Encoding.UTF8.GetString(filedata);
                    Editor.EditorLanguage = CodeEditorLanguage.ini;
                    break;
                default:
                    try
                    {
                        text = Encoding.UTF8.GetString(filedata);
                    }
                    catch (Exception) { }
                    Editor.EditorLanguage = null;
                    break;
            }

            Editor.SourceCode = text;
            Editor.PropertyChanged += Editor_PropertyChanged;
            Editor.CodeEditorSaved += Editor_CodeEditorSaved;
        }

        private void Editor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

        }

        private void Editor_CodeEditorSaved(CodeEditor.Controls.CodeEditor sender)
        {

        }

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            var bytes = Encoding.UTF8.GetBytes(Editor.SourceCode);
            if(File.SetData(bytes))
            {
                MainWindow.SetStatus($"Saved {File.FullPath}");
            }
            else
            {
                MainWindow.SetStatus($"Failed to save {File.FullPath}");
            }
        }
    }
}
