using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2.Structures
{
#pragma warning disable CS0649
    internal struct RawProperty
    {
        public Int32 NameOffset;
        public UInt16 DefinitionIndex;
        public DataType DataType;
        public ConversionType ConversionType;
        public UInt16 Padding;
    }
}
