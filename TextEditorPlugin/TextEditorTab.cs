using Crucible.Filesystem;

namespace TextEditor
{
    class TextEditorTab : Crucible.FilesystemEntryTab
    {
        public TextEditorTab(IFilesystemEntry filesystemEntry) : base(filesystemEntry)
        {
            this.Content = new TextFile(filesystemEntry);
            this.Header = "TextEditor";
        }
    }
}