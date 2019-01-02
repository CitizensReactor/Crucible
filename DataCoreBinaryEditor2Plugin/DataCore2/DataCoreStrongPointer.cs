namespace DataCore2
{
    public interface IDataCoreStrongPointer
    {
        IDataCoreStructure InstanceObject { get; set; }
    };
    public class DataCoreStrongPointer<T> : DataCorePointerBase<T>, IDataCoreStrongPointer, IDataCorePointer
    {
        public DataCoreStrongPointer() { }
        public DataCoreStrongPointer(T instance)
        {
            Instance = instance;
        }
    }
}
