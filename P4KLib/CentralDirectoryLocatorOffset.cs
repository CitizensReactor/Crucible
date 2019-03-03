using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    class CentralDirectoryLocatorOffset : IPackageStructure
    {
        private int unknownA;
        public long DirectoryLocatorOffset;
        private int unknownB;

        public CentralDirectoryLocatorOffset(Stream stream, CustomBinaryReader reader)
        {
            unknownA = reader.ReadInt32();
            DirectoryLocatorOffset = reader.ReadInt64();
            unknownB = reader.ReadInt32();
        }

        public byte[] CreateBinaryData(bool header)
        {
            using (MemoryStream stream = new MemoryStream())
            using (CustomBinaryWriter writer = new CustomBinaryWriter(stream, Encoding.ASCII))
            {
                
                WriteBinaryToStream(stream, writer, header);
                return stream.ToArray();
            }
        }

        public void WriteBinaryToStream(Stream stream, CustomBinaryWriter writer, bool header)
        {
            if (header)
            {
                writer.Write((byte)0x50);
                writer.Write((byte)0x4B);
                writer.Write((byte)0x06);
                writer.Write((byte)0x07);
            }

            writer.Write(unknownA);
            writer.Write(DirectoryLocatorOffset);
            writer.Write(unknownB);
        }
    }
}
