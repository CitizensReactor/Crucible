using DataCore2.Structures;
using System;
using System.Reflection;

namespace DataCore2.ManagedTypeConstruction
{
    internal class ReferenceFixup : FixupBase
    {
        private RawReference Reference;

        internal ReferenceFixup(DataCoreDatabase database, object instance, PropertyInfo info, RawReference reference)
            : base(database, instance, info) { Reference = reference; }

        public override void Run()
        {
            var reference = Reference;
            
            if (reference.VariantIndex == -1)
            {
                //NOTE: This is for data preservation.
                // If we keep these ID's around from the previous valid, we can try and reconstruct
                // missing data potentially?

                // Not 100% sure what the fuck is with these values.
                // its as if they have a reference, but its been deleted or its not included in the build???

                // these records don't exist but we can keep the GUID around using an unreferenced record
                //var unreferencedRecord = new DataCoreRecord(Database);
                var unreferencedRecord = Activator.CreateInstance(Property.PropertyType) as DataCoreRecord;
                unreferencedRecord.ID = reference.Value;
                Property.SetValue(Instance, unreferencedRecord);
            }
            else
            {
                //NOTE: This should ALWAYS be valid. Do NOT check against the GUID Table
                // if the GUID is missing from the table, we need to crash and figure out how to handle
                // the reference properly

                var record = Database.ManagedGUIDTable[reference.Value];
                Property.SetValue(Instance, record);
            }
        }
    }
}
