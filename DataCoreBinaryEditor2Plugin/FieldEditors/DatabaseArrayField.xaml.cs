using DataCore2;
using DataCore2.ManagedTypeConstruction;
using DataCore2.Structures;
using DataCoreBinary2.FieldEditors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    /// <summary>
    /// Interaction logic for DatabaseBoolField.xaml
    /// </summary>
    internal partial class DatabaseArrayField : DatabaseFieldEditorBase
    {
        private bool _IsExpanded = true;
        public bool IsExpanded { get => _IsExpanded; set => this.SetProperty(ref _IsExpanded, value && ArrayLength > 0); }

        public IDataCoreCollection Value
        {
            get => GetRawData() as IDataCoreCollection;
            set
            {
                SetRawData(value);

                var arrayChanged = this.Value as INotifyCollectionChanged;
                if (arrayChanged != null)
                {
                    arrayChanged.CollectionChanged += ArrayChanged_CollectionChanged;
                }
            }
        }
        public int ArrayLength => (Value as dynamic)?.Count ?? 0;
        //{
        //    get => _ArrayLength; set
        //    {
        //        var old_index = CurrentIndex;
        //        var old_is_min = IsMinIndex;
        //        var old_is_max = IsMaxIndex;
        //        this.SetProperty(ref _ArrayLength, value);
        //        var new_is_min = IsMinIndex;
        //        var new_is_max = IsMaxIndex;
        //        if (old_is_max != new_is_max) this.OnPropertyChanged("IsMaxIndex");
        //        if (old_is_min != new_is_min) this.OnPropertyChanged("IsMinIndex");
        //        CurrentIndex = old_index; // ensures CurrentIndex update
        //    }
        //}

        public bool ShowMultiplePreviewItems => MaxPreviewItems > 1;

        public int MaxPreviewItems
        {
            get
            {
                if (PropertyType == typeof(object)) return 5;
                if (PropertyType.IsClass) return 5;
                if (typeof(IDataCoreStructure).IsAssignableFrom(PropertyType))
                {
                    int fields = 40;
                    var properties = Database.ManagedStructureInheritedProperties[PropertyType];
                    var propertyCount = properties.Count();

                    return fields / (propertyCount);
                }
                if (PropertyType == typeof(object)) return 5;
                if (PropertyType.IsEnum) return 5;

                return 1;
            }
        }

        private int _CurrentIndex = -1;
        public int CurrentIndex
        {
            get => _CurrentIndex;
            set
            {
                var index = value;
                if (index < 0) index = 0;
                if (index > ArrayLength) index = ArrayLength - 1;



                var old_is_min = IsMinIndex;
                var old_is_max = IsMaxIndex;
                this.SetProperty(ref _CurrentIndex, index);
                var new_is_min = IsMinIndex;
                var new_is_max = IsMaxIndex;
                if (old_is_max != new_is_max) this.OnPropertyChanged("IsMaxIndex");
                if (old_is_min != new_is_min) this.OnPropertyChanged("IsMinIndex");
            }
        }

        public bool IsMinIndex => CurrentIndex == 0;
        public bool IsMaxIndex => (CurrentIndex + 1) >= ArrayLength;

        private bool _IsReadOnly = false;
        public bool IsReadOnly { get => _IsReadOnly; set => this.SetProperty(ref _IsReadOnly, value); }

        public DatabaseArrayField(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
            : base(database, instance, property, arrayIndex)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            this.DataContext = this;
            this.PropertyChanged += DatabaseArrayField_PropertyChanged;

            var arrayChanged = this.Value as INotifyCollectionChanged;
            if(arrayChanged != null)
            {
                arrayChanged.CollectionChanged += ArrayChanged_CollectionChanged;
            }

            SetupView();
        }

        private void ArrayChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("ArrayLength");
        }

        private void SetupView()
        {
            try
            {



                //var structure_array_data = Database.dataTable[Database.dataTableMap[Pointer.StructIndex]];
                //var variant_array_data = structure_array_data[Pointer.VariantIndex];
                ////var data = GetData<DataCore.DataCore.StructureData>();

                //var raw_property_data = Database.dataTable[Database.dataTableMap[Pointer.StructIndex]][Pointer.VariantIndex][PropertyIndex];
                //var property_data = raw_property_data as IList;
                //var properties_count = property_data?.Count ?? 0;
                ////var properties_count = 0;
                //if (PropertyName == "StaticEntityClassData")
                //{
                //    //Console.WriteLine();


                //    //var property_data_dyn = (dynamic)property_data;
                //    //var Pointer = (Pointer)property_data_dyn[0];

                //    //var Struct = dataCore.structureDefinitions[Pointer.StructType];
                //    //var StructName = dataCore.textBlock.GetString(Struct.NameOffset);

                //    //var Pointer2 = dataCore.strongValues[Pointer.Index];

                //    //var Struct2 = dataCore.structureDefinitions[Pointer2.StructType];
                //    //var Struct2Name = dataCore.textBlock.GetString(Struct2.NameOffset);



                //    //var struct2_mapped_index = dataCore.dataTableMap[Pointer2.StructType];
                //    //var default_entitlement_entity_params = dataCore.dataTable[struct2_mapped_index][Pointer2.Index];

                //    //var reference = default_entitlement_entity_params[0];


                //    //Console.WriteLine();


                //}

                //var properties = Database.ManagedStructureProperties[PropertyType];

                int start_index = Math.Max(0, CurrentIndex);
                int end_index = Math.Min(start_index + Math.Max(MaxPreviewItems, 1), ArrayLength);

                List<UIElement> uIElements = new List<UIElement>();
                //if (properties_count > 0)
                //{
                for (int array_index = start_index; array_index < end_index; array_index++)
                {
                    var property_field_ui = DatabaseStructureView.CreateUIElement(Database, Instance, Property, array_index);
                    uIElements.Add(property_field_ui);
                }
                //}

                Root.Children.Clear();
                foreach (var uiElement in uIElements)
                {
                    Root.Children.Add(uiElement);
                }

                // set the index if we have elements
                if (ArrayLength > 0 && CurrentIndex == -1)
                {
                    CurrentIndex = 0;
                }
                else if (ArrayLength == 0)
                {
                    CurrentIndex = -1;
                }
            }
            catch (Exception)
            {
                Console.WriteLine();
            }
        }

        private void DatabaseArrayField_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentIndex":

                    SetupView();

                    break;
            }
        }

        private void Increment(object sender, RoutedEventArgs e)
        {
            CurrentIndex++;
        }

        private void Deincrement(object sender, RoutedEventArgs e)
        {
            CurrentIndex--;
        }

        private void CreateInstance(object sender, RoutedEventArgs e)
        {
            if(Value == null)
            {
                var newAarray = Activator.CreateInstance(ArrayType);
                Value = (dynamic)newAarray;
            }
            var newValue = Activator.CreateInstance(PropertyType);
            var dynamicCollection = Value as dynamic;
            dynamicCollection.Add(newValue as dynamic);

            //var raw_property_data = Database.dataTable[Database.dataTableMap[Pointer.StructIndex]][Pointer.VariantIndex][PropertyIndex];
            //var property_data = raw_property_data as IList;
            //var properties_count = property_data?.Count ?? 0;

            //var property_data_ext = property_data as DataCore.ArrayData2<Pointer>;
            ////property_data_ext.FirstIndex = 0x00010ea7;
            ////property_data_ext.ArrayCount = 1;


            //foreach(var record in Database.records)
            //{
            //    var recordName = Database.textBlock.GetString(record.NameOffset);

            //    if(string.Equals(recordName, "EntityClassDefinition.ANVL_Hornet_F7C", StringComparison.OrdinalIgnoreCase))
            //    {
            //        var record_data = Database.dataTable[Database.dataTableMap[record.StructIndex]][record.VariantIndex];

            //        var staticEntityClassData = record_data["StaticEntityClassData"] as DataCore.ArrayData2<Pointer>;

            //        if(staticEntityClassData != null)
            //        {
            //            property_data_ext.FirstIndex = staticEntityClassData.FirstIndex;
            //            property_data_ext.ArrayCount = staticEntityClassData.ArrayCount;
            //            foreach(var pointer in staticEntityClassData)
            //            {
            //                property_data_ext.Add(pointer);
            //            }
            //        }

            //        Console.WriteLine();

            //        break;
            //    }
            //}

            SetupView();
        }

        private void DeleteInstance(object sender, RoutedEventArgs e)
        {
            //if (ArrayLength > 0)
            //{
            //    ArrayLength--;
            //}
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
