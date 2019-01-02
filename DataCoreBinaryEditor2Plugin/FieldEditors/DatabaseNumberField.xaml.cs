using DataCore2;
using System;
using System.Reflection;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    /// <summary>
    /// Interaction logic for DatabaseBoolField.xaml
    /// </summary>
    internal partial class DatabaseNumberField : DatabaseFieldEditorBase
    {
        public bool IsFloat => PropertyType == typeof(Single) || PropertyType == typeof(Double);

        public string Value
        {
            get
            {
                var data = GetData<dynamic>();
                if (data != null)
                {
                    return data.ToString();
                }
                throw new Exception("Failed to find value");
            }
            set
            {
                var method = PropertyType.GetMethod("Parse", BindingFlags.Public);
                var parsedValue = method.Invoke(null, new object[] { value });
                SetRawData(parsedValue);
            }
        }

        public DatabaseNumberField(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
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
