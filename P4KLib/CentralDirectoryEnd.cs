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
        public short disk_number;
        public short disk_number_w_cd;
        public short disk_entries;
        public short total_entries;
        public int central_directory_size;
        public int offset_of_cd_starting_disk;
        public short comment_length;
        public string comment;

        public CentralDirectoryEnd(Stream stream, CustomBinaryReader reader)
        {
            disk_number = reader.ReadInt16();
            disk_number_w_cd = reader.ReadInt16();
            disk_entries = reader.ReadInt16();
            total_entries = reader.ReadInt16();
            central_directory_size = reader.ReadInt32();
            offset_of_cd_starting_disk = reader.ReadInt32();
            comment_length = reader.ReadInt16();
            comment = reader.ReadString(comment_length);
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

            writer.Write(disk_number);
            writer.Write(disk_number_w_cd);
            writer.Write(disk_entries);
            writer.Write(total_entries);
            writer.Write(central_directory_size);
            writer.Write(offset_of_cd_starting_disk);
            writer.Write(comment_length);
            writer.WriteString(comment, false);
        }
    }
}
