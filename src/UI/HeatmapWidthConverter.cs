using System;
using System.Globalization;
using System.Windows.Data;

namespace ActivityDashboard.UI
{
    public class HeatmapWidthConverter : IValueConverter
    {
        private const double MinimumWidth = 840.0;
        private const double MaximumWidth = 1440.0;
        private const double HorizontalPadding = 48.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var availableWidth = value is double ? (double)value : 0;
            if (availableWidth <= 0)
            {
                return MinimumWidth;
            }

            var contentWidth = availableWidth - HorizontalPadding;
            return Math.Max(MinimumWidth, Math.Min(MaximumWidth, contentWidth));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

