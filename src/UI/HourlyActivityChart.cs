using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ActivityDashboard.Models;
using ActivityDashboard.Services;

namespace ActivityDashboard.UI
{
    public class HourlyActivityChart : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(IEnumerable<HourlyActivity>), typeof(HourlyActivityChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        private List<HourlyActivity> hours = new List<HourlyActivity>();

        public HourlyActivityChart()
        {
            SnapsToDevicePixels = true;
            ToolTipService.SetInitialShowDelay(this, 100);
        }

        public IEnumerable<HourlyActivity> ItemsSource
        {
            get { return (IEnumerable<HourlyActivity>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            hours = (ItemsSource ?? Enumerable.Empty<HourlyActivity>()).OrderBy(item => item.Hour).ToList();
            if (ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            var size = Math.Min(ActualWidth, ActualHeight);
            var center = new Point(ActualWidth / 2, ActualHeight / 2);
            var outerRadius = Math.Max(60, size / 2 - 38);
            var innerRadius = Math.Max(28, outerRadius * 0.12);
            var maxBarLength = outerRadius - innerRadius - 4;
            var maximum = hours.Any() ? hours.Max(item => item.DurationSeconds) : 0;

            var guidePen = new Pen(new SolidColorBrush(Color.FromArgb(125, 169, 187, 207)), 1.2);
            drawingContext.DrawEllipse(null, guidePen, center, outerRadius, outerRadius);
            drawingContext.DrawEllipse(new SolidColorBrush(Color.FromArgb(125, 27, 34, 54)), null, center, innerRadius, innerRadius);

            for (var hour = 0; hour < 24; hour++)
            {
                var data = hours.FirstOrDefault(item => item.Hour == hour) ?? new HourlyActivity { Hour = hour };
                var startAngle = hour * 15 - 90 - 6.5;
                var endAngle = hour * 15 - 90 + 6.5;
                var length = maximum == 0 ? 0 : Math.Max(4, maxBarLength * data.DurationSeconds / maximum);
                if (data.DurationSeconds > 0)
                {
                    drawingContext.DrawGeometry(CreateBarBrush(hour), null, CreateWedge(center, innerRadius, innerRadius + length, startAngle, endAngle));
                }

                var dividerAngle = hour * 15 - 90 - 7.5;
                var dividerStart = Polar(center, innerRadius + 3, dividerAngle);
                var dividerEnd = Polar(center, outerRadius - 2, dividerAngle);
                drawingContext.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(30, 196, 209, 225)), 0.6), dividerStart, dividerEnd);
                DrawHourLabel(drawingContext, center, outerRadius + 22, hour);
            }

            drawingContext.DrawEllipse(new SolidColorBrush(Color.FromRgb(27, 34, 54)), new Pen(new SolidColorBrush(Color.FromArgb(120, 125, 225, 204)), 1), center, innerRadius, innerRadius);
            DrawCenteredText(drawingContext, "24H", center, 11, new SolidColorBrush(Color.FromRgb(228, 239, 255)), FontWeights.Bold);

            if (maximum == 0)
            {
                DrawCenteredText(drawingContext, "No sessions", new Point(center.X, center.Y + 18), 10, new SolidColorBrush(Color.FromRgb(156, 174, 197)), FontWeights.Normal);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            var point = e.GetPosition(this);
            var center = new Point(ActualWidth / 2, ActualHeight / 2);
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            var degrees = Math.Atan2(dy, dx) * 180 / Math.PI + 90;
            if (degrees < 0) degrees += 360;
            var hour = ((int)Math.Floor((degrees + 7.5) / 15)) % 24;
            var data = hours.FirstOrDefault(item => item.Hour == hour) ?? new HourlyActivity { Hour = hour };
            ToolTip = string.Format("{0:D2}:00 – {1:D2}:59\n{2} across {3} session(s)", hour, hour, DurationFormatter.Format(data.DurationSeconds), data.SessionCount);
        }

        private static Geometry CreateWedge(Point center, double innerRadius, double outerRadius, double startAngle, double endAngle)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                var innerStart = Polar(center, innerRadius, startAngle);
                var outerStart = Polar(center, outerRadius, startAngle);
                var outerEnd = Polar(center, outerRadius, endAngle);
                var innerEnd = Polar(center, innerRadius, endAngle);
                context.BeginFigure(innerStart, true, true);
                context.LineTo(outerStart, true, false);
                context.ArcTo(outerEnd, new Size(outerRadius, outerRadius), 0, false, SweepDirection.Clockwise, true, false);
                context.LineTo(innerEnd, true, false);
                context.ArcTo(innerStart, new Size(innerRadius, innerRadius), 0, false, SweepDirection.Counterclockwise, true, false);
            }

            geometry.Freeze();
            return geometry;
        }

        private static Brush CreateBarBrush(int hour)
        {
            var start = hour < 7 ? Color.FromRgb(67, 156, 222) : hour < 16 ? Color.FromRgb(122, 101, 213) : Color.FromRgb(221, 99, 194);
            var end = hour < 7 ? Color.FromRgb(88, 202, 236) : hour < 16 ? Color.FromRgb(167, 119, 230) : Color.FromRgb(247, 132, 210);
            var brush = new LinearGradientBrush(start, end, 90);
            brush.Freeze();
            return brush;
        }

        private static Point Polar(Point center, double radius, double angle)
        {
            var radians = angle * Math.PI / 180;
            return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
        }

        private static void DrawHourLabel(DrawingContext context, Point center, double radius, int hour)
        {
            var position = Polar(center, radius, hour * 15 - 90);
            var text = new FormattedText(string.Format("{0:D2}h", hour), CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, new SolidColorBrush(Color.FromRgb(206, 217, 236)), 1.0);
            context.DrawText(text, new Point(position.X - text.Width / 2, position.Y - text.Height / 2));
        }

        private static void DrawCenteredText(DrawingContext context, string value, Point center, double fontSize, Brush brush, FontWeight weight)
        {
            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal);
            var text = new FormattedText(value, CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight, typeface, fontSize, brush, 1.0);
            context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
        }
    }
}
