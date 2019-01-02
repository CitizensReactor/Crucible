using DataCore2;
using DataCoreBinary2.FieldEditors;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    /// <summary>
    /// Interaction logic for DatabaseBoolField.xaml
    /// </summary>
    internal partial class DatabaseClassField : DatabaseFieldEditorBase
    {
        public object Value
        {
            get => GetRawData();
            //set => Data[PropertyIndex] = value;
        }

        public DatabaseClassField(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
            : base(database, instance, property, arrayIndex)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            this.DataContext = this;

            Root.Children.Clear();

            var structureView = new DatabaseStructureView(Database, Value);

            Root.Children.Add(structureView);
        }
    }
}
