using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2.Structures
{
#pragma warning disable CS0649
    internal struct RawHeader
    {
        public int unknown1;
        public int version;

        public UInt16 unknown2; // !IsLegacy
        public UInt16 unknown3; // !IsLegacy
        public UInt16 unknown4; // !IsLegacy
        public UInt16 unknown5; // !IsLegacy

        public Int32 structDefinitionCount;
        public Int32 propertyDefinitionCount;
        public Int32 enumDefinitionCount;
        public Int32 dataMappingCount;
        public Int32 recordDefinitionCount;

        public Int32 booleanValueCount;
        public Int32 int8ValueCount;
        public Int32 int16ValueCount;
        public Int32 int32ValueCount;
        public Int32 int64ValueCount;
        public Int32 uInt8ValueCount;
        public Int32 uInt16ValueCount;
        public Int32 uInt32ValueCount;
        public Int32 uInt64ValueCount;

        public Int32 singleValueCount;
        public Int32 doubleValueCount;
        public Int32 guidValueCount;
        public Int32 stringValueCount;
        public Int32 localeValueCount;
        public Int32 enumValueCount;
        public Int32 strongValueCount;
        public Int32 weakValueCount;


        public Int32 referenceValueCount;
        public Int32 enumOptionCount;
        public Int32 textLength;

        public Int32 unknown6; // !IsLegacy

    }
}
