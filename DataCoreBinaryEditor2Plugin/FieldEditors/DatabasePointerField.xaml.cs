using DataCore2;
using DataCoreBinary2.FieldEditors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    /// <summary>
    /// Interaction logic for DatabaseBoolField.xaml
    /// </summary>
    internal partial class DatabasePointerField : DatabaseFieldEditorBase
    {
        public Type ValueType => Value?.GetType() ?? PropertyType.GenericTypeArguments[0];

        public IDataCoreStructure Value
        {
            get
            {
                var pointer = GetData<IDataCorePointer>();
                return pointer?.InstanceObject as IDataCoreStructure;
            }
            set
            {
                var isNullValueBefore = IsNull;
                var valueBefore = Value;

                var pointerValue = GetData<IDataCorePointer>();
                //if(pointerValue == null)
                //{
                //    pointerValue = Activator.CreateInstance(PropertyType) as IDataCorePointer;
                //    SetData(pointerValue);
                //}
                pointerValue.InstanceObject = value;

                if (valueBefore != value)
                {
                    OnPropertyChanged();
                }
                if (isNullValueBefore != IsNull)
                {
                    OnPropertyChanged("IsNull");
                }
            }
        }

        public IDataCorePointer PointerValue
        {
            get => this.GetData<IDataCorePointer>();
            set => this.SetData<IDataCorePointer>(value);
        }

        public bool IsNull => Value == null ? true : false;

        public DatabasePointerField(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
            : base(database, instance, property, arrayIndex)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            this.DataContext = this;

            SetupView();

            this.PropertyChanged += DatabasePointerField_PropertyChanged;
        }

        void SetupView()
        {
            UIElement innerContent = null;
            try
            {
                if (!IsNull)
                {
                    innerContent = new DatabaseStructureView(Database, Value);
                }
                else
                {
                    innerContent = new ErrorField(Property, "Value is null!");
                }
            }
            catch (Exception e)
            {
                innerContent = new ErrorField(Property, $"Error: {e.Message}");
            }

            Contents.Children.Clear();
            Contents.Children.Add(innerContent);
        }

        private void DatabasePointerField_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Value":
                    OnPropertyChanged("ValueType");
                    break;
            }
        }

        public IEnumerable<Type> FindDerivedTypes(Type baseType)
        {
            return Assembly.GetAssembly(baseType).GetTypes().Where(t => baseType.IsAssignableFrom(t));
        }

        private IDataCoreStructure InitializeStructureInstance(Type type)
        {
            var newValue = Activator.CreateInstance(type);

            var properties = Database.ManagedStructureInheritedProperties[newValue.GetType()];
            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;
                object value = null;

                if (typeof(IDataCoreStructure).IsAssignableFrom(propertyType))
                {
                    value = InitializeStructureInstance(propertyType);
                }
                else
                {
                    value = Activator.CreateInstance(property.PropertyType);
                }
                
                property.SetValue(newValue, value);
            }

            return newValue as IDataCoreStructure;
        }

        private void CreateInstance(object sender, RoutedEventArgs e)
        {
            var pointerType = PropertyType.GenericTypeArguments[0];
            var derivedTypes = FindDerivedTypes(pointerType);

            if(derivedTypes.Count() > 1)
            {
                PointerCreationTypeSelectionWindow pointerCreationTypeSelectionWindow = new PointerCreationTypeSelectionWindow(derivedTypes);
                pointerCreationTypeSelectionWindow.ShowDialog();
                var selectedType = pointerCreationTypeSelectionWindow.SelectedType;

                Value = InitializeStructureInstance(selectedType);
            }
            else
            {
                Value = InitializeStructureInstance(pointerType);
            }

            SetupView();
        }

        private void SetNull(object sender, RoutedEventArgs e)
        {
            Value = null;

            SetupView();
        }
    }
}
