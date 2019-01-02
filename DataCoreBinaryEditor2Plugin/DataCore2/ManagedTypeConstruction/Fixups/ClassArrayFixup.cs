using System;
using System.Reflection;
using DataCore2.ManagedTypeConstruction;

namespace DataCore2
{
    public partial class DataCoreDatabase
    {
        internal class ClassArrayFixup : ArrayFixupBase
        {
            internal ClassArrayFixup(DataCoreDatabase database, object instance, PropertyInfo info, int firstIndex, int arrayCount)
                : base(database, instance, info, firstIndex, arrayCount) { }

            public override void Run()
            {
                var propertyType = Property.PropertyType;
                var collectionType = propertyType;
                propertyType = propertyType.GenericTypeArguments[0];

                var classArrayType = propertyType.MakeArrayType();
                dynamic classArray = Activator.CreateInstance(classArrayType, new object[] { ArrayCount });
                for (int i = 0; i < ArrayCount; i++)
                {
                    dynamic classInstance = Database.ManagedDataTable[propertyType][FirstIndex + i];
                    classArray[i] = classInstance;
                }

                var classCollection = Activator.CreateInstance(collectionType, new object[] { classArray });
                Property.SetValue(Instance, classCollection);
            }
        }
    }
}
