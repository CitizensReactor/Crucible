using Binary;
using Crucible.DDSTypes;
using Crucible.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Crucible
{
    class DDSConverter
    {
        static UInt32 uchar32(string text)
        {

            var bytes = Encoding.ASCII.GetBytes(text);

            if (bytes.Length != 4)
                throw new Exception("invalid");

            return BitConverter.ToUInt32(bytes, 0);
        }

        public unsafe static void SaveDDS(IFilesystemEntry file, string path = null)
        {
            var filesystem = file.Filesystem;
            var filesystemManager = filesystem.FilesystemManager;
            var searchName = file.FullPath;


            var rawData = file.GetData();
            BinaryBlobReader reader = new BinaryBlobReader(rawData, 0);

            bool isDX10 = false;

            UInt32 dwMagic = reader.ReadUInt32();
            if (dwMagic != uchar32("DDS "))
            {
                throw new Exception("Invalid dds file");
            }
            DDS_HEADER header = reader.Read<DDS_HEADER>();
            DDS_HEADER_DXT10 header10;

            if (header.dwSize != sizeof(DDS_HEADER))
            {
                throw new Exception("Invalid header size");
            }

            bool fourCCFlag = (header.ddspf.dwFlags & DDS_PIXELFORMAT_FLAGS.DDPF_FOURCC) != 0;
            if (fourCCFlag)
            {
                isDX10 = header.ddspf.dwFourCC == uchar32("DX10");
                if (isDX10)
                {
                    header10 = reader.Read<DDS_HEADER_DXT10>();
                }
            }

            var max_dimension = Math.Min(header.dwWidth, header.dwHeight);
            var mips = (int)Math.Log((double)max_dimension, 2.0);
            var extra_mips = Math.Max(mips - 4, 0);

            // hack: there must be a better way to tell
            bool isCubemap = file.Name.EndsWith("_cm_diff.dds", StringComparison.OrdinalIgnoreCase);
            if(isCubemap)
            {
                var index = searchName.IndexOf("_cm_diff.dds", StringComparison.OrdinalIgnoreCase);
                var prefix = searchName.Substring(0, index);
                var newSearchName = prefix + "_cm.dds";
                searchName = newSearchName;
            }

            var isNormal = false;
            isNormal |= searchName.EndsWith("ddna.dds", StringComparison.OrdinalIgnoreCase);
            isNormal |= searchName.EndsWith("ddn.dds", StringComparison.OrdinalIgnoreCase);

            byte[] allData;
            {
                List<byte> allDataBuffer = new List<byte>(rawData);
                List<byte> alphaDataBuffer = new List<byte>(0);

                var chunkFileAlphaPath = $"{searchName}.a";
                var chunkFileAlpha = filesystem[chunkFileAlphaPath];
                if (chunkFileAlpha != null) //todo programatically determine alpha existance
                {
                    var dataChunkAlpha = chunkFileAlpha.GetData();

                    BinaryBlobReader alphaReader = new BinaryBlobReader(dataChunkAlpha, 0);
                    DDS_HEADER alphaHeader = reader.Read<DDS_HEADER>();
                    DDS_HEADER_DXT10 alphaHeader10;
                    if (isDX10)
                    {
                        alphaHeader10 = reader.Read<DDS_HEADER_DXT10>();
                    }
                    var remainderBytes = reader.Read<byte>((int)(dataChunkAlpha.LongLength - reader.Position));

                    alphaDataBuffer.AddRange(remainderBytes);
                    //alphaDataBuffer.AddRange(dataChunkAlpha);

                    for (int i = 1; i <= extra_mips; i++)
                    {
                        chunkFileAlphaPath = $"{searchName}.{i}a";
                        chunkFileAlpha = filesystem[chunkFileAlphaPath];
                        dataChunkAlpha = chunkFileAlpha.GetData();

                        alphaDataBuffer.AddRange(dataChunkAlpha);
                    }

                }

                for (int i = 1; i <= extra_mips; i++)
                {
                    var chunkFilePath = $"{searchName}.{i}";
                    var chunkFile = filesystem[chunkFilePath];

                    if (chunkFile == null)
                    {
                        //throw new Exception($"Data chunk {chunkFilePath} missing!");
                        continue; // this is valid for some files
                    }
                    var dataChunk = chunkFile.GetData();

                    allDataBuffer.AddRange(dataChunk);
                }
                allDataBuffer.AddRange(alphaDataBuffer);
                allData = allDataBuffer.ToArray();
            }

            string fullPath;
            var baseDirectory = path ?? filesystemManager.DirectoryName;
            if (file.IsDirectory)
            {
                fullPath = Path.Combine(baseDirectory, searchName);
            }
            else
            {
                fullPath = path ?? Path.Combine(baseDirectory, searchName);
            }
            var fullDirectory = Path.GetDirectoryName(fullPath);

            Directory.CreateDirectory(fullDirectory);
            using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(allData, 0, allData.Length);
            }
            if(!isCubemap)
            {
                var tiffPath = Path.ChangeExtension(fullPath, ".tiff");
                try
                {
                    var tiffData = ImageProcessing.DDS.Convert(allData, isNormal);
                    using (FileStream fs = new FileStream(tiffPath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(tiffData, 0, tiffData.Length);
                    }
                }
                catch(Exception e)
                {
                    MainWindow.SetStatus($"Failed to save TIFF {tiffPath} | {e.Message}");
                }
            }

        }




    }
}
