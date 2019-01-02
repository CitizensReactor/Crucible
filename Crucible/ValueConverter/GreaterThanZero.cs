using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Crucible.ValueConverter
{
    internal class GreaterThanZero : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool booleanValue;
            if (value == null)
            {
                booleanValue = false;
            }
            else
            {
                booleanValue = (value as dynamic) > 0;
            }

            if (targetType == typeof(bool))
            {
                return booleanValue;
            }
            if(targetType == typeof(Visibility))
            {
                return booleanValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return booleanValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
