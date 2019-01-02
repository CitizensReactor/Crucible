using Binary;
using System;
using System.Reflection;
using DataCore2.ManagedTypeConstruction;

namespace DataCore2
{
    public partial class DataCoreDatabase
    {
        internal class StrongPointerArrayFixup : ArrayFixupBase
        {
            internal StrongPointerArrayFixup(DataCoreDatabase database, object instance, PropertyInfo info, int firstIndex, int arrayCount)
                : base(database, instance, info, firstIndex, arrayCount) { }

            public override void Run()
            {
                var propertyType = Property.PropertyType;
                var collectionType = propertyType;
                propertyType = propertyType.GenericTypeArguments[0];
                var propertyArrayType = propertyType.MakeArrayType();

                var pointerValues = BinaryBlobReader.FastCopySafe(Database.RawDatabase.strongValues, FirstIndex, ArrayCount);
                dynamic pointersArray = Activator.CreateInstance(propertyArrayType, (object)ArrayCount);
                for (int i = 0; i < ArrayCount; i++)
                {
                    var instance = Database.ResolvePointer(pointerValues[i]);
                    dynamic pointer = Activator.CreateInstance(propertyType, (object)instance);
                    pointersArray[i] = pointer;
                }

                var pointerCollection = Activator.CreateInstance(collectionType, (object)pointersArray);
                Property.SetValue(Instance, pointerCollection);
            }
        }
    }
}
