using Crucible;
using DataCore2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Data;

namespace DataCoreBinary2.DatabaseFieldEditors
{
    internal class DataForgeEnumWrapper
    {
        public string Title { get; set; }
    }

    internal class EnumToArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            List<string> strings = new List<string>();

            switch(value)
            {
                case Type type:

                    var enumValues = Enum.GetValues(type);
                    foreach(var enumValue in enumValues)
                    {
                        strings.Add(enumValue.ToString());
                    }
                    break;
            }

            return strings;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// Interaction logic for DatabaseField.xaml
    /// </summary>
    internal partial class DatabaseEnumField : DatabaseFieldEditorBase
    {
        public int Value
        {
            get => CrucibleUtil.Cast<int>(GetRawData());
            set => SetRawData(CrucibleUtil.Cast(value, PropertyType));
        }

        public DatabaseEnumField(DataCoreDatabase database, object instance, PropertyInfo property, int arrayIndex = -1)
            : base(database, instance, property, arrayIndex)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            this.DataContext = this;
            
            this.cbEnumOptions.ItemsSource = Enum.GetValues(this.PropertyType);
        }
    }
}
