using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ActivityDashboard.Models;

namespace ActivityDashboard.Services
{
    public class DashboardAnalytics
    {
        private static readonly string[] MonthLabels =
        {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };

        private static readonly Tuple<ulong, ulong, string>[] SessionLengthBuckets =
        {
            Tuple.Create(0UL, 600UL, "< 10 min"),
            Tuple.Create(600UL, 1800UL, "10–29 min"),
            Tuple.Create(1800UL, 3600UL, "30–59 min"),
            Tuple.Create(3600UL, 7200UL, "1–2 h"),
            Tuple.Create(7200UL, 14400UL, "2–4 h"),
            Tuple.Create(14400UL, ulong.MaxValue, "4 h +")
        };

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
            metrics.TopGames = AddRanking(gameList.Where(game => game.PlaytimeSeconds > 0)
                .OrderByDescending(game => game.PlaytimeSeconds)
                .ThenBy(game => game.Name)
                .Take(20)
                .Select(game => new RankedItem { Name = game.Name, CoverPath = game.CoverPath, DurationSeconds = game.PlaytimeSeconds })
                .ToList());
            metrics.FavoriteGames = gameList.Where(game => game.IsFavorite)
                .OrderByDescending(game => game.PlaytimeSeconds)
                .ThenBy(game => game.Name)
                .Select(game => new RankedItem { Name = game.Name, CoverPath = game.CoverPath, DurationSeconds = game.PlaytimeSeconds })
                .ToList();
            metrics.Platforms = BuildBreakdown(gameList, game => game.Platforms, "Uncategorized platform");
            metrics.Genres = BuildBreakdown(gameList, game => game.Genres, "Uncategorized genre");
            metrics.RecentSessions = sessionList.OrderByDescending(session => session.EndedAtLocal).Take(10).ToList();
            metrics.HourlyActivity = BuildHourlyActivity(sessionList);

            var validSessions = sessionList.Where(session => session != null && session.DurationSeconds > 0).ToList();
            metrics.TotalSessions = validSessions.Count;
            metrics.TrackedDurationSeconds = validSessions.Aggregate(0UL, (total, session) => total + session.DurationSeconds);
            metrics.AverageSessionSeconds = validSessions.Count == 0 ? 0UL : metrics.TrackedDurationSeconds / (ulong)validSessions.Count;
            metrics.MonthlyBuckets = BuildMonthlyBuckets(sessionList, todayLocal);
            metrics.WeekdayBreakdown = BuildWeekdayBreakdown(sessionList);
            metrics.SessionLengthDistribution = BuildSessionLengthDistribution(validSessions);
            metrics.Streak = BuildStreak(metrics.HeatmapDays, todayLocal);
            metrics.LongestSession = BuildLongestSession(validSessions);
            metrics.FirstSessionDate = validSessions.Count == 0
                ? (DateTime?)null
                : validSessions.Min(session => session.StartedAtLocal.DateTime.Date);
            metrics.ActiveDaysLast30 = metrics.HeatmapDays.Count(day => day.Date >= last30Days && day.DurationSeconds > 0);
            metrics.WeekdaySeconds = metrics.WeekdayBreakdown
                .Where(bucket => bucket.Day != DayOfWeek.Saturday && bucket.Day != DayOfWeek.Sunday)
                .Aggregate(0UL, (total, bucket) => total + bucket.DurationSeconds);
            metrics.WeekendSeconds = metrics.WeekdayBreakdown
                .Where(bucket => bucket.Day == DayOfWeek.Saturday || bucket.Day == DayOfWeek.Sunday)
                .Aggregate(0UL, (total, bucket) => total + bucket.DurationSeconds);

            return metrics;
        }

        public List<RankedItem> BuildTopGamesForPeriod(IEnumerable<GameSnapshot> games, IEnumerable<ActivitySession> sessions, DateTime todayLocal, int periodDays)
        {
            var gameList = (games ?? Enumerable.Empty<GameSnapshot>()).ToList();
            var gameById = gameList.Where(game => game.Id != Guid.Empty).GroupBy(game => game.Id).ToDictionary(group => group.Key, group => group.First());
            var totals = new Dictionary<string, RankedItem>(StringComparer.OrdinalIgnoreCase);
            var periodStart = periodDays > 0 ? todayLocal.Date.AddDays(1 - periodDays) : DateTime.MinValue;
            var periodEnd = periodDays > 0 ? todayLocal.Date.AddDays(1) : DateTime.MaxValue;

            foreach (var session in sessions ?? Enumerable.Empty<ActivitySession>())
            {
                if (session == null || session.DurationSeconds == 0)
                {
                    continue;
                }

                var sessionStart = session.StartedAtLocal.DateTime;
                var maximumDuration = (DateTime.MaxValue - sessionStart).TotalSeconds;
                var safeDuration = Math.Min((double)session.DurationSeconds, maximumDuration);
                var sessionEnd = sessionStart.AddSeconds(safeDuration);
                var overlapStart = sessionStart > periodStart ? sessionStart : periodStart;
                var overlapEnd = sessionEnd < periodEnd ? sessionEnd : periodEnd;
                if (overlapEnd <= overlapStart)
                {
                    continue;
                }

                var identity = session.GameId != Guid.Empty ? "id:" + session.GameId.ToString("D") : "name:" + (session.GameName ?? string.Empty);
                RankedItem item;
                if (!totals.TryGetValue(identity, out item))
                {
                    GameSnapshot game;
                    gameById.TryGetValue(session.GameId, out game);
                    item = new RankedItem
                    {
                        Name = game == null ? (string.IsNullOrWhiteSpace(session.GameName) ? "Unknown game" : session.GameName) : game.Name,
                        CoverPath = game == null ? null : game.CoverPath
                    };
                    totals[identity] = item;
                }

                item.DurationSeconds += (ulong)Math.Ceiling((overlapEnd - overlapStart).TotalSeconds);
            }

            return AddRanking(totals.Values.OrderByDescending(item => item.DurationSeconds)
                .ThenBy(item => item.Name)
                .Take(20)
                .ToList());
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

        public StreakInfo BuildStreak(IList<HeatmapDay> heatmapDays, DateTime todayLocal)
        {
            var info = new StreakInfo();
            if (heatmapDays == null || heatmapDays.Count == 0)
            {
                return info;
            }

            var activeByDate = new HashSet<DateTime>(heatmapDays.Where(day => day.DurationSeconds > 0).Select(day => day.Date));
            info.LastActiveDate = activeByDate.Count == 0 ? (DateTime?)null : activeByDate.Max();

            var cursor = todayLocal.Date;
            while (activeByDate.Contains(cursor))
            {
                info.CurrentStreak++;
                cursor = cursor.AddDays(-1);
            }

            var ordered = activeByDate.OrderBy(date => date).ToList();
            var currentRun = 0;
            var previous = DateTime.MinValue;
            foreach (var date in ordered)
            {
                if (previous != DateTime.MinValue && date == previous.AddDays(1))
                {
                    currentRun++;
                }
                else
                {
                    currentRun = 1;
                }

                if (currentRun > info.LongestStreak)
                {
                    info.LongestStreak = currentRun;
                }

                previous = date;
            }

            return info;
        }

        public List<MonthlyBucket> BuildMonthlyBuckets(IEnumerable<ActivitySession> sessions, DateTime todayLocal)
        {
            var buckets = new List<MonthlyBucket>();
            for (var offset = 11; offset >= 0; offset--)
            {
                var month = todayLocal.Date.AddMonths(-offset);
                buckets.Add(new MonthlyBucket
                {
                    Year = month.Year,
                    Month = month.Month,
                    Label = MonthLabels[month.Month - 1] + " " + (month.Year % 100).ToString("D2", CultureInfo.InvariantCulture)
                });
            }

            var indexByKey = buckets.ToDictionary(bucket => bucket.Year * 12 + (bucket.Month - 1));
            foreach (var session in sessions ?? Enumerable.Empty<ActivitySession>())
            {
                if (session == null || session.DurationSeconds == 0)
                {
                    continue;
                }

                var sessionStart = session.StartedAtLocal.DateTime;
                var sessionEnd = session.EndedAtLocal.DateTime;
                if (sessionEnd <= sessionStart)
                {
                    sessionEnd = sessionStart.AddSeconds(session.DurationSeconds);
                }

                var cursor = sessionStart;
                while (cursor < sessionEnd)
                {
                    var monthEnd = new DateTime(cursor.Year, cursor.Month, 1).AddMonths(1);
                    var segmentEnd = sessionEnd < monthEnd ? sessionEnd : monthEnd;
                    var key = cursor.Year * 12 + (cursor.Month - 1);
                    MonthlyBucket bucket;
                    if (indexByKey.TryGetValue(key, out bucket))
                    {
                        bucket.DurationSeconds += (ulong)Math.Ceiling((segmentEnd - cursor).TotalSeconds);
                        bucket.SessionCount += 1;
                    }

                    cursor = segmentEnd;
                }
            }

            return buckets;
        }

        public List<WeekdayBucket> BuildWeekdayBreakdown(IEnumerable<ActivitySession> sessions)
        {
            var labels = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            var shortLabels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var buckets = new List<WeekdayBucket>();
            for (var day = 0; day < 7; day++)
            {
                buckets.Add(new WeekdayBucket
                {
                    Order = day,
                    Day = (DayOfWeek)day,
                    Label = labels[day],
                    ShortLabel = shortLabels[day]
                });
            }

            foreach (var session in sessions ?? Enumerable.Empty<ActivitySession>())
            {
                if (session == null || session.DurationSeconds == 0)
                {
                    continue;
                }

                var day = session.StartedAtLocal.DateTime.DayOfWeek;
                buckets[(int)day].DurationSeconds += session.DurationSeconds;
                buckets[(int)day].SessionCount += 1;
            }

            var max = buckets.Max(bucket => bucket.DurationSeconds);
            foreach (var bucket in buckets)
            {
                bucket.RelativePercentage = max == 0 ? 0 : (double)bucket.DurationSeconds / max * 100;
            }

            return buckets;
        }

        public List<SessionLengthBucket> BuildSessionLengthDistribution(IList<ActivitySession> sessions)
        {
            var buckets = new List<SessionLengthBucket>();
            for (var i = 0; i < SessionLengthBuckets.Length; i++)
            {
                var range = SessionLengthBuckets[i];
                buckets.Add(new SessionLengthBucket
                {
                    Label = range.Item3,
                    Order = i,
                    MinSeconds = range.Item1,
                    MaxSeconds = range.Item2
                });
            }

            foreach (var session in sessions ?? Enumerable.Empty<ActivitySession>())
            {
                if (session == null || session.DurationSeconds == 0)
                {
                    continue;
                }

                SessionLengthBucket matched = null;
                for (var i = 0; i < buckets.Count; i++)
                {
                    if (session.DurationSeconds >= buckets[i].MinSeconds && session.DurationSeconds < buckets[i].MaxSeconds)
                    {
                        matched = buckets[i];
                        break;
                    }
                }

                if (matched == null)
                {
                    matched = buckets[buckets.Count - 1];
                }

                matched.Count += 1;
            }

            var max = buckets.Max(bucket => bucket.Count);
            foreach (var bucket in buckets)
            {
                bucket.RelativePercentage = max == 0 ? 0 : (double)bucket.Count / max * 100;
            }

            return buckets;
        }

        private static LongestSessionInfo BuildLongestSession(IList<ActivitySession> validSessions)
        {
            if (validSessions == null || validSessions.Count == 0)
            {
                return null;
            }

            var longest = validSessions.OrderByDescending(session => session.DurationSeconds).First();
            return new LongestSessionInfo
            {
                GameName = string.IsNullOrWhiteSpace(longest.GameName) ? "Unknown game" : longest.GameName,
                DurationSeconds = longest.DurationSeconds,
                StartedAtLocal = longest.StartedAtLocal
            };
        }

        private static List<RankedItem> BuildBreakdown(IEnumerable<GameSnapshot> games, Func<GameSnapshot, IEnumerable<string>> selector, string emptyLabel)
        {
            var grouped = games.SelectMany(game =>
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

            var maximum = grouped.Count == 0 ? 0UL : grouped[0].DurationSeconds;
            for (var index = 0; index < grouped.Count; index++)
            {
                grouped[index].Rank = index + 1;
                grouped[index].RelativePercentage = maximum == 0 ? 0 : (double)grouped[index].DurationSeconds / maximum * 100;
            }

            return grouped;
        }

        private static List<RankedItem> AddRanking(List<RankedItem> items)
        {
            var maximum = items.Count == 0 ? 0UL : items[0].DurationSeconds;
            for (var index = 0; index < items.Count; index++)
            {
                items[index].Rank = index + 1;
                items[index].RelativePercentage = maximum == 0 ? 0 : (double)items[index].DurationSeconds / maximum * 100;
            }

            return items;
        }

        private static void AddSessionToDays(ActivitySession session, DateTime firstDate, DateTime lastDate, IDictionary<DateTime, HeatmapDay> totals)
        {
            if (session == null || session.DurationSeconds == 0)
            {
                return;
            }

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
