using System.Windows;
using System.Windows.Controls;
using ActivityDashboard.Services;

namespace ActivityDashboard.UI
{
    public partial class DashboardSettingsView : UserControl
    {
        private readonly IActivityStore store;

        public DashboardSettingsView(IActivityStore store)
        {
            InitializeComponent();
            this.store = store;
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Remove every session tracked by Activity Dashboard? Your Playnite library data will not be changed.", "Clear tracked history", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            store.ClearHistory();
            StatusText.Text = "Tracked history cleared.";
        }
    }
}
