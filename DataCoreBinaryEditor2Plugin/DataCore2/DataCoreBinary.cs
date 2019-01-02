using Binary;
using DataCore2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{
    internal class DataCoreBinary
    {
        internal RawHeader header;
        internal RawStructure[] structureDefinitions;
        internal RawProperty[] propertyDefinitions;
        internal RawEnum[] enumDefinitions;
        internal RawDataMapping[] datamappingDefinitions;
        internal RawRecord[] records;
        internal sbyte[] int8Values;
        internal Int16[] int16Values;
        internal Int32[] int32Values;
        internal Int64[] int64Values;
        internal byte[] UInt8Values;
        internal UInt16[] UInt16Values;
        internal UInt32[] UInt32Values;
        internal UInt64[] UInt64Values;
        internal bool[] booleanValues;
        internal float[] singleValues;
        internal double[] doubleValues;
        internal Guid[] guidValues;
        internal RawStringReference[] stringValues;
        internal RawLocaleReference[] localeValues;
        internal Int32[] enumValues;
        internal RawStrongPointer[] strongValues;
        internal RawWeakPointer[] weakValues;
        internal RawReference[] referenceValues;
        internal RawStringReference[] enumValueNameTable;
        internal TextBlock textBlock;

        public BinaryBlobReader Reader { get; }

        internal byte[] DataChunk;

        internal DataCoreBinary(byte[] data)
        {
            Reader = new BinaryBlobReader(data, 0);

            header = Reader.Read<RawHeader>();

            structureDefinitions = Reader.Read<RawStructure>(header.structDefinitionCount);
            propertyDefinitions = Reader.Read<RawProperty>(header.propertyDefinitionCount);
            enumDefinitions = Reader.Read<RawEnum>(header.enumDefinitionCount);
            datamappingDefinitions = Reader.Read<RawDataMapping>(header.dataMappingCount);
            records = Reader.Read<RawRecord>(header.recordDefinitionCount);

            int8Values = Reader.Read<sbyte>(header.int8ValueCount);
            int16Values = Reader.Read<Int16>(header.int16ValueCount);
            int32Values = Reader.Read<Int32>(header.int32ValueCount);
            int64Values = Reader.Read<Int64>(header.int64ValueCount);
            UInt8Values = Reader.Read<byte>(header.uInt8ValueCount);
            UInt16Values = Reader.Read<UInt16>(header.uInt16ValueCount);
            UInt32Values = Reader.Read<UInt32>(header.uInt32ValueCount);
            UInt64Values = Reader.Read<UInt64>(header.uInt64ValueCount);
            booleanValues = Reader.Read<bool>(header.booleanValueCount);

            singleValues = Reader.Read<float>(header.singleValueCount);
            doubleValues = Reader.Read<double>(header.doubleValueCount);

            guidValues = Reader.Read<Guid>(header.guidValueCount);

            stringValues = Reader.Read<RawStringReference>(header.stringValueCount);
            localeValues = Reader.Read<RawLocaleReference>(header.localeValueCount);

            enumValues = Reader.Read<Int32>(header.enumValueCount);
            strongValues = Reader.Read<RawStrongPointer>(header.strongValueCount);
            weakValues = Reader.Read<RawWeakPointer>(header.weakValueCount);
            referenceValues = Reader.Read<RawReference>(header.referenceValueCount);
            enumValueNameTable = Reader.Read<RawStringReference>(header.enumOptionCount);

            textBlock = new TextBlock(Reader, header.textLength);

            var start = Reader.Position;
            DataChunk = Reader.Read<byte>(Reader.Length - start);
            Reader.Position = start;

#if DEBUG
            Console.WriteLine("--------------------------- READ DCB ---------------------------");
            Console.WriteLine($"structureDefinitions: {structureDefinitions.Length}");
            Console.WriteLine($"propertyDefinitions: {propertyDefinitions.Length}");
            Console.WriteLine($"enumDefinitions: {enumDefinitions.Length}");
            Console.WriteLine($"datamappingDefinitions: {datamappingDefinitions.Length}");
            Console.WriteLine($"records: {records.Length}");
            Console.WriteLine();
            Console.WriteLine($"int8Values: {int8Values.Length}");
            Console.WriteLine($"int16Values: {int16Values.Length}");
            Console.WriteLine($"int32Values: {int32Values.Length}");
            Console.WriteLine($"int64Values: {int64Values.Length}");
            Console.WriteLine($"UInt8Values: {UInt8Values.Length}");
            Console.WriteLine($"UInt16Values: {UInt16Values.Length}");
            Console.WriteLine($"UInt32Values: {UInt32Values.Length}");
            Console.WriteLine($"UInt64Values: {UInt64Values.Length}");
            Console.WriteLine($"booleanValues: {booleanValues.Length}");
            Console.WriteLine();
            Console.WriteLine($"singleValues: {singleValues.Length}");
            Console.WriteLine($"doubleValues: {doubleValues.Length}");
            Console.WriteLine();
            Console.WriteLine($"stringValues: {stringValues.Length}");
            Console.WriteLine($"localeValues: {localeValues.Length}");
            Console.WriteLine();
            Console.WriteLine($"enumValues: {enumValues.Length}");
            Console.WriteLine($"strongValues: {strongValues.Length}");
            Console.WriteLine($"weakValues: {weakValues.Length}");
            Console.WriteLine($"referenceValues: {referenceValues.Length}");
            Console.WriteLine($"enumValueNameTable: {enumValueNameTable.Length}");
            Console.WriteLine();
            Console.WriteLine($"textBlock: {textBlock.Size}");
            Console.WriteLine("------------------------- END READ DCB -------------------------");
#endif
        }
    }
}
