using DataCore2.Structures;
using System;
using System.Reflection;
using DataCore2.ManagedTypeConstruction;

namespace DataCore2
{
    public partial class DataCoreDatabase
    {
        internal class StrongPointerFixup : FixupBase
        {
            private RawStrongPointer Pointer;

            internal StrongPointerFixup(DataCoreDatabase database, object instance, PropertyInfo info, RawStrongPointer pointer)
                : base(database, instance, info) { Pointer = pointer; }

            public override void Run()
            {
                var instance = Database.ResolvePointer(Pointer);
                var pointer = Activator.CreateInstance(Property.PropertyType, (object)instance);
                Property.SetValue(Instance, pointer);
            }
        }
    }
}
