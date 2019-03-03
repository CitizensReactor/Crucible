using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    class CentralDirectoryLocator : IPackageStructure
    {
        private int unknownA;
        private int unknownB;
        private short unknownC;
        private short unknownD;
        private int unknownE;
        private int unknownF;
        public int DontentDictionaryCount;
        private int unknownG;
        public int ContentDictionaryCount2;
        private int unknownH;
        public int ContentDictionarySize;
        private int unknownI;
        public long ContentDirectoryOffset;

        public CentralDirectoryLocator(Stream stream, CustomBinaryReader reader)
        {
            unknownA = reader.ReadInt32();
            unknownB = reader.ReadInt32();
            unknownC = reader.ReadInt16();
            unknownD = reader.ReadInt16();
            unknownE = reader.ReadInt32();
            unknownF = reader.ReadInt32();
            DontentDictionaryCount = reader.ReadInt32();
            unknownG = reader.ReadInt32();
            ContentDictionaryCount2 = reader.ReadInt32();
            unknownH = reader.ReadInt32();
            ContentDictionarySize = reader.ReadInt32();
            unknownI = reader.ReadInt32();
            ContentDirectoryOffset = reader.ReadInt64();
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
                writer.Write((byte)0x06);
            }

            writer.Write(unknownA);
            writer.Write(unknownB);
            writer.Write(unknownC);
            writer.Write(unknownD);
            writer.Write(unknownE);
            writer.Write(unknownF);
            writer.Write(DontentDictionaryCount);
            writer.Write(unknownG);
            writer.Write(ContentDictionaryCount2);
            writer.Write(unknownH);
            writer.Write(ContentDictionarySize);
            writer.Write(unknownI);
            writer.Write(ContentDirectoryOffset);
        }
    }
}
