using System;
using System.Windows;
using System.Windows.Controls;
using ActivityDashboard.Services;
using Playnite.SDK;

namespace ActivityDashboard.UI
{
    public partial class DashboardView : UserControl
    {
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

        private void SelectOverview(object sender, RoutedEventArgs e)
        {
            viewModel.SelectedSection = DashboardSection.Overview;
        }

        private void SelectActivity(object sender, RoutedEventArgs e)
        {
            viewModel.SelectedSection = DashboardSection.Activity;
        }

        private void SelectLibrary(object sender, RoutedEventArgs e)
        {
            viewModel.SelectedSection = DashboardSection.Library;
        }

        private void SelectSessions(object sender, RoutedEventArgs e)
        {
            viewModel.SelectedSection = DashboardSection.Sessions;
        }
    }
}
