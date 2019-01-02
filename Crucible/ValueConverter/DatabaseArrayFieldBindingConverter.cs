using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Crucible.ValueConverter
{
    class DatabaseArrayFieldBindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int CurrentIndex = (int)values[0];
            int ArrayLength = (int)values[1];
            int MaxPreviewItems = (int)values[2];

            bool visibility = true;

            //visibility &= CurrentIndex > 0;
            visibility |= ArrayLength <= MaxPreviewItems;

            return visibility ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            List<object> values = new List<object>();
            values.Add(Binding.DoNothing);
            return values.ToArray();
        }
    }
}
