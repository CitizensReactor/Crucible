using Crucible.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crucible
{
    public class FilesystemEntryOpenEvent : EventArgs
    {
        public IFilesystemEntry FilesystemEntry { get; set; }
    }
}
