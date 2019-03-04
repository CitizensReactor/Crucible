using System;
using System.Globalization;
using System.Windows.Data;

namespace Crucible.ValueConverter
{
    class UtcToLocalDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime datetime;

            if (value is DateTime)
            {
                datetime = ((DateTime)value).ToLocalTime();
            }
            else
            {
                datetime = DateTime.Parse(value?.ToString()).ToLocalTime();
            }

            var resultTime = TimeZone.CurrentTimeZone.ToLocalTime(datetime);
            var resultString = resultTime.ToString();
            return resultString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
