using DataCore2.Structures;
using System;

namespace DataCore2
{
    public interface IDataCoreRecord
    {
        string Name { get; }
        string FileName { get; }
        Type StructureType { get; }
        Guid ID { get; }
        object Instance { get; }
    };

    public class DataCoreRecord : IDataCoreRecord
    {
        public string Name { get; internal set; }
        public string FileName { get; internal set; }
        public Type StructureType { get; internal set; }
        public Guid ID { get; internal set; }
        public object Instance { get; internal set; }

        public string NiceName
        {
            get
            {
                if (StructureType.Name == "Tag")
                {
                    if (Instance != null)
                    {
                        dynamic dynamicInstance = Instance;
                        return dynamicInstance.tagName.String;
                    }
                }
                return Name;
            }
        }

        internal DataCoreRecord(DataCoreDatabase database, RawRecord record)
        {
            ID = record.ID;
            Name = database.RawDatabase.textBlock.GetString(record.NameOffset);
            FileName = database.RawDatabase.textBlock.GetString(record.FileNameOffset);
            StructureType = database.ManagedStructureTypes[record.StructureIndex];
            Instance = database.GetRawRecordInstance(record);

        }

        public DataCoreRecord()
        {

        }
    }

    public class DataCoreRecord<T> : DataCoreRecord
    {
        public DataCoreRecord() : base()
        {
        }

        public DataCoreRecord(DataCoreDatabase database, object record) : this(database, (RawRecord)record)
        {
        }

        internal DataCoreRecord(DataCoreDatabase database, RawRecord record) : base(database, record)
        {
        }
    }
}
