namespace DataCore2
{
    public interface IDataCoreWeakPointer
    {
        IDataCoreStructure InstanceObject { get; set; }
    };
    public class DataCoreWeakPointer<T> : DataCorePointerBase<T>, IDataCoreWeakPointer, IDataCorePointer
    {
        public DataCoreWeakPointer() { }
        public DataCoreWeakPointer(T instance)
        {
            Instance = instance;
        }
    }
}
