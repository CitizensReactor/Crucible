using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    class CentralDirectoryEnd : IPackageStructure
    {
        public short DiskNumber;
        public short DiskNumberWithCD;
        public short DiskEntries;
        public short TotalEntries;
        public int CentralDirectorySize;
        public int OffsetOfCdStartingDisk;
        public short CommentLength;
        public string Comment;

        public CentralDirectoryEnd(Stream stream, CustomBinaryReader reader)
        {
            DiskNumber = reader.ReadInt16();
            DiskNumberWithCD = reader.ReadInt16();
            DiskEntries = reader.ReadInt16();
            TotalEntries = reader.ReadInt16();
            CentralDirectorySize = reader.ReadInt32();
            OffsetOfCdStartingDisk = reader.ReadInt32();
            CommentLength = reader.ReadInt16();
            Comment = reader.ReadString(CommentLength);
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
                writer.Write((byte)0x05);
                writer.Write((byte)0x06);
            }

            writer.Write(DiskNumber);
            writer.Write(DiskNumberWithCD);
            writer.Write(DiskEntries);
            writer.Write(TotalEntries);
            writer.Write(CentralDirectorySize);
            writer.Write(OffsetOfCdStartingDisk);
            writer.Write(CommentLength);
            writer.WriteString(Comment, false);
        }
    }
}
