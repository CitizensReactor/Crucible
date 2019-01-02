using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2.Structures
{
#pragma warning disable CS0649
    internal struct RawStructure
    {
        public Int32 NameOffset;
        public Int32 ParentTypeIndex;
        public UInt16 PropertyCount;
        public UInt16 FirstPropertyIndex;
        public Int32 StructureSize;
    }
}
