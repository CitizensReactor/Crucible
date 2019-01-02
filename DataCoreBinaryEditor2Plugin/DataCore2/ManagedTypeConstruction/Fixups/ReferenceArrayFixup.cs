using Binary;
using DataCore2.Structures;
using System;
using System.Reflection;
using DataCore2.ManagedTypeConstruction;

namespace DataCore2
{
    public partial class DataCoreDatabase
    {
        internal class ReferenceArrayFixup : ArrayFixupBase
        {
            internal ReferenceArrayFixup(DataCoreDatabase database, object instance, PropertyInfo info, int firstIndex, int arrayCount)
                : base(database, instance, info, firstIndex, arrayCount) { }

            public override void Run()
            {
                var propertyType = Property.PropertyType;
                var collectionType = propertyType;
                propertyType = propertyType.GenericTypeArguments[0];
                var propertyArrayType = propertyType.MakeArrayType();

                var referenceValues = BinaryBlobReader.FastCopySafe(Database.RawDatabase.referenceValues, FirstIndex, ArrayCount);
                dynamic recordsArray = Activator.CreateInstance(propertyArrayType, (object)ArrayCount);
                for (int i = 0; i < ArrayCount; i++)
                {
                    var reference = referenceValues[i];


                    if (reference.VariantIndex == -1)
                    {
                        //NOTE: This is for data preservation.
                        // If we keep these ID's around from the previous valid, we can try and reconstruct
                        // missing data potentially?

                        // Not 100% sure what the fuck is with these values.
                        // its as if they have a reference, but its been deleted or its not included in the build???

                        // these records don't exist but we can keep the GUID around using an unreferenced record
                        //var unreferencedRecord = new DataCoreRecord(Database);
                        var unreferencedRecord = Activator.CreateInstance(propertyType) as DataCoreRecord;
                        unreferencedRecord.ID = reference.Value;
                        recordsArray[i] = (dynamic)unreferencedRecord;
                    }
                    else
                    {
                        //NOTE: This should ALWAYS be valid. Do NOT check against the GUID Table
                        // if the GUID is missing from the table, we need to crash and figure out how to handle
                        // the reference properly

                        var record = Database.ManagedGUIDTable[reference.Value];
                        recordsArray[i] = (dynamic)record;
                    }
                }

#if DEBUG // validation
                foreach (object value in recordsArray)
                {
                    if (value == null)
                    {
                        throw new Exception("ReferenceArrayFixup values should never be null!");
                    }
                }
#endif

                var classCollection = Activator.CreateInstance(collectionType, (object)recordsArray);
                Property.SetValue(Instance, classCollection);
            }
        }
    }
}

