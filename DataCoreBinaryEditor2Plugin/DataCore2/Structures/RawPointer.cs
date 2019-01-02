using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2.Structures
{
#pragma warning disable CS0649
    internal struct RawStrongPointer
    {
        public Int32 StructIndex;
        public Int32 VariantIndex;

        public override string ToString()
        {
            return $"Struct:{StructIndex} Variant:{VariantIndex}";
        }
    }

    internal struct RawWeakPointer
    {
        public Int32 StructureIndex;
        public Int32 VariantIndex;

        public override string ToString()
        {
            return $"Struct:{StructureIndex} Variant:{VariantIndex}";
        }
    }
}
