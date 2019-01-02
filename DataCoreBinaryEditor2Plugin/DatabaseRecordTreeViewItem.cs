using DataCore2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataCoreBinary2.Sidebar
{
    internal class DatabaseRecordTreeViewItem
    {
        public DataCoreRecord Record { get; set; }
        public DataCoreDatabase Database { get; set; }

        private string _Title;
        public string Title { get => Record?.Name.Substring(Record.StructureType.Name.Length + 1) ?? _Title; set => _Title = value; }
        public bool IsRecord => Record != null;

        private ObservableCollection<DatabaseRecordTreeViewItem> _Items = new ObservableCollection<DatabaseRecordTreeViewItem>();
        public ObservableCollection<DatabaseRecordTreeViewItem> Items { get => _Items; set => _Items = value; }

        public DatabaseRecordTreeViewItem(DataCoreDatabase database, DataCoreRecord record)
        {
            this.Database = database;
            this.Record = record;
        }

        public static IEnumerable<DatabaseRecordTreeViewItem> SortCollection(IEnumerable<DatabaseRecordTreeViewItem> collection, bool recursive)
        {
            // sort
            List<DatabaseRecordTreeViewItem> items = collection.ToList();
            items.Sort((emp1, emp2) => emp1.Title.CompareTo(emp2.Title));

            // create original object
            var type = collection.GetType();
            var result = (dynamic)Activator.CreateInstance(type, new object[] { items });

            if (recursive)
            {
                foreach (var item in items)
                {
                    item.Sort(recursive);
                }
            }

            return result;
        }

        public void Sort(bool recursive)
        {
            Items = SortCollection(Items, recursive) as ObservableCollection<DatabaseRecordTreeViewItem>;
        }
    }
}
