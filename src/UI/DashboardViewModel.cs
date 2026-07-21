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
        private string peakHour;
        private string trackedPlaytime;
        private string currentStreakText;
        private string longestStreakText;
        private string averageSessionText;
        private string totalSessionsText;
        private string activeDaysText;
        private string longestSessionGame;
        private string longestSessionDuration;
        private string longestSessionWhen;
        private string mostActiveDay;
        private string weekendRatioText;
        private string firstSessionText;
        private string monthlyTotalText;
        private string monthlyPeakLabel;
        private int monthlyPeakIndex = -1;
        private ObservableCollection<HourlyActivity> hourlyActivity;
        private ObservableCollection<MonthlyBucket> monthlyBuckets;
        private ObservableCollection<WeekdayBucket> weekdayBreakdown;
        private ObservableCollection<SessionLengthBucket> sessionLengthDistribution;
        private PlaytimePeriod selectedPlaytimePeriod;
        private bool isPeriodLoading;
        private bool hasLoadedData;
        private int periodRequestVersion;
        private List<GameSnapshot> loadedGames = new List<GameSnapshot>();
        private List<ActivitySession> loadedSessions = new List<ActivitySession>();

        public DashboardViewModel(IPlayniteAPI api, IActivityStore store)
        {
            this.api = api;
            this.store = store;
            HeatmapCells = new ObservableCollection<HeatmapCell>();
            TopGames = new ObservableCollection<RankedItem>();
            FavoriteGames = new ObservableCollection<RankedItem>();
            FilteredTopGames = new ObservableCollection<RankedItem>();
            Platforms = new ObservableCollection<RankedItem>();
            Genres = new ObservableCollection<RankedItem>();
            RecentSessions = new ObservableCollection<ActivitySession>();
            HourlyActivity = new ObservableCollection<HourlyActivity>();
            MonthlyBuckets = new ObservableCollection<MonthlyBucket>();
            WeekdayBreakdown = new ObservableCollection<WeekdayBucket>();
            SessionLengthDistribution = new ObservableCollection<SessionLengthBucket>();
            PeakHour = "No data yet";
            TrackedPlaytime = "0min";
            TotalPlaytime = "—";
            GamesPlayed = "—";
            TotalLaunches = "—";
            RecentGames = "—";
            CurrentStreakText = "0 days";
            LongestStreakText = "0 days";
            AverageSessionText = "—";
            TotalSessionsText = "0";
            ActiveDaysText = "0 / 30";
            LongestSessionGame = "—";
            LongestSessionDuration = "—";
            LongestSessionWhen = "No tracked sessions yet";
            MostActiveDay = "—";
            WeekendRatioText = "—";
            FirstSessionText = "Not tracked yet";
            MonthlyTotalText = "0min";
            MonthlyPeakLabel = "No data yet";
            PlaytimePeriods = new ObservableCollection<PlaytimePeriod>
            {
                new PlaytimePeriod("Last 7 days", 7),
                new PlaytimePeriod("Last 30 days", 30),
                new PlaytimePeriod("Last 90 days", 90),
                new PlaytimePeriod("Last 12 months", 365),
                new PlaytimePeriod("All tracked time", 0)
            };
            selectedPlaytimePeriod = PlaytimePeriods[1];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<HeatmapCell> HeatmapCells { get; private set; }
        public ObservableCollection<RankedItem> TopGames { get; private set; }
        public ObservableCollection<RankedItem> FavoriteGames { get; private set; }
        public ObservableCollection<RankedItem> FilteredTopGames { get; private set; }
        public ObservableCollection<PlaytimePeriod> PlaytimePeriods { get; private set; }
        public ObservableCollection<RankedItem> Platforms { get; private set; }
        public ObservableCollection<RankedItem> Genres { get; private set; }
        public ObservableCollection<ActivitySession> RecentSessions { get; private set; }
        public ObservableCollection<HourlyActivity> HourlyActivity { get { return hourlyActivity; } private set { SetField(ref hourlyActivity, value); } }
        public ObservableCollection<MonthlyBucket> MonthlyBuckets { get { return monthlyBuckets; } private set { SetField(ref monthlyBuckets, value); } }
        public ObservableCollection<WeekdayBucket> WeekdayBreakdown { get { return weekdayBreakdown; } private set { SetField(ref weekdayBreakdown, value); } }
        public ObservableCollection<SessionLengthBucket> SessionLengthDistribution { get { return sessionLengthDistribution; } private set { SetField(ref sessionLengthDistribution, value); } }

        public bool IsLoading { get { return isLoading; } private set { SetField(ref isLoading, value); } }
        public string ErrorMessage { get { return errorMessage; } private set { SetField(ref errorMessage, value); } }
        public string TotalPlaytime { get { return totalPlaytime; } private set { SetField(ref totalPlaytime, value); } }
        public string GamesPlayed { get { return gamesPlayed; } private set { SetField(ref gamesPlayed, value); } }
        public string TotalLaunches { get { return totalLaunches; } private set { SetField(ref totalLaunches, value); } }
        public string RecentGames { get { return recentGames; } private set { SetField(ref recentGames, value); } }
        public bool HasSessions { get { return RecentSessions.Count > 0; } }
        public bool HasFilteredGames { get { return FilteredTopGames.Count > 0; } }
        public bool HasFavoriteGames { get { return FavoriteGames.Count > 0; } }
        public bool HasPlatforms { get { return Platforms.Count > 0; } }
        public bool HasGenres { get { return Genres.Count > 0; } }
        public bool HasMonthlyData { get { return MonthlyBuckets != null && MonthlyBuckets.Any(bucket => bucket.DurationSeconds > 0); } }
        public bool HasWeekdayData { get { return WeekdayBreakdown != null && WeekdayBreakdown.Any(bucket => bucket.DurationSeconds > 0); } }
        public bool HasSessionLengthData { get { return SessionLengthDistribution != null && SessionLengthDistribution.Any(bucket => bucket.Count > 0); } }
        public bool IsPeriodLoading { get { return isPeriodLoading; } private set { SetField(ref isPeriodLoading, value); } }
        public PlaytimePeriod SelectedPlaytimePeriod
        {
            get { return selectedPlaytimePeriod; }
            set
            {
                if (SetField(ref selectedPlaytimePeriod, value) && hasLoadedData)
                {
                    UpdateFilteredGamesAsync();
                }
            }
        }
        public string PeakHour { get { return peakHour; } private set { SetField(ref peakHour, value); } }
        public string TrackedPlaytime { get { return trackedPlaytime; } private set { SetField(ref trackedPlaytime, value); } }
        public string CurrentStreakText { get { return currentStreakText; } private set { SetField(ref currentStreakText, value); } }
        public string LongestStreakText { get { return longestStreakText; } private set { SetField(ref longestStreakText, value); } }
        public string AverageSessionText { get { return averageSessionText; } private set { SetField(ref averageSessionText, value); } }
        public string TotalSessionsText { get { return totalSessionsText; } private set { SetField(ref totalSessionsText, value); } }
        public string ActiveDaysText { get { return activeDaysText; } private set { SetField(ref activeDaysText, value); } }
        public string LongestSessionGame { get { return longestSessionGame; } private set { SetField(ref longestSessionGame, value); } }
        public string LongestSessionDuration { get { return longestSessionDuration; } private set { SetField(ref longestSessionDuration, value); } }
        public string LongestSessionWhen { get { return longestSessionWhen; } private set { SetField(ref longestSessionWhen, value); } }
        public string MostActiveDay { get { return mostActiveDay; } private set { SetField(ref mostActiveDay, value); } }
        public string WeekendRatioText { get { return weekendRatioText; } private set { SetField(ref weekendRatioText, value); } }
        public string FirstSessionText { get { return firstSessionText; } private set { SetField(ref firstSessionText, value); } }
        public string MonthlyTotalText { get { return monthlyTotalText; } private set { SetField(ref monthlyTotalText, value); } }
        public string MonthlyPeakLabel { get { return monthlyPeakLabel; } private set { SetField(ref monthlyPeakLabel, value); } }
        public int MonthlyPeakIndex { get { return monthlyPeakIndex; } private set { SetField(ref monthlyPeakIndex, value); } }
        public string DashboardGreeting { get { return BuildGreeting(); } }

        public async Task RefreshAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var gameSnapshots = CreateGameSnapshots();
                var sessions = store.Load().Sessions.ToList();
                var period = SelectedPlaytimePeriod;
                var today = DateTime.Today;
                var result = await Task.Run(() => new DashboardLoadResult
                {
                    Metrics = analytics.Build(gameSnapshots, sessions, today),
                    FilteredGames = analytics.BuildTopGamesForPeriod(gameSnapshots, sessions, today, period.Days)
                });
                loadedGames = gameSnapshots;
                loadedSessions = sessions;
                hasLoadedData = true;
                ApplyMetrics(result.Metrics);
                if (ReferenceEquals(period, SelectedPlaytimePeriod))
                {
                    ApplyFilteredGames(result.FilteredGames);
                }
                else
                {
                    UpdateFilteredGamesAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "The dashboard could not be loaded: " + ex.Message;
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
                IsFavorite = game.Favorite,
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
            Replace(FavoriteGames, metrics.FavoriteGames);
            Replace(Platforms, metrics.Platforms);
            Replace(Genres, metrics.Genres);
            Replace(RecentSessions, metrics.RecentSessions);
            Replace(HeatmapCells, BuildCells(metrics.HeatmapDays));
            HourlyActivity = new ObservableCollection<HourlyActivity>(metrics.HourlyActivity);
            Replace(MonthlyBuckets, metrics.MonthlyBuckets);
            Replace(WeekdayBreakdown, metrics.WeekdayBreakdown);
            Replace(SessionLengthDistribution, metrics.SessionLengthDistribution);

            var busiestHour = metrics.HourlyActivity.OrderByDescending(hour => hour.DurationSeconds).ThenBy(hour => hour.Hour).FirstOrDefault();
            PeakHour = busiestHour == null || busiestHour.DurationSeconds == 0 ? "No data yet" : string.Format("{0:D2}:00 — {1}", busiestHour.Hour, DurationFormatter.Format(busiestHour.DurationSeconds));
            TrackedPlaytime = DurationFormatter.Format(metrics.HourlyActivity.Aggregate(0UL, (total, hour) => total + hour.DurationSeconds));

            TotalSessionsText = metrics.TotalSessions.ToString();
            AverageSessionText = metrics.TotalSessions == 0 ? "—" : DurationFormatter.Format(metrics.AverageSessionSeconds);
            CurrentStreakText = FormatStreak(metrics.Streak == null ? 0 : metrics.Streak.CurrentStreak);
            LongestStreakText = FormatStreak(metrics.Streak == null ? 0 : metrics.Streak.LongestStreak);
            ActiveDaysText = metrics.ActiveDaysLast30 + " / 30";
            FirstSessionText = metrics.FirstSessionDate.HasValue ? metrics.FirstSessionDate.Value.ToString("MMM dd, yyyy") : "Not tracked yet";

            if (metrics.LongestSession == null)
            {
                LongestSessionGame = "—";
                LongestSessionDuration = "—";
                LongestSessionWhen = "No tracked sessions yet";
            }
            else
            {
                LongestSessionGame = metrics.LongestSession.GameName;
                LongestSessionDuration = DurationFormatter.Format(metrics.LongestSession.DurationSeconds);
                LongestSessionWhen = "Played on " + metrics.LongestSession.StartedAtLocal.ToString("MMM dd, yyyy");
            }

            var mostActive = metrics.WeekdayBreakdown.OrderByDescending(bucket => bucket.DurationSeconds).FirstOrDefault();
            MostActiveDay = mostActive == null || mostActive.DurationSeconds == 0 ? "—" : mostActive.Label;

            var totalSeconds = metrics.WeekdaySeconds + metrics.WeekendSeconds;
            if (totalSeconds == 0)
            {
                WeekendRatioText = "—";
            }
            else
            {
                var weekendPct = (double)metrics.WeekendSeconds / totalSeconds * 100;
                var weekdayPct = 100.0 - weekendPct;
                WeekendRatioText = string.Format("{0:0}% weekend · {1:0}% weekday", weekendPct, weekdayPct);
            }

            var monthlyTotal = metrics.MonthlyBuckets.Aggregate(0UL, (total, bucket) => total + bucket.DurationSeconds);
            MonthlyTotalText = DurationFormatter.Format(monthlyTotal);
            var monthlyPeak = metrics.MonthlyBuckets
                .Select((bucket, index) => new { bucket, index })
                .OrderByDescending(item => item.bucket.DurationSeconds)
                .ThenBy(item => item.index)
                .FirstOrDefault();
            if (monthlyPeak == null || monthlyPeak.bucket.DurationSeconds == 0)
            {
                MonthlyPeakLabel = "No data yet";
                MonthlyPeakIndex = -1;
            }
            else
            {
                MonthlyPeakLabel = monthlyPeak.bucket.Label + " · " + DurationFormatter.Format(monthlyPeak.bucket.DurationSeconds);
                MonthlyPeakIndex = monthlyPeak.index;
            }

            OnPropertyChanged("HasSessions");
            OnPropertyChanged("HasFavoriteGames");
            OnPropertyChanged("HasPlatforms");
            OnPropertyChanged("HasGenres");
            OnPropertyChanged("HasMonthlyData");
            OnPropertyChanged("HasWeekdayData");
            OnPropertyChanged("HasSessionLengthData");
            OnPropertyChanged("DashboardGreeting");
        }

        private async void UpdateFilteredGamesAsync()
        {
            var requestVersion = ++periodRequestVersion;
            var period = SelectedPlaytimePeriod;
            var games = loadedGames.ToList();
            var sessions = loadedSessions.ToList();
            IsPeriodLoading = true;
            try
            {
                var filteredGames = await Task.Run(() => analytics.BuildTopGamesForPeriod(games, sessions, DateTime.Today, period.Days));
                if (requestVersion == periodRequestVersion)
                {
                    ApplyFilteredGames(filteredGames);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "The selected playtime period could not be loaded: " + ex.Message;
            }
            finally
            {
                if (requestVersion == periodRequestVersion)
                {
                    IsPeriodLoading = false;
                }
            }
        }

        private void ApplyFilteredGames(IEnumerable<RankedItem> games)
        {
            Replace(FilteredTopGames, games);
            OnPropertyChanged("HasFilteredGames");
        }

        private static string FormatStreak(int days)
        {
            if (days <= 0)
            {
                return "0 days";
            }

            return days + (days == 1 ? " day" : " days");
        }

        private static string BuildGreeting()
        {
            var hour = DateTime.Now.Hour;
            if (hour < 5) return "Late-night sessions ahead";
            if (hour < 12) return "Good morning, player";
            if (hour < 18) return "Good afternoon, player";
            return "Good evening, player";
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
                        Tooltip = string.Format("{0:MMM dd, yyyy}: {1} across {2} session(s)", day.Date, DurationFormatter.Format(day.DurationSeconds), day.SessionCount),
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

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
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

    public class PlaytimePeriod
    {
        public PlaytimePeriod(string name, int days)
        {
            Name = name;
            Days = days;
        }

        public string Name { get; private set; }
        public int Days { get; private set; }
    }

    internal class DashboardLoadResult
    {
        public DashboardMetrics Metrics { get; set; }
        public List<RankedItem> FilteredGames { get; set; }
    }
}
