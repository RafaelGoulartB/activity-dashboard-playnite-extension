using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ActivityDashboard.Services;
using Playnite.SDK;

namespace ActivityDashboard.UI
{
    public partial class DashboardView : UserControl
    {
        private const double AnchorOffsetCompensation = 64.0;

        private readonly DashboardViewModel viewModel;

        public DashboardView(IPlayniteAPI api, IActivityStore store)
        {
            InitializeComponent();
            viewModel = new DashboardViewModel(api, store);
            DataContext = viewModel;
            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DashboardView_Loaded;
            await viewModel.RefreshAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await viewModel.RefreshAsync();
        }

        private void ScrollToOverview(object sender, RoutedEventArgs e)
        {
            ScrollTo(OverviewAnchor);
        }

        private void ScrollToActivity(object sender, RoutedEventArgs e)
        {
            ScrollTo(ActivityAnchor);
        }

        private void ScrollToLibrary(object sender, RoutedEventArgs e)
        {
            ScrollTo(LibraryAnchor);
        }

        private void ScrollToSessions(object sender, RoutedEventArgs e)
        {
            ScrollTo(SessionsAnchor);
        }

        private void ScrollTo(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            if (BringAnchorIntoView(element))
            {
                return;
            }

            element.BringIntoView();
        }

        private bool BringAnchorIntoView(FrameworkElement element)
        {
            if (RootScroller == null)
            {
                return false;
            }

            try
            {
                RootScroller.UpdateLayout();
                var transform = element.TransformToAncestor(RootScroller);
                var point = transform.Transform(new Point(0, 0));
                var target = Math.Max(0, point.Y - AnchorOffsetCompensation);
                RootScroller.ScrollToVerticalOffset(target);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
