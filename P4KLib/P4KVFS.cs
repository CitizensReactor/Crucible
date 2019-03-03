using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P4KLib
{
    public class P4KVFS : IDisposable
    {
        public Func<byte[], byte[]> DecryptFunc { get; set; }
        private bool Logging { get; set; } = false;
        private string Filename { get; set; }

        private FileStream fileStream;
        private CustomBinaryReader customBinaryReader;
        private CustomBinaryWriter customBinaryWriter;
        private Mutex mutex;

        public string Filepath => fileStream?.Name;

        public Mutex GetStream(out FileStream stream, out CustomBinaryReader reader, out CustomBinaryWriter writer)
        {
            mutex.WaitOne();

            stream = this.fileStream;
            reader = this.customBinaryReader;
            writer = this.customBinaryWriter;

            return mutex;
        }

        

        private OrderedDictionary<string, P4KFile> Files = new OrderedDictionary<string, P4KFile>();
        //private List<CentralDirectory> Directory = new List<CentralDirectory>();
        //private List<FileStructure> FileStructures = new List<FileStructure>();
        private CentralDirectoryEnd central_directory_end;
        private CentralDirectoryLocatorOffset central_directory_locator_offset;
        private CentralDirectoryLocator central_directory_locator;

        public P4KFile this[string index]
        {
            get { return Files[index]; }
        }

        public P4KFile this[int index]
        {
            get { return Files[index]; }
        }

        public int Count => Files.Count;

        public bool ReadOnly { get; private set; }

        public P4KVFS(Func<byte[], byte[]> decryptionConversionCallback)
        {
            DecryptFunc = decryptionConversionCallback;
        }

        public void Initialize(string filename, bool readOnly, bool logging = false)
        {
            Filename = filename;
            Logging = logging;

            mutex = new Mutex();

            if(readOnly)
            {
                ReadOnly = true;
                fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            else
            {
                try
                {
                    ReadOnly = false;
                    fileStream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    ReadOnly = true;
                    fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
            }

            try
            {
                mutex.WaitOne();

                customBinaryReader = new CustomBinaryReader(fileStream);
                customBinaryWriter = ReadOnly ? null : new CustomBinaryWriter(fileStream);

                var offset_central_directory_end = FindEndCentralDirectoryOffset(fileStream, customBinaryReader);
                central_directory_end = ReadPK(fileStream, customBinaryReader, offset_central_directory_end) as CentralDirectoryEnd;

                var offset_central_directory_locator_offset = offset_central_directory_end - 0x14;
                central_directory_locator_offset = ReadPK(fileStream, customBinaryReader, offset_central_directory_locator_offset) as CentralDirectoryLocatorOffset;

                var offset_central_directory_locator = central_directory_locator_offset.DirectoryLocatorOffset;
                central_directory_locator = ReadPK(fileStream, customBinaryReader, offset_central_directory_locator) as CentralDirectoryLocator;

                PopulateDirectory(fileStream, customBinaryReader, central_directory_locator);
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            if (Logging) Console.WriteLine($"Finished loading {filename}");
        }

        public void WriteContentDirectoryChunk(FileStream stream, CustomBinaryReader reader, CustomBinaryWriter writer)
        {
            stream.Position = central_directory_locator.ContentDirectoryOffset;

            var cd_chunk = CreateCentralDirectoryChunk();

            writer.Write(cd_chunk);

            central_directory_locator.ContentDictionarySize = cd_chunk.Length;
            central_directory_locator.DontentDictionaryCount = Files.Count;
            central_directory_locator.ContentDictionaryCount2 = Files.Count;

            central_directory_locator_offset.DirectoryLocatorOffset = stream.Position;
            byte[] locator_bytes = this.central_directory_locator.CreateBinaryData(true);
            byte[] locator_offset_bytes = this.central_directory_locator_offset.CreateBinaryData(true);
            byte[] cd_end_bytes = this.central_directory_end.CreateBinaryData(true);

            writer.Write(locator_bytes);
            writer.Write(locator_offset_bytes);
            writer.Write(cd_end_bytes);
        }

        public void AllocateFilesystemChunk(
            long allocation_size,
            out long file_offset,
            out long total_allocated_size,
            bool regenerate_content_directory = true,
            byte fill = 0
            )
        {
            var current_offset = central_directory_locator.ContentDirectoryOffset;

            long total_allocation = 0;

            // Just rebase the existing offset to be always 0x1000 aligned for simplicity
            if (current_offset % 0x1000 != 0)
            {
                total_allocation += 0x1000 - (current_offset % 0x1000);
            }

            var start_offset = current_offset + total_allocation;

            long allocation_padded = allocation_size;

            // make 4kb aligned allocation
            if (allocation_padded % 0x1000 != 0)
            {
                allocation_padded += 0x1000 - (allocation_padded % 0x1000);
            }

            total_allocation += allocation_padded;


            var mutex = this.GetStream(out FileStream stream, out CustomBinaryReader reader, out CustomBinaryWriter writer);


            var total_data_to_move = stream.Length - current_offset;

            stream.Position = current_offset;

            List<byte[]> chunks = new List<byte[]>();
            long data_to_read = total_data_to_move;
            while (data_to_read > 0)
            {
                var iteration_bytes = Math.Min(0xFFFFFFFL, data_to_read);
                chunks.Add(reader.ReadBytes((int)iteration_bytes));
                data_to_read -= iteration_bytes;
            }

            stream.SetLength(stream.Length + total_allocation);
            stream.Position = current_offset;
            for (int i = 0; i < total_allocation; i++)
            {
                stream.WriteByte(0x00);
            }

            stream.Position = start_offset;
            for (int i = 0; i < allocation_size; i++)
            {
                stream.WriteByte(fill);
            }

            central_directory_locator.ContentDirectoryOffset += total_allocation;

            if(regenerate_content_directory)
            {
                WriteContentDirectoryChunk(stream, reader, writer);
            }



            

            mutex.ReleaseMutex();

            file_offset = start_offset;
            total_allocated_size = total_allocation;
        }

        public byte[] CreateCentralDirectoryChunk()
        {
            using (MemoryStream cd_stream = new MemoryStream())
            using (CustomBinaryWriter cd_writer = new CustomBinaryWriter(cd_stream))
            {
                for (var i = 0; i < Files.Count; i++)
                {
                    var file = Files[i];

                    byte[] bytes = file.centralDirectory.CreateBinaryData(true);

                    cd_writer.Write(bytes);
                }

                return cd_stream.ToArray();
            }
        }

        //public delegate void PopulateDirectoryCallback(int current_index, int content_dictionary_count);
        //public PopulateDirectoryCallback PopulateDirectoryCallbackFunc = null;

        public volatile int current_index = 0;
        public volatile int content_dictionary_count = 0;



        void PopulateDirectory(FileStream stream, CustomBinaryReader reader, CentralDirectoryLocator central_directory_locator)
        {
            long central_directory_base_offset = central_directory_locator.ContentDirectoryOffset;

            stream.Position = central_directory_base_offset;
            byte[] central_directory_data = reader.ReadBytes(central_directory_locator.ContentDictionarySize);
            using (MemoryStream central_directory_stream = new MemoryStream(central_directory_data))
            using (CustomBinaryReader central_directory_reader = new CustomBinaryReader(central_directory_stream))
            {
                // rebase current offset to 0 because we've copied data to a relative stream
                long central_directory_current_offset = 0;

                content_dictionary_count = central_directory_locator.DontentDictionaryCount;
                for (int i = 0; i < central_directory_locator.DontentDictionaryCount; i++)
                {
                    var central_directory = ReadPK(central_directory_stream, central_directory_reader, central_directory_current_offset) as CentralDirectory;
                    central_directory_current_offset = central_directory_stream.Position;

                    //TODO async query
                    //var file_structure = ReadPK(stream, reader, central_directory.extra.data_offset) as FileStructure;

                    Files[central_directory.Filename] = new P4KFile(this, central_directory);

                    //var file_data = ReadDataFromFileSection(stream, reader, file_offset.extra.file_data_offset, file_offset.extra.file_data_length);

                    /*if(PopulateDirectoryCallbackFunc != null)
                    {
                        PopulateDirectoryCallbackFunc(i, central_directory_locator.content_dictionary_count);
                    }*/

                    current_index = i;



                }


            }
        }

        public object ReadPK(Stream stream, CustomBinaryReader reader, Int64 offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);

            var magic = reader.ReadInt16();
            if (magic != 0x4B50)
                throw new Exception("Invalid PK offset");

            Signature signature = (Signature)reader.ReadInt16();

            switch (signature)
            {
                case Signature.CentralDirectory:

                    CentralDirectory centralDirectory = new CentralDirectory(stream, reader);

                    if (Logging) Console.WriteLine($"Found CentralDirectory {centralDirectory.Filename}");
                    if (Logging) Console.WriteLine($"Searching for FileStructure @{centralDirectory.Extra.data_offset.ToString("X")}");

                    return centralDirectory;
                case Signature.FileStructure:

                    FileStructure fileStructure = new FileStructure(stream, reader);

                    if (Logging) Console.WriteLine($"Found FileStructure {fileStructure.Filename}");


                    return fileStructure;
                case Signature.CentralDirectoryLocator:

                    CentralDirectoryLocator centralDirectoryLocator = new CentralDirectoryLocator(stream, reader);

                    if (Logging) Console.WriteLine($"Found CentralDirectoryLocator @{offset}");

                    return centralDirectoryLocator;
                case Signature.CentralDirectoryLocatorOffset:

                    CentralDirectoryLocatorOffset centralDirectoryLocatorOffset = new CentralDirectoryLocatorOffset(stream, reader);

                    if (Logging) Console.WriteLine($"Found CentralDirectoryLocatorOffset @{offset}");

                    return centralDirectoryLocatorOffset;
                case Signature.CentralDirectoryEnd:

                    CentralDirectoryEnd centralDirectoryEnd = new CentralDirectoryEnd(stream, reader);

                    if (Logging) Console.WriteLine($"Found CentralDirectoryEnd @{offset}");

                    return centralDirectoryEnd;
                default:
                    throw new NotImplementedException();
            }
        }

        static long FindEndCentralDirectoryOffset(FileStream stream, CustomBinaryReader reader)
        {
            // last PK must be within 0x1000 alignment, worst case scenario
            long length = Math.Min(stream.Length, 0x2000L);

            stream.Position = stream.Length - length;

            byte[] data = reader.ReadBytes((int)length);


            byte[] end_central_directory_magic = {
                0x50, 0x4B, 0x05, 0x06, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF
            };


            long current_offset = length - end_central_directory_magic.LongLength;
            bool found = true;
            for (; current_offset > 0; current_offset--)
            {
                found = true;
                for (long magic_index = 0; magic_index < end_central_directory_magic.LongLength; magic_index++)
                {
                    if (data[current_offset + magic_index] != end_central_directory_magic[magic_index])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    break;
                }
            }

            if(!found)
            {
                throw new Exception("Couldn't find CentralDirectoryEnd");
            }

            long offset_from_eof = length - current_offset;

            stream.Position = stream.Length - offset_from_eof;

            return stream.Position;
        }


        public static byte[] ReadDataFromFileSection(FileStream stream, CustomBinaryReader reader, Int64 offset, int size)
        {
            stream.Position = offset;

            if (offset % 0x1000 != 0)
                throw new Exception("File data alignment error");

            return reader.ReadBytes(size);
        }

        public void Dispose()
        {
            mutex.WaitOne();
            if(fileStream != null) fileStream.Close();
        }

        public void AddP4KFile(P4KFile file, string filename, byte[] data, ref CentralDirectory central_directory, ref FileStructure file_structure)
        {
            if (!Files.ContainsValue(file))
            {
                file.centralDirectory = new CentralDirectory(filename, data);
                file.FileStructure = new FileStructure(filename, data);

                // Add this first otherwise the Locator count wont be correct
                Files[file.Filepath] = file;

                AllocateFilesystemChunk(
                    data.Length + 0x1000, // add an extra chunk in for the fileinfo
                    out long allocation_offset,
                    out long total_allocated_size,
                    false
                    );

                file.centralDirectory.Extra.data_offset = allocation_offset;
                var crypt = new SHA256Managed();
                file.centralDirectory.Extra.sha256_hash = crypt.ComputeHash(data);

                var mutex = this.GetStream(out FileStream stream, out CustomBinaryReader reader, out CustomBinaryWriter writer);

                // these are the same for some reason... weird
                file.FileStructure.Extra.DataOffset = file.centralDirectory.Extra.data_offset;

                // write fileinfo
                stream.Position = allocation_offset;
                file.FileStructure.WriteBinaryToStream(stream, writer, true);

                // not actually part of the structure, just a useful helper to get the data as its
                // immediately after this structure
                file.FileStructure.Extra.FileDataOffset = stream.Position;
                file.centralDirectory.Extra.compressed_file_length = file.FileStructure.Extra.CompressedFileLength;
                file.centralDirectory.Extra.uncompressed_file_length = file.FileStructure.Extra.UncompressedFileLength;

                writer.Write(data);

                WriteContentDirectoryChunk(stream, reader, writer);

                mutex.ReleaseMutex();
            }
        }

        public bool FilepathExists(string filename)
        {
            return Files.Contains(filename);
        }
    }
}
