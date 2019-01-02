using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{
    public enum DataType : ushort
    {
        Boolean = 0x1,
        SByte = 0x2,
        Int16 = 0x3,
        Int32 = 0x4,
        Int64 = 0x5,
        Byte = 0x6,
        UInt16 = 0x7,
        UInt32 = 0x8,
        UInt64 = 0x9,
        String = 0xA,
        Single = 0xB,
        Double = 0xC,
        Locale = 0xD,
        Guid = 0xE,
        Enum = 0xF,

        Class = 0x0010,
        StrongPointer = 0x0110,
        WeakPointer = 0x0210,
        Reference = 0x0310,
    }
}
