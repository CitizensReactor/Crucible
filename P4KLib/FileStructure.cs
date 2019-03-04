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
        public short Version;
        public short VersionNeeded;
        public short Flags;
        public FileCompressionMode CompressionMode;
        public ushort ModificationTime;
        public ushort ModificationDate;
        public int CRC32;
        public int CompressedSize;
        public int UncompressedSize;
        public short FilenameLength;
        public short ExtraLength;
        public short CommentLength;
        public short DiskNumberStart;
        public short InternalAttribute;
        public int ExternalAttribute;
        public int OffsetOfLocalHeader;
        public string Filename;
        public FileStructureExtraData Extra;
        public string Comment;

        public FileStructure(Stream stream, CustomBinaryReader reader)
        {
            if ((stream.Position - 4) % 0x1000 != 0)
                throw new Exception("File Data Section Alignment Error (Pre Read)");

            Version = reader.ReadInt16();
            Flags = reader.ReadInt16();
            CompressionMode = (FileCompressionMode)reader.ReadInt16();
            ModificationTime = reader.ReadUInt16();
            ModificationDate = reader.ReadUInt16();
            CRC32 = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            FilenameLength = reader.ReadInt16();
            ExtraLength = reader.ReadInt16();

            Filename = reader.ReadString(FilenameLength);
            Extra = new FileStructureExtraData(stream, reader, ExtraLength);

        }

        public enum FileCompressionMode : short
        {
            Uncompressed = 0,
            ZStd = 100
        }

        public FileStructure(string _filename, byte[] data)
        {
            Version = 0;
            Flags = 0;
            CompressionMode = FileCompressionMode.Uncompressed;
            ModificationTime = 0;
            ModificationDate = 0;
            CRC32 = (int)Cryptography.Crc32Algorithm.Compute(data);
            CompressedSize = -1;
            UncompressedSize = -1;
            FilenameLength = (short)_filename.Length;

            //TODO: Calculate this value, make sure alignment to 0x1000
            ExtraLength = 0;

            Filename = _filename;
            Extra = new FileStructureExtraData(_filename, data);

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

            var extra_data = Extra.CreateBinaryData();
            ExtraLength = (short)extra_data.Length;

            writer.Write(Version);
            writer.Write(Flags);
            writer.Write((short)CompressionMode);
            writer.Write(ModificationTime);
            writer.Write(ModificationDate);
            writer.Write(CRC32);
            writer.Write(CompressedSize);
            writer.Write(UncompressedSize);
            writer.Write(FilenameLength);

            var extra_length_position = stream.Position;
            writer.Write(ExtraLength);

            writer.WriteString(Filename, false);
            var extra_data_start_position  = stream.Position;
            writer.Write(extra_data);

            // fixup the extra data to include the length of the padding, otherwise
            // the stream will only read part way through the padding into a bunch
            // of zeroed bytes
            var end = stream.Position;
            var padding_remaining = 0x1000 - (end - start);
            stream.Position = extra_length_position;
            ExtraLength += (short)padding_remaining;
            writer.Write((short)ExtraLength);

            stream.Position = extra_data_start_position + ExtraLength;
        }

        public class FileStructureExtraData : IPackageStructureExtra
        {
            public short _UnknownID;
            public short StructureSizeAfterHeader;
            public int UncompressedFileLength;
            private int _UnknownD; // potentially uncompressed_file_length hidword
            public int CompressedFileLength;
            private int _UnknownF; // potentially compressed_file_length hidword
            public long DataOffset;
            private int _UnknownG;
            private int _Timestamp_Maybe_But_Not_Sure;
            public long FileDataOffset;

            public FileStructureExtraData(Stream stream, CustomBinaryReader reader, int extra_length)
            {
                var end = stream.Position + extra_length;

                _UnknownID = reader.ReadInt16();
                StructureSizeAfterHeader = reader.ReadInt16();

                UncompressedFileLength = reader.ReadInt32();
                _UnknownD = reader.ReadInt32();
                CompressedFileLength = reader.ReadInt32();
                _UnknownF = reader.ReadInt32();

                var data_offset_ldword = (Int64)reader.ReadUInt32();
                var data_offset_hdword = (Int64)reader.ReadUInt32();

                DataOffset = data_offset_ldword + (data_offset_hdword << 32);

                _UnknownG = reader.ReadInt32();
                _Timestamp_Maybe_But_Not_Sure = reader.ReadInt32();

                // here be demons and lots of padding
                // make sure we read this to ensure the stream position is correct

                var padding_length = end - stream.Position;
                var data = reader.ReadBytes((int)padding_length);

                if (stream.Position % 0x1000 != 0)
                    throw new Exception("File Data Section Alignment Error (End Extra Read)");

                FileDataOffset = stream.Position;
            }

            public FileStructureExtraData(string filename, byte[] data)
            {
                _UnknownID = 0x0001;
                StructureSizeAfterHeader = 0x0020; // length of the extradata after this point
                UncompressedFileLength = data.Length; // TODO: Compress the data
                _UnknownD = 0;
                CompressedFileLength = data.Length;
                _UnknownF = 0;

                DataOffset = 0; // this is set externally later
                _UnknownG = 0;
                _Timestamp_Maybe_But_Not_Sure = 0;
            }

            public byte[] CreateBinaryData()
            {
                using (MemoryStream stream = new MemoryStream())
                using (CustomBinaryWriter writer = new CustomBinaryWriter(stream, Encoding.ASCII))
                {
                    writer.Write(_UnknownID);
                    writer.Write(StructureSizeAfterHeader);
                    writer.Write(UncompressedFileLength);
                    writer.Write(_UnknownD);
                    writer.Write(CompressedFileLength);
                    writer.Write(_UnknownF);

                    var data_offset_ldword = (int)(DataOffset & 0xFFFFFFFF);
                    var data_offset_hdword = (int)((DataOffset >> 32) & 0xFFFFFFFF);

                    writer.Write(data_offset_ldword);
                    writer.Write(data_offset_hdword);

                    return stream.ToArray();
                }
            }
        }
    }
}
