using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2.Structures
{
#pragma warning disable CS0649
    internal struct RawStringReference
    {
        public Int32 NameOffset;
    }

    internal struct RawLocaleReference
    {
        public Int32 NameOffset;
    }

    internal struct RawEnumNameReference
    {
        public Int32 NameOffset;
    }
}
