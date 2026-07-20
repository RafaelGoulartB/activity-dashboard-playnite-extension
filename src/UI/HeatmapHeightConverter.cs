using System;
using System.Globalization;
using System.Windows.Data;

namespace ActivityDashboard.UI
{
    public class HeatmapHeightConverter : IValueConverter
    {
        private const double WeekCount = 52.0;
        private const double DayCount = 7.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var width = value is double ? (double)value : 0;
            return width <= 0 ? 0 : width * DayCount / WeekCount;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

