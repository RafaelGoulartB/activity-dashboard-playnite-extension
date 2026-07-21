using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ActivityDashboard.UI
{
    public class DistributionFillConverter : IValueConverter
    {
        private static readonly Brush[] Palette = new Brush[]
        {
            CreateBrush(Color.FromRgb(109, 131, 245)),
            CreateBrush(Color.FromRgb(122, 156, 246)),
            CreateBrush(Color.FromRgb(95, 178, 199)),
            CreateBrush(Color.FromRgb(73, 216, 178)),
            CreateBrush(Color.FromRgb(233, 163, 77)),
            CreateBrush(Color.FromRgb(224, 107, 198))
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int))
            {
                return Palette[0];
            }

            var index = (int)value;
            if (index < 0)
            {
                index = 0;
            }

            if (index >= Palette.Length)
            {
                index = Palette.Length - 1;
            }

            return Palette[index];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static Brush CreateBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
