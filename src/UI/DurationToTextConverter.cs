using System;
using System.Globalization;
using System.Windows.Data;
using ActivityDashboard.Services;

namespace ActivityDashboard.UI
{
    public class DurationToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ulong ? DurationFormatter.Format((ulong)value) : "0min";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

