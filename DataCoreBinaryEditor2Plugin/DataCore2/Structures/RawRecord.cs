using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2.Structures
{
#pragma warning disable CS0649
    internal struct RawRecord
    {
        public Int32 NameOffset;
        public Int32 FileNameOffset; // !IsLegacy
        public Int32 StructureIndex;
        public Guid ID;
        public UInt16 VariantIndex;
        public UInt16 OtherIndex;
    }
}
