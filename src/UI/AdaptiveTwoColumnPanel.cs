using System;
using System.Windows;
using System.Windows.Controls;

namespace ActivityDashboard.UI
{
    public class AdaptiveTwoColumnPanel : Panel
    {
        public static readonly DependencyProperty BreakpointProperty = DependencyProperty.Register(
            "Breakpoint", typeof(double), typeof(AdaptiveTwoColumnPanel), new FrameworkPropertyMetadata(1500.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
            "Gap", typeof(double), typeof(AdaptiveTwoColumnPanel), new FrameworkPropertyMetadata(18.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double Breakpoint
        {
            get { return (double)GetValue(BreakpointProperty); }
            set { SetValue(BreakpointProperty, value); }
        }

        public double Gap
        {
            get { return (double)GetValue(GapProperty); }
            set { SetValue(GapProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var availableWidth = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
            var useColumns = availableWidth >= Breakpoint && InternalChildren.Count > 1;
            var childWidth = useColumns ? Math.Max(0, (availableWidth - Gap) / 2) : availableWidth;
            var height = 0.0;
            var widestChild = 0.0;

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(childWidth, double.PositiveInfinity));
                widestChild = Math.Max(widestChild, child.DesiredSize.Width);
                if (useColumns)
                {
                    height = Math.Max(height, child.DesiredSize.Height);
                }
                else
                {
                    height += child.DesiredSize.Height;
                }
            }

            if (!useColumns && InternalChildren.Count > 1)
            {
                height += Gap * (InternalChildren.Count - 1);
            }

            return new Size(useColumns ? availableWidth : Math.Min(availableWidth, widestChild), height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var useColumns = finalSize.Width >= Breakpoint && InternalChildren.Count > 1;
            if (useColumns)
            {
                var childWidth = Math.Max(0, (finalSize.Width - Gap) / 2);
                foreach (UIElement child in InternalChildren)
                {
                    var index = InternalChildren.IndexOf(child);
                    var left = index == 0 ? 0 : childWidth + Gap;
                    child.Arrange(new Rect(left, 0, childWidth, finalSize.Height));
                }
            }
            else
            {
                var top = 0.0;
                foreach (UIElement child in InternalChildren)
                {
                    child.Arrange(new Rect(0, top, finalSize.Width, child.DesiredSize.Height));
                    top += child.DesiredSize.Height + Gap;
                }
            }

            return finalSize;
        }
    }
}

