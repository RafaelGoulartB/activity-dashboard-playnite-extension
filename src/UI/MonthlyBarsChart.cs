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
    public class MonthlyBarsChart : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(IEnumerable<MonthlyBucket>), typeof(MonthlyBarsChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HighlightIndexProperty = DependencyProperty.Register(
            "HighlightIndex", typeof(int), typeof(MonthlyBarsChart),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

        private List<MonthlyBucket> buckets = new List<MonthlyBucket>();
        private int hoveredIndex = -1;
        private int resolvedHighlightIndex = -1;

        public MonthlyBarsChart()
        {
            SnapsToDevicePixels = true;
            ToolTipService.SetInitialShowDelay(this, 100);
        }

        public IEnumerable<MonthlyBucket> ItemsSource
        {
            get { return (IEnumerable<MonthlyBucket>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public int HighlightIndex
        {
            get { return (int)GetValue(HighlightIndexProperty); }
            set { SetValue(HighlightIndexProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            buckets = (ItemsSource ?? Enumerable.Empty<MonthlyBucket>()).ToList();
            resolvedHighlightIndex = HighlightIndex < 0 || HighlightIndex >= buckets.Count ? -1 : HighlightIndex;
            if (ActualWidth <= 0 || ActualHeight <= 0 || buckets.Count == 0)
            {
                return;
            }

            const double labelArea = 30;
            const double valueArea = 18;
            const double horizontalPadding = 8;
            var chartTop = valueArea + 4;
            var chartBottom = ActualHeight - labelArea;
            var chartHeight = Math.Max(0, chartBottom - chartTop);
            var chartWidth = Math.Max(0, ActualWidth - horizontalPadding * 2);
            var barSpacing = 6;
            var barWidth = Math.Max(6, (chartWidth - barSpacing * (buckets.Count - 1)) / buckets.Count);
            var maximum = buckets.Max(bucket => bucket.DurationSeconds);

            DrawGuides(drawingContext, chartTop, chartBottom, horizontalPadding, chartWidth, maximum);

            for (var index = 0; index < buckets.Count; index++)
            {
                var bucket = buckets[index];
                var left = horizontalPadding + index * (barWidth + barSpacing);
                var ratio = maximum == 0 ? 0 : (double)bucket.DurationSeconds / maximum;
                var height = Math.Max(bucket.DurationSeconds > 0 ? 3 : 0, chartHeight * ratio);
                var top = chartBottom - height;
                var isHighlight = index == resolvedHighlightIndex || index == hoveredIndex;
                var brush = CreateBarBrush(isHighlight, bucket.DurationSeconds > 0);
                drawingContext.DrawRoundedRectangle(brush, null, new Rect(left, top, barWidth, height), 4, 4);

                var label = new FormattedText(bucket.Label, CultureInfo.GetCultureInfo("en-US"),
                    FlowDirection.LeftToRight, new Typeface("Segoe UI"),
                    10, new SolidColorBrush(Color.FromArgb(180, 175, 195, 219)), 1.0);
                drawingContext.DrawText(label, new Point(left + (barWidth - label.Width) / 2, chartBottom + 8));

                if (isHighlight && bucket.DurationSeconds > 0)
                {
                    var valueText = new FormattedText(DurationFormatter.Format(bucket.DurationSeconds),
                        CultureInfo.GetCultureInfo("en-US"), FlowDirection.LeftToRight, new Typeface("Segoe UI"),
                        10, new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1.0);
                    drawingContext.DrawText(valueText, new Point(left + (barWidth - valueText.Width) / 2, top - valueText.Height - 2));
                }
            }

            if (maximum == 0)
            {
                DrawCenteredText(drawingContext, "Launch a game through Playnite to start your monthly trend",
                    new Point(ActualWidth / 2, chartTop + chartHeight / 2), 11,
                    new SolidColorBrush(Color.FromRgb(170, 190, 215)), FontWeights.Normal);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (ActualWidth <= 0 || buckets.Count == 0)
            {
                return;
            }

            const double labelArea = 30;
            const double horizontalPadding = 8;
            var chartWidth = Math.Max(0, ActualWidth - horizontalPadding * 2);
            var chartBottom = ActualHeight - labelArea;
            var barSpacing = 6;
            var barWidth = Math.Max(6, (chartWidth - barSpacing * (buckets.Count - 1)) / buckets.Count);
            var point = e.GetPosition(this);
            if (point.Y > chartBottom)
            {
                UpdateHover(-1, null);
                return;
            }

            var relativeX = point.X - horizontalPadding;
            if (relativeX < 0)
            {
                UpdateHover(-1, null);
                return;
            }

            var index = (int)(relativeX / (barWidth + barSpacing));
            if (index < 0 || index >= buckets.Count)
            {
                UpdateHover(-1, null);
                return;
            }

            var bucket = buckets[index];
            UpdateHover(index, string.Format("{0}\n{1} · {2} session(s)", bucket.Label, DurationFormatter.Format(bucket.DurationSeconds), bucket.SessionCount));
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            UpdateHover(-1, null);
        }

        private void UpdateHover(int index, string tooltip)
        {
            if (hoveredIndex == index)
            {
                return;
            }

            hoveredIndex = index;
            ToolTip = tooltip;
            InvalidateVisual();
        }

        private static void DrawGuides(DrawingContext drawingContext, double top, double bottom, double left, double width, ulong maximum)
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(45, 145, 165, 195)), 1);
            pen.Freeze();
            for (var step = 0; step <= 4; step++)
            {
                var y = top + (bottom - top) * step / 4.0;
                drawingContext.DrawLine(pen, new Point(left, y), new Point(left + width, y));
            }

            if (maximum == 0)
            {
                return;
            }

            for (var step = 0; step <= 4; step++)
            {
                var ratio = 1.0 - step / 4.0;
                var valueLabel = DurationFormatter.Format((ulong)(maximum * ratio));
                var text = new FormattedText(valueLabel, CultureInfo.GetCultureInfo("en-US"),
                    FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9,
                    new SolidColorBrush(Color.FromArgb(150, 170, 190, 215)), 1.0);
                var y = top + (bottom - top) * step / 4.0;
                drawingContext.DrawText(text, new Point(left, y - text.Height - 1));
            }
        }

        private static Brush CreateBarBrush(bool isHighlight, bool hasValue)
        {
            if (!hasValue)
            {
                return new SolidColorBrush(Color.FromArgb(60, 109, 131, 201));
            }

            var start = isHighlight ? Color.FromRgb(122, 101, 213) : Color.FromRgb(76, 95, 178);
            var end = isHighlight ? Color.FromRgb(167, 119, 230) : Color.FromRgb(98, 191, 209);
            var brush = new LinearGradientBrush(start, end, 90);
            brush.Freeze();
            return brush;
        }

        private static void DrawCenteredText(DrawingContext drawingContext, string value, Point center, double fontSize, Brush brush, FontWeight weight)
        {
            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal);
            var text = new FormattedText(value, CultureInfo.GetCultureInfo("en-US"), FlowDirection.LeftToRight, typeface, fontSize, brush, 1.0);
            drawingContext.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
        }
    }
}
