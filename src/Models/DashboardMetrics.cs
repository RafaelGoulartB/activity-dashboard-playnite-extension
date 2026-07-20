using System;
using System.Collections.Generic;

namespace ActivityDashboard.Models
{
    public class DashboardMetrics
    {
        public DashboardMetrics()
        {
            HeatmapDays = new List<HeatmapDay>();
            TopGames = new List<RankedItem>();
            Platforms = new List<RankedItem>();
            Genres = new List<RankedItem>();
            RecentSessions = new List<ActivitySession>();
            HourlyActivity = new List<HourlyActivity>();
        }

        public ulong TotalPlaytimeSeconds { get; set; }
        public int GamesPlayed { get; set; }
        public ulong TotalLaunches { get; set; }
        public int GamesActiveLast30Days { get; set; }
        public List<HeatmapDay> HeatmapDays { get; set; }
        public List<RankedItem> TopGames { get; set; }
        public List<RankedItem> Platforms { get; set; }
        public List<RankedItem> Genres { get; set; }
        public List<ActivitySession> RecentSessions { get; set; }
        public List<HourlyActivity> HourlyActivity { get; set; }
    }

    public class HeatmapDay
    {
        public DateTime Date { get; set; }
        public ulong DurationSeconds { get; set; }
        public int SessionCount { get; set; }
        public int IntensityLevel { get; set; }
    }

    public class RankedItem
    {
        public string Name { get; set; }
        public string CoverPath { get; set; }
        public ulong DurationSeconds { get; set; }
    }

    public class HourlyActivity
    {
        public int Hour { get; set; }
        public ulong DurationSeconds { get; set; }
        public int SessionCount { get; set; }
    }
}
