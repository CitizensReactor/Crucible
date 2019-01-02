using Crucible.Filesystem;
using System.Windows.Controls;

namespace HelloWorld
{
    /// <summary>
    /// Interaction logic for DatabaseFile.xaml
    /// </summary>
    internal partial class HelloWorld : UserControl
    {
        public IFilesystemEntry File { get; set; }

        public HelloWorld(IFilesystemEntry file)
        {
            File = file;

            InitializeComponent();
        }

    }
}
