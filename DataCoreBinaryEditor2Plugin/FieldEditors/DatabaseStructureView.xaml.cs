using DataCore2;
using DataCoreBinary2.DatabaseFieldEditors;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DataCoreBinary2.FieldEditors
{
    /// <summary>
    /// Interaction logic for DatabaseRecordView.xaml
    /// </summary>
    internal partial class DatabaseStructureView : UserControl
    {
        public static UIElement CreateUIElement(DataCoreDatabase database, object instance, PropertyInfo property, int array_index = -1)
        {
            try
            {
                var propertyType = property.PropertyType;
                if(DatabaseFieldEditorBase.IsTypeArray(property.PropertyType))
                {
                    propertyType = propertyType.GenericTypeArguments[0];
                }

                if (propertyType.IsPrimitive)
                {
                    if (propertyType == typeof(bool))
                    {
                        return new DatabaseBoolField(database, instance, property, array_index);
                    }

                    return new DatabaseNumberField(database, instance, property, array_index);
                }
                else if (propertyType == typeof(Guid))
                {
                    return new UnsupportedField(property);
                }
                else if (propertyType.IsEnum)
                {
                    return new DatabaseEnumField(database, instance, property, array_index);
                }
                else if (typeof(IDataCoreString).IsAssignableFrom(propertyType))
                {
                    return new DatabaseStringField(database, instance, property, array_index);
                }
                else if (typeof(IDataCoreLocale).IsAssignableFrom(propertyType))
                {
                    return new DatabaseStringField(database, instance, property, array_index);
                }
                else if (typeof(IDataCoreStrongPointer).IsAssignableFrom(propertyType))
                {
                    return new DatabasePointerField(database, instance, property, array_index);
                }
                else if (typeof(IDataCoreWeakPointer).IsAssignableFrom(propertyType))
                {
                    return new DatabasePointerField(database, instance, property, array_index);
                }
                else if (typeof(IDataCoreStructure).IsAssignableFrom(propertyType))
                {
                    return new DatabaseClassField(database, instance, property, array_index);
                }
                else if (typeof(IDataCoreRecord).IsAssignableFrom(propertyType))
                {
                    return new DatabaseReferenceField(database, instance, property, array_index);
                }
                return new UnsupportedField(property);
            }
            catch (Exception)
            {
                return new ErrorField(property);
            }
        }

        //public DataCoreRecord Record { get; }
        public object Instance { get; }
        public DataCoreDatabase Database { get; }

        //public DatabaseStructureView(DataCoreDatabase database, DataCoreRecord record, bool isRoot = false)
        //    : this(database, new Pointer { StructIndex = record.StructIndex, VariantIndex = record.VariantIndex }, isRoot)
        //{

        //}

        public DatabaseStructureView(DataCoreDatabase database, object instance, bool isRoot = false)
        {
            this.Database = database;
            this.Instance = instance;
            this.DataContext = this;
            InitializeComponent();

            var type = instance.GetType();
#if DEBUG
            if (!typeof(IDataCoreStructure).IsAssignableFrom(type))
            {
                throw new Exception("Invalid type used in DatabaseStructureView");
            }
#endif

            var fieldsStackPanel = new StackPanel();

            if (database.ManagedStructureInheritedProperties.ContainsKey(type))
            {
                var properties = database.ManagedStructureInheritedProperties[type];

                foreach (var property in properties)
                {
                    UIElement root_ui_element = null;
                    var propertyType = property.PropertyType;

                    if (DatabaseFieldEditorBase.IsTypeArray(propertyType))
                    {
                        root_ui_element = new DatabaseArrayField(database, instance, property);
                    }
                    else
                    {
                        var property_field_ui = CreateUIElement(database, instance, property);
                        if (property_field_ui == null)
                        {
                            TextBox textbox = new TextBox();
                            textbox.Text = $"{property.Name} {propertyType.Name} is unsupported";
                            property_field_ui = textbox;
                        }
                        root_ui_element = property_field_ui;
                    }

                    if (root_ui_element != null)
                    {
                        fieldsStackPanel.Children.Add(root_ui_element);
                    }
                }

                if (isRoot)
                {
                    scrollViewer.Content = fieldsStackPanel;
                }
                else
                {
                    Root.Children.Clear();
                    Root.Children.Add(fieldsStackPanel);
                }
            }
            else
            {
                Crucible.MainWindow.SetStatus($"Invalid record {type.Name} what the heck!??? Please report this!!!!");
            }
        }
    }
}
