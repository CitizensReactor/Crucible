using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataCore2
{
    public interface IDataCoreCollection
    {
        Type GetCollectionType();
        object[] ToObjectArray();
    };
    public interface IConversionAttribute { };
    public interface IConversionClassArray { };
    public interface IConversionComplexArray { };
    public interface IConversionSimpleArray { };

    public class DataCoreCollection<T, TConversion> : ObservableCollection<T>, IDataCoreCollection
    {
        public Type GetCollectionType()
        {
            return typeof(T);
        }

        public object[] ToObjectArray()
        {
            return this.Cast<object>().ToArray();
        }

        private void CheckTConversion()
        {
            bool valid = false;
            valid |= typeof(TConversion) == typeof(IConversionClassArray);
            valid |= typeof(TConversion) == typeof(IConversionComplexArray);
            valid |= typeof(TConversion) == typeof(IConversionSimpleArray);
            if (!valid)
            {
                throw new Exception("Invalid TConversion type");
            }
        }

        public DataCoreCollection()
        {
#if DEBUG
            CheckTConversion();
#endif
        }
        public DataCoreCollection(List<T> list) : base(list)
        {
#if DEBUG
            CheckTConversion();
#endif
        }
        public DataCoreCollection(IEnumerable<T> collection) : base(collection)
        {
#if DEBUG
            CheckTConversion();
#endif
        }
        public DataCoreCollection(T[] array) : base(array)
        {
#if DEBUG
            CheckTConversion();
#endif
        }
    }
}
