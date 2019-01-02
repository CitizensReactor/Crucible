using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crucible.Filesystem
{
    public interface IVirtualFilesystem
    {
        VirtualFilesystemManager FilesystemManager { get; }
        List<IFilesystemEntry> Files { get; }
        List<IFilesystemEntry> Folders { get; }
        IFilesystemEntry RootDirectory { get; }

        IFilesystemEntry this[string path] { get; }
    }
}
