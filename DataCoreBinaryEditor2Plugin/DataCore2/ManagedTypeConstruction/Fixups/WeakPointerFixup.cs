using DataCore2.Structures;
using System;
using System.Reflection;

namespace DataCore2.ManagedTypeConstruction
{
    internal class WeakPointerFixup : FixupBase
    {
        private RawWeakPointer Pointer;

        internal WeakPointerFixup(DataCoreDatabase database, object instance, PropertyInfo info, RawWeakPointer pointer)
            : base(database, instance, info) { Pointer = pointer; }

        public override void Run()
        {
            var instance = Database.ResolvePointer(Pointer);
            var pointer = Activator.CreateInstance(Property.PropertyType, (object)instance);
            Property.SetValue(Instance, pointer);
        }
    }
}
