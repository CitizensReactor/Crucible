using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Crucible.ValueConverter
{
    internal class StringNullOrWhitespaceToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Visible;
            if (value as string == null) return Visibility.Visible;

            return String.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
