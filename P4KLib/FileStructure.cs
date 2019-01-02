using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    public class FileStructure : IPackageStructure
    {
        public short version;
        public short version_needed;
        public short flags;
        public FileCompressionMode compression;
        public short modtime;
        public short moddate;
        public int crc32;
        public int compressed_size;
        public int uncompressed_size;
        public short filename_length;
        public short extra_length;
        public short filecomment_length;
        public short disk_num_start;
        public short internal_attr;
        public int external_attr;
        public int offset_of_local_header;
        public string filename;
        public FileStructureExtraData extra;
        public string filecomment;

        public FileStructure(Stream stream, CustomBinaryReader reader)
        {
            if ((stream.Position - 4) % 0x1000 != 0)
                throw new Exception("File Data Section Alignment Error (Pre Read)");

            version = reader.ReadInt16();
            flags = reader.ReadInt16();
            compression = (FileCompressionMode)reader.ReadInt16();
            modtime = reader.ReadInt16();
            moddate = reader.ReadInt16();
            crc32 = reader.ReadInt32();
            compressed_size = reader.ReadInt32();
            uncompressed_size = reader.ReadInt32();
            filename_length = reader.ReadInt16();
            extra_length = reader.ReadInt16();

            filename = reader.ReadString(filename_length);
            extra = new FileStructureExtraData(stream, reader, extra_length);

        }

        public enum FileCompressionMode : short
        {
            Uncompressed = 0,
            ZStd = 100
        }

        public FileStructure(string _filename, byte[] data)
        {
            version = 0;
            flags = 0;
            compression = FileCompressionMode.Uncompressed;
            modtime = 0;
            moddate = 0;
            crc32 = (int)Cryptography.Crc32Algorithm.Compute(data);
            compressed_size = -1;
            uncompressed_size = -1;
            filename_length = (short)_filename.Length;

            //TODO: Calculate this value, make sure alignment to 0x1000
            extra_length = 0;

            filename = _filename;
            extra = new FileStructureExtraData(_filename, data);

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
            var start = stream.Position;

            if (header)
            {
                writer.Write((byte)0x50);
                writer.Write((byte)0x4B);
                writer.Write((byte)0x03);
                writer.Write((byte)0x14);
            }

            var extra_data = extra.CreateBinaryData();
            extra_length = (short)extra_data.Length;

            writer.Write(version);
            writer.Write(flags);
            writer.Write((short)compression);
            writer.Write(modtime);
            writer.Write(moddate);
            writer.Write(crc32);
            writer.Write(compressed_size);
            writer.Write(uncompressed_size);
            writer.Write(filename_length);

            var extra_length_position = stream.Position;
            writer.Write(extra_length);

            writer.WriteString(filename, false);
            var extra_data_start_position  = stream.Position;
            writer.Write(extra_data);

            // fixup the extra data to include the length of the padding, otherwise
            // the stream will only read part way through the padding into a bunch
            // of zeroed bytes
            var end = stream.Position;
            var padding_remaining = 0x1000 - (end - start);
            stream.Position = extra_length_position;
            extra_length += (short)padding_remaining;
            writer.Write((short)extra_length);

            stream.Position = extra_data_start_position + extra_length;
        }

        public class FileStructureExtraData : IPackageStructureExtra
        {
            public short unknown_id;
            public short structure_size_after_header;
            public int uncompressed_file_length;
            private int unknownD; // potentially uncompressed_file_length hidword
            public int compressed_file_length;
            private int unknownF; // potentially compressed_file_length hidword
            public long data_offset;
            private int unknownG;
            private int timestamp_maybe_but_not_sure;

            public long file_data_offset;

            public FileStructureExtraData(Stream stream, CustomBinaryReader reader, int extra_length)
            {
                var end = stream.Position + extra_length;

                unknown_id = reader.ReadInt16();
                structure_size_after_header = reader.ReadInt16();

                uncompressed_file_length = reader.ReadInt32();
                unknownD = reader.ReadInt32();
                compressed_file_length = reader.ReadInt32();
                unknownF = reader.ReadInt32();

                var data_offset_ldword = (Int64)reader.ReadUInt32();
                var data_offset_hdword = (Int64)reader.ReadUInt32();

                data_offset = data_offset_ldword + (data_offset_hdword << 32);

                unknownG = reader.ReadInt32();
                timestamp_maybe_but_not_sure = reader.ReadInt32();

                // here be demons and lots of padding
                // make sure we read this to ensure the stream position is correct

                var padding_length = end - stream.Position;
                var data = reader.ReadBytes((int)padding_length);

                if (stream.Position % 0x1000 != 0)
                    throw new Exception("File Data Section Alignment Error (End Extra Read)");

                file_data_offset = stream.Position;
            }

            public FileStructureExtraData(string filename, byte[] data)
            {
                unknown_id = 0x0001;
                structure_size_after_header = 0x0020; // length of the extradata after this point
                uncompressed_file_length = data.Length; // TODO: Compress the data
                unknownD = 0;
                compressed_file_length = data.Length;
                unknownF = 0;

                data_offset = 0; // this is set externally later
                unknownG = 0;
                timestamp_maybe_but_not_sure = 0;
            }

            public byte[] CreateBinaryData()
            {
                using (MemoryStream stream = new MemoryStream())
                using (CustomBinaryWriter writer = new CustomBinaryWriter(stream, Encoding.ASCII))
                {
                    writer.Write(unknown_id);
                    writer.Write(structure_size_after_header);
                    writer.Write(uncompressed_file_length);
                    writer.Write(unknownD);
                    writer.Write(compressed_file_length);
                    writer.Write(unknownF);

                    var data_offset_ldword = (int)(data_offset & 0xFFFFFFFF);
                    var data_offset_hdword = (int)((data_offset >> 32) & 0xFFFFFFFF);

                    writer.Write(data_offset_ldword);
                    writer.Write(data_offset_hdword);

                    return stream.ToArray();
                }
            }
        }
    }
}
