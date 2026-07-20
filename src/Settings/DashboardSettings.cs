using System.Collections.Generic;
using System.ComponentModel;
using Playnite.SDK;

namespace ActivityDashboard.Settings
{
    public class DashboardSettings : ISettings
    {
        public void BeginEdit() { }
        public void CancelEdit() { }
        public void EndEdit() { }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
