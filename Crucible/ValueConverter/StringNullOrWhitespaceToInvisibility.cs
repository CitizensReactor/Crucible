using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Crucible.ValueConverter
{
    internal class StringNullOrWhitespaceToInvisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;
            if (value as string == null) return Visibility.Collapsed;

            return String.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
