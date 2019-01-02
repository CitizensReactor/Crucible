using DataCore2.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{

    internal static class DataCore2Util
    {
        internal static RawStrongPointer ConvertStrongPointer(DataCoreCompiler compiler, DataCoreDatabase database, IDataCorePointer dataCorePointer)
        {
            var rawStrongPointer = new RawStrongPointer();
            rawStrongPointer.StructIndex = -1;
            rawStrongPointer.VariantIndex = -1;

            var pointerValue = dataCorePointer;
            var instance = pointerValue?.InstanceObject;

            if (instance != null)
            {
                var structureType = instance.GetType();
                var structureIndex = database.ManagedStructureTypes.IndexOf(structureType);

                var structureDataTable = compiler.ManagedDataTable[structureType];
                //TODO: Instead of appending the data, we should rebuild from scratch each time
                if (!structureDataTable.Contains(instance)) 
                {
                    structureDataTable.Add(instance);
                }
                var variantIndex = structureDataTable.IndexOf(instance);

                rawStrongPointer.StructIndex = structureIndex;
                rawStrongPointer.VariantIndex = variantIndex;
            }

            if(rawStrongPointer.StructIndex == -1 || rawStrongPointer.VariantIndex == -1)
            {
                if(rawStrongPointer.StructIndex != -1 && rawStrongPointer.VariantIndex != -1)
                {
                    throw new Exception("Invalid strong pointer");
                }
            }

            return rawStrongPointer;
        }

        internal static RawWeakPointer ConvertWeakPointer(DataCoreCompiler compiler, DataCoreDatabase database, IDataCorePointer dataCorePointer)
        {
            var rawWeakPointer = new RawWeakPointer();
            rawWeakPointer.StructureIndex = -1;
            rawWeakPointer.VariantIndex = -1;

            var pointerValue = dataCorePointer;
            var instance = pointerValue?.InstanceObject;

            if (instance != null)
            {
                var structureType = instance.GetType();
                var structureIndex = database.ManagedStructureTypes.IndexOf(structureType);

                var structureDataTable = compiler.ManagedDataTable[structureType];
                //TODO: Instead of appending the data, we should rebuild from scratch each time
                if (!structureDataTable.Contains(instance))
                {
                    structureDataTable.Add(instance);
                }
                var variantIndex = structureDataTable.IndexOf(instance);

                rawWeakPointer.StructureIndex = structureIndex;
                rawWeakPointer.VariantIndex = variantIndex;
            }

            return rawWeakPointer;
        }

        public static byte[] SHA256(byte[] bytes)
        {
            var crypt = new SHA256Managed();
            byte[] crypto = crypt.ComputeHash(bytes);
            return crypto;
        }

        public static byte[] Compress(byte[] data)
        {
            byte[] compressArray = null;
            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionLevel.Optimal))
                    {
                        deflateStream.Write(data, 0, data.Length);
                    }
                    compressArray = memoryStream.ToArray();
                }
            }
            catch (Exception)
            {

            }
            return compressArray;
        }

        public static byte[] Decompress(byte[] data)
        {
            byte[] decompressedArray = null;
            try
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    using (MemoryStream compressStream = new MemoryStream(data))
                    {
                        using (DeflateStream deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(decompressedStream);
                        }
                    }
                    decompressedArray = decompressedStream.ToArray();
                }
            }
            catch (Exception)
            {

            }

            return decompressedArray;
        }
    }
}
