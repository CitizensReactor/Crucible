using Crucible;
using DataCore2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    /// <summary>
    /// Interaction logic for DatabaseBoolField.xaml
    /// </summary>
    internal partial class DatabaseReferenceField : DatabaseFieldEditorBase
    {
        public DataCoreRecord Value
        {
            get => GetData<DataCoreRecord>();
            set => SetProperty(value);
        }

        public int ValueIndex
        {
            get
            {
                var currentValue = Value;
                if (currentValue == null) return -1;
                if (RecordNameReferences == null) return 1;

                var index = RecordNameReferences.IndexOf(currentValue);
                return index;
            }
            set
            {
                _SelectedStructureIndex = -1;
                if (value == -1)
                {
                    Value = null;
                }
                else
                {
                    Value = RecordNameReferences[value];
                }
            }


        }

        private int _SelectedStructureIndex = -1;
        public int StructureTypeIndex
        {
            get
            {
                if (typeof(IDataCoreRecord).IsAssignableFrom(PropertyType))
                {
                    return 0;
                }

                var currentValue = Value?.StructureType;
                if (currentValue == null) return _SelectedStructureIndex;
                if (StructureNameReferences == null) return -1;

                var index = StructureNameReferences.IndexOf(currentValue);
                return index;
            }
            set
            {
                var currentStructure = Value?.StructureType;
                if (value == -1)
                {
                    Value = null;
                }
                _SelectedStructureIndex = value;
            }
        }

        internal Type StructureType => Value?.StructureType ?? (StructureNameReferences.Count == 1 ? StructureNameReferences[0] : PropertyType.GenericTypeArguments[0]);

        internal class TempRecordNameReference
        {
            public string Name { get; set; }
            public int RecordIndex { get; set; }
        }

        public DatabaseReferenceField(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
            : base(database, instance, property, arrayIndex)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            this.DataContext = this;

            UpdateOptions();

            cbType.SelectionChanged += CbType_SelectionChanged;

            this.PropertyChanged += DatabaseReferenceField_PropertyChanged;
        }

        private void DatabaseReferenceField_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "Value":
                    UpdateOptions();
                    break;
            }
        }

        List<Type> StructureNameReferences;
        private void UpdateTypeOptions()
        {
            

            var structureNameReferencesHashSet = new HashSet<Type>();

            if(typeof(IDataCoreRecord).IsAssignableFrom(PropertyType))
            {
                var structureType = this.PropertyType.GenericTypeArguments[0];
                structureNameReferencesHashSet.Add(structureType);
            }
            else
            {
                foreach (var recordKeyPair in Database.ManagedGUIDTable)
                {
                    var record = recordKeyPair.Value;
                    structureNameReferencesHashSet.Add(record.StructureType);
                }
            }

            StructureNameReferences = structureNameReferencesHashSet.OrderBy(o => o.Name).ToList();
            cbType.ItemsSource = StructureNameReferences;
        }

        List<DataCoreRecord> RecordNameReferences;
        private void UpdateRecordOptions()
        {
            RecordNameReferences = new List<DataCoreRecord>();
            foreach (var recordKeyPair in Database.ManagedGUIDTable)
            {
                var record = recordKeyPair.Value;

                if (!(StructureType == null || record.StructureType == StructureType)) continue;
                if (record.StructureType != StructureType) continue;

                RecordNameReferences.Add(record);
            }

            RecordNameReferences = RecordNameReferences.OrderBy(o => o.Name).ToList();

            cbRecord.ItemsSource = RecordNameReferences;
        }

        private void CbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRecordOptions();
        }

        private void UpdateOptions()
        {
            UpdateTypeOptions();
            UpdateRecordOptions();
        }

        private void SetNull(object sender, RoutedEventArgs e)
        {
            // Force this because WPF C# is retarded and bindings are broke
            cbType.SelectedIndex = -1;
            cbRecord.SelectedIndex = -1;

            ValueIndex = -1;
            StructureTypeIndex = -1;
        }

        private void OpenReferenceTab(object sender, RoutedEventArgs e)
        {
            var parent = CrucibleUtil.FindParent<DatabaseFile>(this);
            if (parent == null) throw new Exception("Couldn't find parent");

            var record = GetData<DataCoreRecord>();
            if (record != null)
            {
                parent.OpenDatabaseRecord(Database, record);
            }
        }
    }
}
