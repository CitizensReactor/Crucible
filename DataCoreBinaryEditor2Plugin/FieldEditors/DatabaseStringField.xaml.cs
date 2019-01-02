using DataCore2;
using System;
using System.Reflection;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    /// <summary>
    /// Interaction logic for DatabaseBoolField.xaml
    /// </summary>
    internal partial class DatabaseStringField : DatabaseFieldEditorBase
    {
        public string Value
        {
            get
            {
                if (typeof(IDataCoreString).IsAssignableFrom(PropertyType))
                {
                    var datacoreString = GetData<DataCoreString>();
                    return datacoreString?.String ?? "";
                }
                else if (typeof(IDataCoreLocale).IsAssignableFrom(PropertyType))
                {
                    var datacoreLocale = GetData<DataCoreLocale>();
                    return datacoreLocale?.String ?? "";
                }
                else throw new NotSupportedException();
            }
            set
            {
                var valueBefore = Value;

                if (typeof(IDataCoreString).IsAssignableFrom(PropertyType))
                {
                    var datacoreString = GetData<DataCoreString>();
                    //if (datacoreString == null)
                    //{
                    //    datacoreString = new DataCoreString(value);
                    //    SetData(datacoreString);
                    //}
                    //else
                    {
                        datacoreString.String = value;
                    }
                    
                }
                else if (typeof(IDataCoreLocale).IsAssignableFrom(PropertyType))
                {
                    var datacoreLocale = GetData<DataCoreLocale>();
                    //if (datacoreLocale == null)
                    //{
                    //    datacoreLocale = new DataCoreLocale(value);
                    //    SetData(datacoreLocale);
                    //}
                    //else
                    {
                        datacoreLocale.String = value;
                    }
                }
                else throw new Exception();

                if (valueBefore != value)
                {
                    OnPropertyChanged();
                }
            }
        }

        public DatabaseStringField(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
            : base(database, instance, property, arrayIndex)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }
}
