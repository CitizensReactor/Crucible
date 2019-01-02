using Crucible.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Crucible
{
    public enum FileType
    {
        Generic,
        DataCoreBinary,
        Text,
        Configuration,
        XML,
        INI,
        Lua,
        JSON,
        // Textures
        DDS,
        DDSChild,
        // Audio
        BNK,
        WEM,
        // CryEngine Art Asset Types https://docs.cryengine.com/display/CEMANUAL/Art+Asset+File+Types
        CrytekGeometryFormat,
        CrytekGeometryAnimation,
        // Package Types
        P4K,
        PAK
    }

    public static class Fileconverter
    {
        public static FileType FileToFiletype(IFilesystemEntry file)
        {
            return FilenameToFiletype(file.FullPath);
        }

        public static FileType FilenameToFiletype(string filename)
        {
            if(FileTypeChecker.IsExtensionDDS(filename))
            {
                return FileType.DDS;
            }
            else if (FileTypeChecker.IsExtensionDDSChild(filename))
            {
                return FileType.DDSChild;
            }

            return ExtensionToFiletype(Path.GetExtension(filename));
        }

        public static FileType ExtensionToFiletype(string extension)
        {
            extension = extension.ToLowerInvariant();

            switch (extension)
            {
                case ".dcb": return FileType.DataCoreBinary;
                case ".txt": return FileType.Text;
                case ".cfg": return FileType.Configuration;
                case ".mtl":
                case ".xml": return FileType.XML;
                case ".ini": return FileType.INI;
                case ".cgf": return FileType.CrytekGeometryFormat;
                case ".cga": return FileType.CrytekGeometryAnimation;
                case ".lua": return FileType.Lua;
                case ".id":
                case ".json": return FileType.JSON;
                case ".bnk": return FileType.BNK;
                case ".wem": return FileType.WEM;
                case ".p4k": return FileType.P4K;
                case ".pak": return FileType.PAK;

            }
            return FileType.Generic;
        }

        public static byte[] GetConvertedData(IFilesystemEntry file)
        {
            var type = FileToFiletype(file);
            var rawData = file.GetData();
            var data = ConvertFile(rawData, type);
            return data;
        }

        struct Codec
        {
            public Func<byte[], byte[]> Encode;
            public Func<byte[], byte[]> Decode;
        }
        private static Dictionary<FileType, Codec> Codecs = new Dictionary<FileType, Codec>();
        internal static void RegisterCodec(FileType fileType, Func<byte[], byte[]> decode, Func<byte[], byte[]> encode)
        {
            Codecs[fileType] = new Codec { Encode = encode, Decode = decode };
        }
        internal static byte[] Encode(FileType fileType, byte[] input)
        {
            if (Codecs.ContainsKey(fileType))
            {
                var codec = Codecs[fileType];
                if (codec.Encode != null)
                {
                    return codec.Encode(input);
                }
            }
            return input;
        }
        internal static byte[] Decode(FileType fileType, byte[] input)
        {
            if (Codecs.ContainsKey(fileType))
            {
                var codec = Codecs[fileType];
                if (codec.Decode != null)
                {
                    return codec.Decode(input);
                }
            }
            return input;
        }

        public static byte[] ConvertFile(byte[] data, FileType type)
        {
            return Decode(type, data);
        }

        internal static void RegisterEncodec(FileType fileType, Func<byte[], byte[]> encode)
        {
            throw new NotImplementedException();
        }

        internal static void RegisterDecodec(FileType fileType, Func<byte[], byte[]> decode)
        {
            throw new NotImplementedException();
        }
    }
}
