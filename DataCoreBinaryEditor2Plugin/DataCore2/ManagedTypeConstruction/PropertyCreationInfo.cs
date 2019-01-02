using DataCore2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2.ManagedTypeConstruction
{
    internal struct PropertyCreationInfo
    {
        public string Name { get; set; }
        public Type MemberType { get; set; }
        public bool IsDeclaringType { get; set; }
        public ConversionType ConversionType { get; set; }
        public RawProperty PropertyDefinition { get; internal set; }
        public bool HideUnderlyingType { get; internal set; }
        public Type OvertType { get; internal set; }
    }
}
