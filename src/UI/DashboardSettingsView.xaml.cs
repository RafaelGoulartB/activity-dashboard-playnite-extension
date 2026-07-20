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
            var result = MessageBox.Show("Remover todas as sessões registradas pelo Activity Dashboard? Os dados da biblioteca Playnite não serão alterados.", "Limpar histórico", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            store.ClearHistory();
            StatusText.Text = "Histórico rastreado removido.";
        }
    }
}

