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
        private short compression;
        private short modtime;
        private short moddate;
        private int crc32;
        private int compressed_size;
        private int uncompressed_size;
        private short filename_length;
        private short extra_length;
        private short filecomment_length;
        private short disk_num_start;
        private short internal_attr;
        private int external_attr;
        private int offset_of_local_header;

        public string filename;
        public CentralDirectoryExtraData extra;
        public string filecomment;

        public CentralDirectory(Stream stream, CustomBinaryReader reader)
        {
            _Version = reader.ReadInt16();
            _RequiredVersion = reader.ReadInt16();
            Flags = reader.ReadInt16();
            compression = reader.ReadInt16();
            modtime = reader.ReadInt16();
            moddate = reader.ReadInt16();
            crc32 = reader.ReadInt32();
            compressed_size = reader.ReadInt32();
            uncompressed_size = reader.ReadInt32();
            filename_length = reader.ReadInt16();
            extra_length = reader.ReadInt16();
            filecomment_length = reader.ReadInt16();
            disk_num_start = reader.ReadInt16();
            internal_attr = reader.ReadInt16();
            external_attr = reader.ReadInt32();
            offset_of_local_header = reader.ReadInt32();

            filename = reader.ReadString(filename_length);

            extra = new CentralDirectoryExtraData(stream, reader, extra_length);
            filecomment = reader.ReadString(filecomment_length);

        }

        public CentralDirectory(string _filename, byte[] data)
        {
            _Version = 0;
            _RequiredVersion = 0;
            Flags = 0;
            compression = 0;
            modtime = 0; //TODO
            moddate = 0; //TODO
            crc32 = (int)Cryptography.Crc32Algorithm.Compute(data);
            compressed_size = -1;
            uncompressed_size = -1;
            filename_length = (short)_filename.Length;
            extra_length = 0xCE;
            filecomment_length = 0;
            disk_num_start = 0;
            internal_attr = 0;
            external_attr = 0;
            offset_of_local_header = 0;

            filename = _filename;
            extra = new CentralDirectoryExtraData(false);
            filecomment = "";

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
            byte[] extra_data = extra.CreateBinaryData();
            if (extra_data.Length != 0xCE)
                throw new Exception("Invalid extra data size");

            extra_length = (short)extra_data.Length;

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
            writer.Write(compression);
            writer.Write(modtime);
            writer.Write(moddate);
            writer.Write(crc32);
            writer.Write(compressed_size);
            writer.Write(uncompressed_size);
            writer.Write(filename_length);
            writer.Write(extra_length);
            writer.Write(filecomment_length);
            writer.Write(disk_num_start);
            writer.Write(internal_attr);
            writer.Write(external_attr);
            writer.Write(offset_of_local_header);

            writer.WriteString(filename, false);
            writer.Write(extra_data);
            writer.WriteString(filecomment, false);
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
