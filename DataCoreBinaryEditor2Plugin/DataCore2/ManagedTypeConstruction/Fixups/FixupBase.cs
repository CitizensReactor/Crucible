using System.Reflection;

namespace DataCore2.ManagedTypeConstruction
{
    internal abstract class FixupBase : IClassFixup
    {
        protected object Instance;
        protected PropertyInfo Property;
        protected DataCoreDatabase Database;

        internal FixupBase(DataCoreDatabase database, object instance, PropertyInfo property)
        {
            Database = database;
            Instance = instance;
            Property = property;
        }

        public abstract void Run();
    }
}
