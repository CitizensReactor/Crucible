using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    public class CentralDirectory : BindableBase, IPackageStructure
    {
        public Signature Signature => Signature.CentralDirectory;

        public enum PKVersion : Byte
        {
            MSDOS = 0,                      // MS-DOS and OS/2 (FAT / VFAT / FAT32 file systems)
            Amiga = 1,                      // Amiga
            OpenVMS = 2,                    // OpenVMS
            UNIX = 3,                       // UNIX
            VM_CMS = 4,                     // VM/CMS
            Atari_ST = 5,                   // Atari ST
            HighPerformanceFileSystem = 6,  // OS/2 H.P.F.S.
            Macintosh = 7,                  // Macintosh
            ZSystem = 8,                    // Z-System
            ControlProgramMonitor = 9,      // CP/M
            WindowsNTFS = 10,               // Windows NTFS
            MVS = 11,                       // MVS (OS/390 - Z/OS) 
            VSE = 12,                       // VSE
            Acorn_Risc = 13,                // Acorn Risc
            VFAT = 14,                      // VFAT
            AlternateMVS = 15,              // alternate MVS
            BeOS = 16,                      // BeOS
            Tandem = 17,                    // Tandem
            IBMi = 18,                      // OS/400
            OSX_Darwin = 19,                // OS/X(Darwin)
                                            // 20 - 255: unused
        }

        public PKVersion Version
        {
            get => (PKVersion)BitConverter.GetBytes(_Version)[0];
            set => _Version = BitConverter.ToInt16(new byte[2] { (byte)value, BitConverter.GetBytes(_Version)[1] }, 0);
        }
        public Byte ZipSpecificationVersion
        {
            get => BitConverter.GetBytes(_Version)[1];
            set => _Version = BitConverter.ToInt16(new byte[2] { BitConverter.GetBytes(_Version)[0], value }, 0);
        }
        public PKVersion RequiredVersion
        {
            get => (PKVersion)BitConverter.GetBytes(_RequiredVersion)[0];
            set => _RequiredVersion = BitConverter.ToInt16(new byte[2] { (byte)value, BitConverter.GetBytes(_RequiredVersion)[1] }, 0);
        }
        public Byte RequiredZipSpecificationVersion
        {
            get => BitConverter.GetBytes(_RequiredVersion)[1];
            set => _RequiredVersion = BitConverter.ToInt16(new byte[2] { BitConverter.GetBytes(_RequiredVersion)[0], value }, 0);
        }

        private short _Version;
        private short _RequiredVersion;

        private short Flags;
        private short Compression;
        private short ModificationTime;
        private short ModificationDate;
        private int CRC32;
        private int CompressedSize;
        private int UncompressedSize;
        private short FilenameLength;
        private short ExtraLength;
        private short CommentLength;
        private short DiskNumberStart;
        private short InternalAttribute;
        private int ExternalAttribute;
        private int OffsetOfLocalHeader;

        public string Filename;
        public CentralDirectoryExtraData Extra;
        public string Comment;

        public CentralDirectory(Stream stream, CustomBinaryReader reader)
        {
            _Version = reader.ReadInt16();
            _RequiredVersion = reader.ReadInt16();
            Flags = reader.ReadInt16();
            Compression = reader.ReadInt16();
            ModificationTime = reader.ReadInt16();
            ModificationDate = reader.ReadInt16();
            CRC32 = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            FilenameLength = reader.ReadInt16();
            ExtraLength = reader.ReadInt16();
            CommentLength = reader.ReadInt16();
            DiskNumberStart = reader.ReadInt16();
            InternalAttribute = reader.ReadInt16();
            ExternalAttribute = reader.ReadInt32();
            OffsetOfLocalHeader = reader.ReadInt32();

            Filename = reader.ReadString(FilenameLength);

            Extra = new CentralDirectoryExtraData(stream, reader, ExtraLength);
            Comment = reader.ReadString(CommentLength);

        }

        public CentralDirectory(string _filename, byte[] data)
        {
            _Version = 0;
            _RequiredVersion = 0;
            Flags = 0;
            Compression = 0;
            ModificationTime = 0; //TODO
            ModificationDate = 0; //TODO
            CRC32 = (int)Cryptography.Crc32Algorithm.Compute(data);
            CompressedSize = -1;
            UncompressedSize = -1;
            FilenameLength = (short)_filename.Length;
            ExtraLength = 0xCE;
            CommentLength = 0;
            DiskNumberStart = 0;
            InternalAttribute = 0;
            ExternalAttribute = 0;
            OffsetOfLocalHeader = 0;

            Filename = _filename;
            Extra = new CentralDirectoryExtraData(false);
            Comment = "";

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
            byte[] extra_data = Extra.CreateBinaryData();
            if (extra_data.Length != 0xCE)
                throw new Exception("Invalid extra data size");

            ExtraLength = (short)extra_data.Length;

            if (header)
            {
                writer.Write((byte)0x50);
                writer.Write((byte)0x4B);
                writer.Write((byte)0x01);
                writer.Write((byte)0x02);
            }

            writer.Write(_Version);
            writer.Write(_RequiredVersion);
            writer.Write(Flags);
            writer.Write(Compression);
            writer.Write(ModificationTime);
            writer.Write(ModificationDate);
            writer.Write(CRC32);
            writer.Write(CompressedSize);
            writer.Write(UncompressedSize);
            writer.Write(FilenameLength);
            writer.Write(ExtraLength);
            writer.Write(CommentLength);
            writer.Write(DiskNumberStart);
            writer.Write(InternalAttribute);
            writer.Write(ExternalAttribute);
            writer.Write(OffsetOfLocalHeader);

            writer.WriteString(Filename, false);
            writer.Write(extra_data);
            writer.WriteString(Comment, false);
        }

        public class CentralDirectoryExtraData : IPackageStructureExtra
        {
            public short unknown_id;
            public short structure_size_after_header;
            public int uncompressed_file_length;
            private int unknownD; // potentially uncompressed_file_length hidword
            public int compressed_file_length;
            private int unknownF; // potentially compressed_file_length hidword
            public long data_offset;
            private int unknownG;
            private int unknownH;
            public byte[] unknownArray;
            private int unknownI;
            private short _IsAesCrypted;
            private int unknownK;
            public byte[] sha256_hash;

            public bool IsAesCrypted { get => _IsAesCrypted == 1; set => _IsAesCrypted = value ? (short)1 : (short)0; }

            public CentralDirectoryExtraData(Stream stream, CustomBinaryReader reader, int extra_length)
            {
                var start = stream.Position;
                {
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
                    unknownH = reader.ReadInt32();

                    unknownArray = new byte[128];
                    for (int i = 0; i < 128; i++)
                    {
                        unknownArray[i] = reader.ReadByte();
                    }

                    // Not sure which ones of these are int 32 if any
                    unknownI = reader.ReadInt32();
                    _IsAesCrypted = reader.ReadInt16();
                    if(!(_IsAesCrypted == 0 || _IsAesCrypted == 1))
                    {
                        throw new Exception("Invalid arg");
                    }
                    unknownK = reader.ReadInt32();

                    sha256_hash = reader.ReadBytes(32);
                }
                if (stream.Position != start + extra_length)
                    throw new Exception("Read out of range");
            }

            public CentralDirectoryExtraData(bool encrypted)
            {
                unknown_id = 0x0001;
                structure_size_after_header = 0x0020; // length of the extradata after this point
                uncompressed_file_length = 0; // this is set externally later
                unknownD = 0;
                compressed_file_length = 0; // this is set externally later
                unknownF = 0;

                //TODO: Data offset request
                var data_offset_ldword = (int)(data_offset & 0xFFFFFFFF);
                var data_offset_hdword = (int)((data_offset >> 32) & 0xFFFFFFFF);

                data_offset = 0;

                unknownG = 0;
                unknownH = 0;

                unknownArray = new byte[128];

                // Not sure which ones of these are int 32 if any
                unknownI = 0x00845000;
                _IsAesCrypted = 0x0000;
                if (encrypted) _IsAesCrypted |= 0x1;
                unknownK = 0x00245003;

                sha256_hash = new byte[32];
            }

            public byte[] CreateBinaryData()
            {
                if ((unknownArray?.Length ?? 0) != 32)
                    throw new Exception("Invalid array size (unknownArray)");

                if ((sha256_hash?.Length ?? 0) != 32)
                    throw new Exception("Invalid array size (sha256_hash_maybe)");

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

                    writer.Write(unknownG);
                    writer.Write(unknownH);

                    for (int i = 0; i < 32; i++)
                    {
                        writer.Write(unknownArray[i]);
                    }

                    // Not sure which ones of these are int 32 if any
                    writer.Write(unknownI);
                    writer.Write(_IsAesCrypted);
                    writer.Write(unknownK);

                    writer.Write(sha256_hash);

                    return stream.ToArray();
                }
            }
        }
    }
}
