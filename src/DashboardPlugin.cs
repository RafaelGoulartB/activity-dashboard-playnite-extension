using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ActivityDashboard.Services;
using ActivityDashboard.Settings;
using ActivityDashboard.UI;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;

namespace ActivityDashboard
{
    public class DashboardPlugin : GenericPlugin
    {
        public override Guid Id { get; } = Guid.Parse("7baf6cdb-15cb-4c77-9af3-825e9df6ec8f");

        private readonly IActivityStore activityStore;
        private readonly SessionTracker sessionTracker;
        private readonly DashboardSettings settings = new DashboardSettings();

        public DashboardPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties { HasSettings = true };
            activityStore = new JsonActivityStore(api.Paths.ExtensionsDataPath);
            sessionTracker = new SessionTracker(activityStore);
            activityStore.Load();
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            sessionTracker.HandleGameStarted(args.Game.Id, args.Game.Name, DateTimeOffset.Now);
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (args == null || args.Game == null)
            {
                return;
            }

            sessionTracker.HandleGameStopped(args.Game.Id, args.ElapsedSeconds, DateTimeOffset.Now);
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Title = "Activity Dashboard",
                Type = SiderbarItemType.View,
                Icon = new Path
                {
                    Data = Geometry.Parse("M4,19H8V10H4V19M10,19H14V5H10V19M16,19H20V13H16V19M2,21H22V23H2V21Z"),
                    Fill = Brushes.White,
                    Stretch = Stretch.Uniform,
                    Width = 20,
                    Height = 20
                },
                Opened = () => new DashboardView(PlayniteApi, activityStore)
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new DashboardSettingsView(activityStore);
        }
    }
}

