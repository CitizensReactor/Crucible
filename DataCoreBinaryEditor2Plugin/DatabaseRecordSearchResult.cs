using DataCore2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataCoreBinary2.Sidebar
{
    class DatabaseRecordSearchResult
    {
        public DataCoreRecord Record { get; set; }
        public DataCoreDatabase Database { get; set; }

        public DatabaseRecordSearchResult(DataCoreDatabase dataCore, DataCoreRecord record)
        {
            this.Database = dataCore;
            this.Record = record;
        }

        public string Title => Record.Name.Substring(Record.StructureType.Name.Length + 1);
        public string SearchString => $"{Record.Name} {Record.FileName}";
        public string StructureName => Record.StructureType.Name;

        static void SortCollection()
        {

        }

        public static IEnumerable<DatabaseRecordSearchResult> SortCollection(IEnumerable<DatabaseRecordSearchResult> collection)
        {
            // sort
            List<DatabaseRecordSearchResult> items = collection.ToList();
            items.Sort((emp1, emp2) => emp1.Title.CompareTo(emp2.Title));

            // create original object
            var type = collection.GetType();
            var result = (dynamic)Activator.CreateInstance(type, new object[] { items });

            return result;
        }
    }
}
