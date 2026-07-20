using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using ActivityDashboard.Models;
using ActivityDashboard.Services;
using Playnite.SDK;

namespace ActivityDashboard.UI
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IPlayniteAPI api;
        private readonly IActivityStore store;
        private readonly DashboardAnalytics analytics = new DashboardAnalytics();
        private bool isLoading;
        private string errorMessage;
        private string totalPlaytime;
        private string gamesPlayed;
        private string totalLaunches;
        private string recentGames;

        public DashboardViewModel(IPlayniteAPI api, IActivityStore store)
        {
            this.api = api;
            this.store = store;
            HeatmapCells = new ObservableCollection<HeatmapCell>();
            TopGames = new ObservableCollection<RankedItem>();
            Platforms = new ObservableCollection<RankedItem>();
            Genres = new ObservableCollection<RankedItem>();
            RecentSessions = new ObservableCollection<ActivitySession>();
            TotalPlaytime = "—";
            GamesPlayed = "—";
            TotalLaunches = "—";
            RecentGames = "—";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<HeatmapCell> HeatmapCells { get; private set; }
        public ObservableCollection<RankedItem> TopGames { get; private set; }
        public ObservableCollection<RankedItem> Platforms { get; private set; }
        public ObservableCollection<RankedItem> Genres { get; private set; }
        public ObservableCollection<ActivitySession> RecentSessions { get; private set; }

        public bool IsLoading { get { return isLoading; } private set { SetField(ref isLoading, value); } }
        public string ErrorMessage { get { return errorMessage; } private set { SetField(ref errorMessage, value); } }
        public string TotalPlaytime { get { return totalPlaytime; } private set { SetField(ref totalPlaytime, value); } }
        public string GamesPlayed { get { return gamesPlayed; } private set { SetField(ref gamesPlayed, value); } }
        public string TotalLaunches { get { return totalLaunches; } private set { SetField(ref totalLaunches, value); } }
        public string RecentGames { get { return recentGames; } private set { SetField(ref recentGames, value); } }
        public bool HasSessions { get { return RecentSessions.Count > 0; } }

        public async Task RefreshAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var gameSnapshots = CreateGameSnapshots();
                var sessions = store.Load().Sessions.ToList();
                var metrics = await Task.Run(() => analytics.Build(gameSnapshots, sessions, DateTime.Today));
                ApplyMetrics(metrics);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Não foi possível carregar o dashboard: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<GameSnapshot> CreateGameSnapshots()
        {
            return api.Database.Games.Select(game => new GameSnapshot
            {
                Id = game.Id,
                Name = game.Name ?? string.Empty,
                CoverPath = string.IsNullOrEmpty(game.CoverImage) ? null : api.Database.GetFullFilePath(game.CoverImage),
                PlaytimeSeconds = game.Playtime,
                PlayCount = game.PlayCount,
                LastActivity = game.LastActivity,
                Platforms = game.Platforms == null ? new List<string>() : game.Platforms.Where(platform => platform != null).Select(platform => platform.Name).ToList(),
                Genres = game.Genres == null ? new List<string>() : game.Genres.Where(genre => genre != null).Select(genre => genre.Name).ToList()
            }).ToList();
        }

        private void ApplyMetrics(DashboardMetrics metrics)
        {
            TotalPlaytime = DurationFormatter.Format(metrics.TotalPlaytimeSeconds);
            GamesPlayed = metrics.GamesPlayed.ToString();
            TotalLaunches = metrics.TotalLaunches.ToString();
            RecentGames = metrics.GamesActiveLast30Days.ToString();
            Replace(TopGames, metrics.TopGames);
            Replace(Platforms, metrics.Platforms);
            Replace(Genres, metrics.Genres);
            Replace(RecentSessions, metrics.RecentSessions);
            Replace(HeatmapCells, BuildCells(metrics.HeatmapDays));
            OnPropertyChanged("HasSessions");
        }

        private static IEnumerable<HeatmapCell> BuildCells(IList<HeatmapDay> days)
        {
            var lookup = days.ToDictionary(day => day.Date.Date);
            var firstMonday = days.Min(day => day.Date.Date);
            var cells = new List<HeatmapCell>();
            for (var dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                for (var week = 0; week < 52; week++)
                {
                    var date = firstMonday.AddDays(week * 7 + dayOfWeek);
                    var day = lookup[date];
                    cells.Add(new HeatmapCell
                    {
                        Tooltip = string.Format("{0:dd/MM/yyyy}: {1} em {2} sessão(ões)", day.Date, DurationFormatter.Format(day.DurationSeconds), day.SessionCount),
                        Background = HeatmapBrush(day.IntensityLevel)
                    });
                }
            }

            return cells;
        }

        private static Brush HeatmapBrush(int intensity)
        {
            switch (intensity)
            {
                case 1: return new SolidColorBrush(Color.FromRgb(22, 92, 68));
                case 2: return new SolidColorBrush(Color.FromRgb(22, 130, 88));
                case 3: return new SolidColorBrush(Color.FromRgb(35, 171, 110));
                case 4: return new SolidColorBrush(Color.FromRgb(75, 216, 137));
                default: return new SolidColorBrush(Color.FromRgb(54, 54, 54));
            }
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
        {
            target.Clear();
            foreach (var value in values)
            {
                target.Add(value);
            }
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class HeatmapCell
    {
        public string Tooltip { get; set; }
        public Brush Background { get; set; }
    }
}

