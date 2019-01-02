using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    public interface IPackageStructure
    {
        void WriteBinaryToStream(Stream stream, CustomBinaryWriter writer, bool header);
        byte[] CreateBinaryData(bool header);
    }
}
