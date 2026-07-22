using System;
using System.Globalization;
using System.Windows.Data;

namespace ActivityDashboard.UI
{
    public class ProgressWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
            {
                return 0.0;
            }

            double value = 0.0;
            double maximum = 100.0;
            double containerWidth = 0.0;

            double parsedValue;
            if (values[0] != null && double.TryParse(values[0].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
            {
                value = parsedValue;
            }

            double parsedMaximum;
            if (values[1] != null && double.TryParse(values[1].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsedMaximum))
            {
                maximum = parsedMaximum;
            }

            double parsedWidth;
            if (values[2] != null && double.TryParse(values[2].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsedWidth))
            {
                containerWidth = parsedWidth;
            }

            if (maximum <= 0.0 || containerWidth <= 0.0)
            {
                return 0.0;
            }

            double ratio = value / maximum;
            if (ratio < 0.0)
            {
                ratio = 0.0;
            }
            else if (ratio > 1.0)
            {
                ratio = 1.0;
            }

            return ratio * containerWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
