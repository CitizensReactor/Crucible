using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    public enum Signature : UInt32
    {
        CentralDirectory = 0x0201,
        FileStructure = 0x1403,
        CentralDirectoryLocator = 0x0606,
        CentralDirectoryLocatorOffset = 0x0706,
        CentralDirectoryEnd = 0x0605,
    }
}
