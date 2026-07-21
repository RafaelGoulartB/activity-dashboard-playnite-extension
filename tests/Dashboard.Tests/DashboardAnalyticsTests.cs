using System;
using System.Collections.Generic;
using System.Linq;
using ActivityDashboard.Models;
using ActivityDashboard.Services;
using NUnit.Framework;

namespace Dashboard.Tests
{
    [TestFixture]
    public class DashboardAnalyticsTests
    {
        [Test]
        public void Format_UsesHoursAndMinutes()
        {
            Assert.AreEqual("2h 05min", DurationFormatter.Format(7500));
            Assert.AreEqual("1min", DurationFormatter.Format(1));
        }

        [Test]
        public void Build_AggregatesLibraryAndUsesEmptyClassifications()
        {
            var games = new[]
            {
                new GameSnapshot { Name = "Alpha", PlaytimeSeconds = 7200, PlayCount = 3, IsFavorite = true, LastActivity = new DateTime(2026, 7, 19), Platforms = new List<string> { "Steam" }, Genres = new List<string> { "RPG", "Action" } },
                new GameSnapshot { Name = "Beta", PlaytimeSeconds = 3600, PlayCount = 0, Platforms = new List<string>(), Genres = new List<string>() }
            };

            var metrics = new DashboardAnalytics().Build(games, new ActivitySession[0], new DateTime(2026, 7, 20));

            Assert.AreEqual(10800UL, metrics.TotalPlaytimeSeconds);
            Assert.AreEqual(2, metrics.GamesPlayed);
            Assert.AreEqual(3UL, metrics.TotalLaunches);
            Assert.AreEqual(1, metrics.GamesActiveLast30Days);
            Assert.AreEqual("Alpha", metrics.TopGames[0].Name);
            Assert.AreEqual(1, metrics.FavoriteGames.Count);
            Assert.AreEqual("Alpha", metrics.FavoriteGames[0].Name);
            Assert.AreEqual(3600UL, metrics.Platforms.Single(item => item.Name == "Uncategorized platform").DurationSeconds);
            Assert.AreEqual(3600UL, metrics.Genres.Single(item => item.Name == "Uncategorized genre").DurationSeconds);
        }

        [Test]
        public void Build_ReturnsTheTopTwentyGamesByPlaytime()
        {
            var games = Enumerable.Range(1, 25)
                .Select(index => new GameSnapshot
                {
                    Name = "Game " + index.ToString("D2"),
                    PlaytimeSeconds = (ulong)index
                });

            var metrics = new DashboardAnalytics().Build(games, new ActivitySession[0], new DateTime(2026, 7, 20));

            Assert.AreEqual(20, metrics.TopGames.Count);
            Assert.AreEqual("Game 25", metrics.TopGames[0].Name);
            Assert.AreEqual("Game 06", metrics.TopGames[19].Name);
            Assert.AreEqual(1, metrics.TopGames[0].Rank);
            Assert.AreEqual(100d, metrics.TopGames[0].RelativePercentage);
        }

        [Test]
        public void BuildTopGamesForPeriod_AggregatesTrackedSessionsAndClipsThePeriodBoundary()
        {
            var alphaId = Guid.NewGuid();
            var betaId = Guid.NewGuid();
            var games = new[]
            {
                new GameSnapshot { Id = alphaId, Name = "Alpha", CoverPath = "alpha.jpg" },
                new GameSnapshot { Id = betaId, Name = "Beta", CoverPath = "beta.jpg" }
            };
            var sessions = new[]
            {
                new ActivitySession { GameId = alphaId, GameName = "Old Alpha", StartedAtLocal = new DateTimeOffset(2026, 7, 20, 18, 0, 0, TimeSpan.Zero), DurationSeconds = 7200 },
                new ActivitySession { GameId = betaId, GameName = "Beta", StartedAtLocal = new DateTimeOffset(2026, 7, 13, 23, 30, 0, TimeSpan.Zero), DurationSeconds = 3600 },
                new ActivitySession { GameId = betaId, GameName = "Beta", StartedAtLocal = new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero), DurationSeconds = 3600 },
                new ActivitySession { GameId = alphaId, GameName = "Alpha", StartedAtLocal = new DateTimeOffset(2026, 7, 20, 20, 0, 0, TimeSpan.Zero), DurationSeconds = 0 }
            };

            var ranking = new DashboardAnalytics().BuildTopGamesForPeriod(games, sessions, new DateTime(2026, 7, 20), 7);

            Assert.AreEqual(2, ranking.Count);
            Assert.AreEqual("Alpha", ranking[0].Name);
            Assert.AreEqual("alpha.jpg", ranking[0].CoverPath);
            Assert.AreEqual(7200UL, ranking[0].DurationSeconds);
            Assert.AreEqual(1, ranking[0].Rank);
            Assert.AreEqual(1800UL, ranking[1].DurationSeconds);
            Assert.AreEqual(25d, ranking[1].RelativePercentage);
        }

        [Test]
        public void BuildTopGamesForPeriod_AllTrackedTimeIncludesOlderSessions()
        {
            var gameId = Guid.NewGuid();
            var session = new ActivitySession
            {
                GameId = gameId,
                GameName = "Alpha",
                StartedAtLocal = new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.Zero),
                DurationSeconds = 600
            };

            var ranking = new DashboardAnalytics().BuildTopGamesForPeriod(new GameSnapshot[0], new[] { session }, new DateTime(2026, 7, 20), 0);

            Assert.AreEqual(1, ranking.Count);
            Assert.AreEqual("Alpha", ranking[0].Name);
            Assert.AreEqual(600UL, ranking[0].DurationSeconds);
        }

        [Test]
        public void BuildHeatmap_SplitsASessionAcrossMidnight()
        {
            var session = new ActivitySession
            {
                StartedAtLocal = new DateTimeOffset(2026, 7, 19, 23, 0, 0, TimeSpan.Zero),
                EndedAtLocal = new DateTimeOffset(2026, 7, 20, 1, 0, 0, TimeSpan.Zero),
                DurationSeconds = 7200
            };

            var days = new DashboardAnalytics().BuildHeatmap(new[] { session }, new DateTime(2026, 7, 20));

            Assert.AreEqual(364, days.Count);
            Assert.AreEqual(3600UL, days.Single(day => day.Date == new DateTime(2026, 7, 19)).DurationSeconds);
            Assert.AreEqual(3600UL, days.Single(day => day.Date == new DateTime(2026, 7, 20)).DurationSeconds);
            Assert.AreEqual(DayOfWeek.Monday, days.First().Date.DayOfWeek);
        }

        [Test]
        public void BuildHourlyActivity_SplitsSessionsAtHourBoundaries()
        {
            var session = new ActivitySession
            {
                StartedAtLocal = new DateTimeOffset(2026, 7, 20, 22, 30, 0, TimeSpan.Zero),
                EndedAtLocal = new DateTimeOffset(2026, 7, 21, 1, 15, 0, TimeSpan.Zero),
                DurationSeconds = 9900
            };

            var hours = new DashboardAnalytics().BuildHourlyActivity(new[] { session });

            Assert.AreEqual(24, hours.Count);
            Assert.AreEqual(1800UL, hours[22].DurationSeconds);
            Assert.AreEqual(3600UL, hours[23].DurationSeconds);
            Assert.AreEqual(3600UL, hours[0].DurationSeconds);
            Assert.AreEqual(900UL, hours[1].DurationSeconds);
            Assert.AreEqual(0UL, hours[2].DurationSeconds);
        }

        [Test]
        public void Build_ExposesStreaksAndSessionAggregates()
        {
            var games = new[] { new GameSnapshot { Name = "Alpha", PlaytimeSeconds = 3600, PlayCount = 2 } };
            var sessions = new[]
            {
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Alpha",
                    StartedAtLocal = new DateTimeOffset(2026, 7, 15, 20, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 1800
                },
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Alpha",
                    StartedAtLocal = new DateTimeOffset(2026, 7, 16, 21, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 3600
                },
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Alpha",
                    StartedAtLocal = new DateTimeOffset(2026, 7, 20, 22, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 7200
                }
            };

            var metrics = new DashboardAnalytics().Build(games, sessions, new DateTime(2026, 7, 20));

            Assert.AreEqual(3, metrics.TotalSessions);
            Assert.AreEqual(4200UL, metrics.AverageSessionSeconds);
            Assert.AreEqual(12600UL, metrics.TrackedDurationSeconds);
            Assert.AreEqual(1, metrics.Streak.CurrentStreak);
            Assert.AreEqual(2, metrics.Streak.LongestStreak);
            Assert.AreEqual(new DateTime(2026, 7, 20), metrics.Streak.LastActiveDate);
            Assert.AreEqual(3, metrics.ActiveDaysLast30);
            Assert.AreEqual(new DateTime(2026, 7, 15), metrics.FirstSessionDate);
            Assert.AreEqual(0UL, metrics.WeekendSeconds);
            Assert.IsTrue(metrics.WeekdaySeconds > 0);
        }

        [Test]
        public void BuildStreak_StopsAtFirstInactiveDayAndRecalculatesLongest()
        {
            var analytics = new DashboardAnalytics();
            var heatmap = new List<HeatmapDay>
            {
                new HeatmapDay { Date = new DateTime(2026, 7, 10), DurationSeconds = 600 },
                new HeatmapDay { Date = new DateTime(2026, 7, 11), DurationSeconds = 600 },
                new HeatmapDay { Date = new DateTime(2026, 7, 12), DurationSeconds = 600 },
                new HeatmapDay { Date = new DateTime(2026, 7, 13), DurationSeconds = 600 },
                new HeatmapDay { Date = new DateTime(2026, 7, 15), DurationSeconds = 600 },
                new HeatmapDay { Date = new DateTime(2026, 7, 16), DurationSeconds = 600 }
            };

            var streak = analytics.BuildStreak(heatmap, new DateTime(2026, 7, 16));

            Assert.AreEqual(2, streak.CurrentStreak);
            Assert.AreEqual(4, streak.LongestStreak);
        }

        [Test]
        public void BuildStreak_ReturnsZerosForEmptyData()
        {
            var streak = new DashboardAnalytics().BuildStreak(new HeatmapDay[0], new DateTime(2026, 7, 20));

            Assert.AreEqual(0, streak.CurrentStreak);
            Assert.AreEqual(0, streak.LongestStreak);
            Assert.IsNull(streak.LastActiveDate);
        }

        [Test]
        public void BuildMonthlyBuckets_AggregatesAcrossMonthBoundaries()
        {
            var sessions = new[]
            {
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    StartedAtLocal = new DateTimeOffset(2026, 5, 31, 23, 30, 0, TimeSpan.Zero),
                    DurationSeconds = 3600
                },
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    StartedAtLocal = new DateTimeOffset(2026, 6, 1, 0, 30, 0, TimeSpan.Zero),
                    DurationSeconds = 1800
                },
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    StartedAtLocal = new DateTimeOffset(2026, 7, 15, 14, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 0
                }
            };

            var buckets = new DashboardAnalytics().BuildMonthlyBuckets(sessions, new DateTime(2026, 7, 20));

            Assert.AreEqual(12, buckets.Count);
            var may = buckets.Single(bucket => bucket.Year == 2026 && bucket.Month == 5);
            var june = buckets.Single(bucket => bucket.Year == 2026 && bucket.Month == 6);
            var july = buckets.Single(bucket => bucket.Year == 2026 && bucket.Month == 7);
            Assert.AreEqual(1800UL, may.DurationSeconds);
            Assert.AreEqual(1, may.SessionCount);
            Assert.AreEqual(3600UL, june.DurationSeconds);
            Assert.AreEqual(2, june.SessionCount);
            Assert.AreEqual(0UL, july.DurationSeconds);
            Assert.AreEqual(0, july.SessionCount);
        }

        [Test]
        public void BuildWeekdayBreakdown_AccumulatesPerDayAndComputesRelativePercentage()
        {
            var sessions = new[]
            {
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    StartedAtLocal = new DateTimeOffset(2026, 7, 13, 10, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 3600
                },
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    StartedAtLocal = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 1800
                },
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    StartedAtLocal = new DateTimeOffset(2026, 7, 18, 20, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 1800
                },
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    StartedAtLocal = new DateTimeOffset(2026, 7, 19, 22, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 0
                }
            };

            var buckets = new DashboardAnalytics().BuildWeekdayBreakdown(sessions);

            Assert.AreEqual(7, buckets.Count);
            var monday = buckets[(int)DayOfWeek.Monday];
            var wednesday = buckets[(int)DayOfWeek.Wednesday];
            var saturday = buckets[(int)DayOfWeek.Saturday];
            Assert.AreEqual(3600UL, monday.DurationSeconds);
            Assert.AreEqual(1800UL, wednesday.DurationSeconds);
            Assert.AreEqual(1800UL, saturday.DurationSeconds);
            Assert.AreEqual(100d, monday.RelativePercentage);
            Assert.AreEqual(50d, wednesday.RelativePercentage);
            Assert.AreEqual(50d, saturday.RelativePercentage);
        }

        [Test]
        public void BuildSessionLengthDistribution_PlacesSessionsInCorrectBuckets()
        {
            var sessions = new[]
            {
                MakeSession(60),
                MakeSession(300),
                MakeSession(900),
                MakeSession(2700),
                MakeSession(5400),
                MakeSession(10800),
                MakeSession(0)
            };

            var buckets = new DashboardAnalytics().BuildSessionLengthDistribution(sessions);

            Assert.AreEqual(6, buckets.Count);
            Assert.AreEqual(2, buckets[0].Count);
            Assert.AreEqual(1, buckets[1].Count);
            Assert.AreEqual(1, buckets[2].Count);
            Assert.AreEqual(1, buckets[3].Count);
            Assert.AreEqual(1, buckets[4].Count);
            Assert.AreEqual(0, buckets[5].Count);
            Assert.AreEqual(100d, buckets[0].RelativePercentage);
            Assert.AreEqual(50d, buckets[1].RelativePercentage);
            Assert.AreEqual(0d, buckets[5].RelativePercentage);
        }

        [Test]
        public void BuildSessionLengthDistribution_HandlesEmptyData()
        {
            var buckets = new DashboardAnalytics().BuildSessionLengthDistribution(new ActivitySession[0]);

            Assert.AreEqual(6, buckets.Count);
            Assert.AreEqual(0, buckets[0].Count);
            Assert.AreEqual(0d, buckets[0].RelativePercentage);
        }

        [Test]
        public void Build_IdentifiesLongestSession()
        {
            var sessions = new[]
            {
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Short",
                    StartedAtLocal = new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 600
                },
                new ActivitySession
                {
                    GameId = Guid.NewGuid(),
                    GameName = "Marathon",
                    StartedAtLocal = new DateTimeOffset(2026, 7, 20, 10, 0, 0, TimeSpan.Zero),
                    DurationSeconds = 14400
                }
            };

            var metrics = new DashboardAnalytics().Build(new GameSnapshot[0], sessions, new DateTime(2026, 7, 20));

            Assert.IsNotNull(metrics.LongestSession);
            Assert.AreEqual("Marathon", metrics.LongestSession.GameName);
            Assert.AreEqual(14400UL, metrics.LongestSession.DurationSeconds);
        }

        [Test]
        public void Build_ReturnsNullLongestSessionWhenNothingTracked()
        {
            var metrics = new DashboardAnalytics().Build(new GameSnapshot[0], new ActivitySession[0], new DateTime(2026, 7, 20));

            Assert.IsNull(metrics.LongestSession);
            Assert.IsNull(metrics.FirstSessionDate);
            Assert.AreEqual(0, metrics.Streak.CurrentStreak);
        }

        private static ActivitySession MakeSession(ulong durationSeconds)
        {
            return new ActivitySession
            {
                GameId = Guid.NewGuid(),
                GameName = "Sample",
                StartedAtLocal = new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero),
                DurationSeconds = durationSeconds
            };
        }
    }
}
