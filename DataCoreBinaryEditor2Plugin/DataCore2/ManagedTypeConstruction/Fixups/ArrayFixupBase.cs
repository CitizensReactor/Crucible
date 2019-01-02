using System.Reflection;

namespace DataCore2.ManagedTypeConstruction
{
    internal abstract class ArrayFixupBase : IClassFixup
    {
        protected object Instance;
        protected PropertyInfo Property;
        protected int FirstIndex;
        protected int ArrayCount;
        protected DataCoreDatabase Database;

        internal ArrayFixupBase(DataCoreDatabase database, object instance, PropertyInfo property, int firstIndex, int arrayCount)
        {
            Database = database;
            Instance = instance;
            Property = property;
            FirstIndex = firstIndex;
            ArrayCount = arrayCount;
        }

        public abstract void Run();
    }
}
