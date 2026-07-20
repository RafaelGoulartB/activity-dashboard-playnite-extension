using System;
using System.Collections.Generic;
using System.Linq;
using ActivityDashboard.Models;

namespace ActivityDashboard.Services
{
    public class DashboardAnalytics
    {
        public DashboardMetrics Build(IEnumerable<GameSnapshot> games, IEnumerable<ActivitySession> sessions, DateTime todayLocal)
        {
            var gameList = (games ?? Enumerable.Empty<GameSnapshot>()).ToList();
            var sessionList = (sessions ?? Enumerable.Empty<ActivitySession>()).ToList();
            var metrics = new DashboardMetrics();

            metrics.TotalPlaytimeSeconds = gameList.Aggregate(0UL, (total, game) => total + game.PlaytimeSeconds);
            metrics.GamesPlayed = gameList.Count(game => game.PlaytimeSeconds > 0 || game.PlayCount > 0);
            metrics.TotalLaunches = gameList.Aggregate(0UL, (total, game) => total + game.PlayCount);
            var last30Days = todayLocal.Date.AddDays(-30);
            metrics.GamesActiveLast30Days = gameList.Count(game => game.LastActivity.HasValue && game.LastActivity.Value.Date >= last30Days);

            metrics.HeatmapDays = BuildHeatmap(sessionList, todayLocal);
            metrics.TopGames = gameList.OrderByDescending(game => game.PlaytimeSeconds)
                .ThenBy(game => game.Name)
                .Take(20)
                .Select(game => new RankedItem { Name = game.Name, CoverPath = game.CoverPath, DurationSeconds = game.PlaytimeSeconds })
                .ToList();
            metrics.Platforms = BuildBreakdown(gameList, game => game.Platforms, "Uncategorized platform");
            metrics.Genres = BuildBreakdown(gameList, game => game.Genres, "Uncategorized genre");
            metrics.RecentSessions = sessionList.OrderByDescending(session => session.EndedAtLocal).Take(10).ToList();
            metrics.HourlyActivity = BuildHourlyActivity(sessionList);
            return metrics;
        }

        public List<HourlyActivity> BuildHourlyActivity(IEnumerable<ActivitySession> sessions)
        {
            var hours = Enumerable.Range(0, 24).Select(hour => new HourlyActivity { Hour = hour }).ToList();
            foreach (var session in sessions ?? Enumerable.Empty<ActivitySession>())
            {
                if (session == null || session.DurationSeconds == 0)
                {
                    continue;
                }

                var cursor = session.StartedAtLocal.DateTime;
                var end = session.EndedAtLocal.DateTime;
                if (end <= cursor)
                {
                    end = cursor.AddSeconds(session.DurationSeconds);
                }

                while (cursor < end)
                {
                    var nextHour = new DateTime(cursor.Year, cursor.Month, cursor.Day, cursor.Hour, 0, 0).AddHours(1);
                    var segmentEnd = end < nextHour ? end : nextHour;
                    var seconds = (ulong)Math.Ceiling((segmentEnd - cursor).TotalSeconds);
                    var hour = hours[cursor.Hour];
                    hour.DurationSeconds += seconds;
                    hour.SessionCount += 1;
                    cursor = segmentEnd;
                }
            }

            return hours;
        }

        public List<HeatmapDay> BuildHeatmap(IEnumerable<ActivitySession> sessions, DateTime todayLocal)
        {
            var currentMonday = todayLocal.Date.AddDays(-((int)(todayLocal.DayOfWeek + 6) % 7));
            var start = currentMonday.AddDays(-51 * 7);
            var end = start.AddDays(363);
            var totals = new Dictionary<DateTime, HeatmapDay>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                totals[date] = new HeatmapDay { Date = date };
            }

            foreach (var session in sessions ?? Enumerable.Empty<ActivitySession>())
            {
                AddSessionToDays(session, start, end, totals);
            }

            var maximum = totals.Values.Max(day => day.DurationSeconds);
            foreach (var day in totals.Values)
            {
                day.IntensityLevel = GetIntensityLevel(day.DurationSeconds, maximum);
            }

            return totals.Values.OrderBy(day => day.Date).ToList();
        }

        private static List<RankedItem> BuildBreakdown(IEnumerable<GameSnapshot> games, Func<GameSnapshot, IEnumerable<string>> selector, string emptyLabel)
        {
            return games.SelectMany(game =>
                {
                    var values = selector(game) == null ? new List<string>() : selector(game).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList();
                    if (values.Count == 0)
                    {
                        values.Add(emptyLabel);
                    }

                    return values.Select(value => new RankedItem { Name = value, DurationSeconds = game.PlaytimeSeconds });
                })
                .GroupBy(item => item.Name)
                .Select(group => new RankedItem { Name = group.Key, DurationSeconds = group.Aggregate(0UL, (total, item) => total + item.DurationSeconds) })
                .OrderByDescending(item => item.DurationSeconds)
                .ThenBy(item => item.Name)
                .Take(5)
                .ToList();
        }

        private static void AddSessionToDays(ActivitySession session, DateTime firstDate, DateTime lastDate, IDictionary<DateTime, HeatmapDay> totals)
        {
            if (session == null || session.DurationSeconds == 0)
            {
                return;
            }

            // Stored timestamps carry the user's local offset. DateTime preserves that wall-clock
            // calendar date instead of converting it to the machine's current time zone.
            var sessionStart = session.StartedAtLocal.DateTime;
            var sessionEnd = session.EndedAtLocal.DateTime;
            if (sessionEnd <= sessionStart)
            {
                sessionEnd = sessionStart.AddSeconds(session.DurationSeconds);
            }

            var cursor = sessionStart;
            while (cursor < sessionEnd)
            {
                var day = cursor.Date;
                var nextDay = day.AddDays(1);
                var segmentEnd = sessionEnd < nextDay ? sessionEnd : nextDay;
                if (day >= firstDate && day <= lastDate)
                {
                    var seconds = (ulong)Math.Ceiling((segmentEnd - cursor).TotalSeconds);
                    totals[day].DurationSeconds += seconds;
                    totals[day].SessionCount += 1;
                }

                cursor = segmentEnd;
            }
        }

        private static int GetIntensityLevel(ulong value, ulong maximum)
        {
            if (value == 0 || maximum == 0)
            {
                return 0;
            }

            return Math.Min(4, Math.Max(1, (int)Math.Ceiling((double)value / maximum * 4)));
        }
    }
}
