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
            FavoriteGames = new List<RankedItem>();
            Platforms = new List<RankedItem>();
            Genres = new List<RankedItem>();
            RecentSessions = new List<ActivitySession>();
            HourlyActivity = new List<HourlyActivity>();
            MonthlyBuckets = new List<MonthlyBucket>();
            WeekdayBreakdown = new List<WeekdayBucket>();
            SessionLengthDistribution = new List<SessionLengthBucket>();
        }

        public ulong TotalPlaytimeSeconds { get; set; }
        public int GamesPlayed { get; set; }
        public ulong TotalLaunches { get; set; }
        public int GamesActiveLast30Days { get; set; }
        public int TotalSessions { get; set; }
        public ulong AverageSessionSeconds { get; set; }
        public ulong TrackedDurationSeconds { get; set; }
        public List<HeatmapDay> HeatmapDays { get; set; }
        public List<RankedItem> TopGames { get; set; }
        public List<RankedItem> FavoriteGames { get; set; }
        public List<RankedItem> Platforms { get; set; }
        public List<RankedItem> Genres { get; set; }
        public List<ActivitySession> RecentSessions { get; set; }
        public List<HourlyActivity> HourlyActivity { get; set; }
        public List<MonthlyBucket> MonthlyBuckets { get; set; }
        public List<WeekdayBucket> WeekdayBreakdown { get; set; }
        public List<SessionLengthBucket> SessionLengthDistribution { get; set; }
        public StreakInfo Streak { get; set; }
        public LongestSessionInfo LongestSession { get; set; }
        public LastSessionInfo LastSession { get; set; }
        public DateTime? FirstSessionDate { get; set; }
        public int ActiveDaysLast30 { get; set; }
        public ulong WeekdaySeconds { get; set; }
        public ulong WeekendSeconds { get; set; }
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
        public int Rank { get; set; }
        public string Name { get; set; }
        public string CoverPath { get; set; }
        public ulong DurationSeconds { get; set; }
        public double RelativePercentage { get; set; }
    }

    public class HourlyActivity
    {
        public int Hour { get; set; }
        public ulong DurationSeconds { get; set; }
        public int SessionCount { get; set; }
    }

    public class MonthlyBucket
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public ulong DurationSeconds { get; set; }
        public int SessionCount { get; set; }
        public string Label { get; set; }
    }

    public class WeekdayBucket
    {
        public int Order { get; set; }
        public DayOfWeek Day { get; set; }
        public string Label { get; set; }
        public string ShortLabel { get; set; }
        public ulong DurationSeconds { get; set; }
        public int SessionCount { get; set; }
        public double RelativePercentage { get; set; }
    }

    public class SessionLengthBucket
    {
        public string Label { get; set; }
        public int Order { get; set; }
        public ulong MinSeconds { get; set; }
        public ulong MaxSeconds { get; set; }
        public int Count { get; set; }
        public double RelativePercentage { get; set; }
    }

    public class StreakInfo
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime? LastActiveDate { get; set; }
    }

    public class LongestSessionInfo
    {
        public string GameName { get; set; }
        public string CoverPath { get; set; }
        public ulong DurationSeconds { get; set; }
        public DateTimeOffset StartedAtLocal { get; set; }
        public DateTimeOffset EndedAtLocal { get; set; }
    }

    public class LastSessionInfo
    {
        public string GameName { get; set; }
        public string CoverPath { get; set; }
        public ulong DurationSeconds { get; set; }
        public DateTimeOffset StartedAtLocal { get; set; }
        public DateTimeOffset EndedAtLocal { get; set; }
    }
}
