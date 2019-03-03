using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    public class P4KFile
    {
        public string Filename
        {
            get => Path.GetFileName(this.Filepath);
            set => Filepath = Path.GetDirectoryName(this.Filename) + value;
        }

        public string Filepath
        {
            get => this.centralDirectory.Filename;
            set
            {
                if (Path.IsPathRooted(value))
                {
                    throw new Exception("Paths can't be rooted in the P4K Virtual File System");
                }

                // Todo, write information back to the filesystem
                throw new NotImplementedException();
            }
        }

        public P4KFile(P4KVFS filesystem, CentralDirectory central_directory, FileStructure file_structure = null)
        {
            this.centralDirectory = central_directory;
            this.FileStructure = file_structure;
            this.Filesystem = filesystem;
        }

        public P4KFile(P4KVFS filesystem, string filename, byte[] data)
        {
            this.Filesystem = filesystem;

            if (filesystem.FilepathExists(filename))
            {
                throw new Exception("File already exists");
            }

            filesystem.AddP4KFile(this, filename, data, ref centralDirectory, ref _fileStructure);
        }

        private FileStructure _fileStructure = null;
        public FileStructure FileStructure
        {
            get
            {
                if (_fileStructure == null)
                {
                    var mutex = Filesystem.GetStream(out FileStream stream, out CustomBinaryReader reader, out CustomBinaryWriter writer);
                    var file_structure = Filesystem.ReadPK(stream, reader, centralDirectory.Extra.data_offset) as FileStructure;
                    mutex.ReleaseMutex();

                    _fileStructure = file_structure;
                }
                return _fileStructure;
            }
            set => _fileStructure = value;
        }

        public readonly P4KVFS Filesystem;
        public CentralDirectory centralDirectory;

        public override string ToString()
        {
            return $"{Path.GetFileName(centralDirectory.Filename)} [{FileStructure.Extra.CompressedFileLength}b]";
        }

        public byte[] GetRawData()
        {
            var mutex = Filesystem.GetStream(out FileStream stream, out CustomBinaryReader reader, out CustomBinaryWriter writer);

            var result = P4KVFS.ReadDataFromFileSection(stream, reader, FileStructure.Extra.FileDataOffset, FileStructure.Extra.CompressedFileLength);

            mutex.ReleaseMutex();

            return result;
        }

        public byte[] GetData(bool decrypt = true)
        {
            var data = GetRawData();

            var crypt = new SHA256Managed();
            var dataHash = crypt.ComputeHash(data);
            bool isEqual = Enumerable.SequenceEqual(dataHash, this.centralDirectory.Extra.sha256_hash);
            if (!isEqual)
            {
                throw new Exception("SHA256 integrity check failed");
            }

            if (decrypt && this.centralDirectory.Extra.IsAesCrypted)
            {
                if(Filesystem.DecryptFunc != null)
                {
                    //data = Crypto.Crypto.Dercrypt128(data, EncryptionKey);
                    data = Filesystem.DecryptFunc(data);
                }
                else
                {
                    return null;
                }
            }


            

            switch (this._fileStructure.CompressionMode)
            {
                case FileStructure.FileCompressionMode.Uncompressed:
                    break;
                case FileStructure.FileCompressionMode.ZStd:
                    data = UnmanagedZStd.ZStd.StreamingUncompress(data, (ulong)this.centralDirectory.Extra.uncompressed_file_length);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return data;
        }

        void SetData()
        {
            throw new NotImplementedException();
        }
    }
}
