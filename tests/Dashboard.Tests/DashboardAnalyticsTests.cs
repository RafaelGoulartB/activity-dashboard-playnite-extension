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
                new GameSnapshot { Name = "Alpha", PlaytimeSeconds = 7200, PlayCount = 3, LastActivity = new DateTime(2026, 7, 19), Platforms = new List<string> { "Steam" }, Genres = new List<string> { "RPG", "Action" } },
                new GameSnapshot { Name = "Beta", PlaytimeSeconds = 3600, PlayCount = 0, Platforms = new List<string>(), Genres = new List<string>() }
            };

            var metrics = new DashboardAnalytics().Build(games, new ActivitySession[0], new DateTime(2026, 7, 20));

            Assert.AreEqual(10800UL, metrics.TotalPlaytimeSeconds);
            Assert.AreEqual(2, metrics.GamesPlayed);
            Assert.AreEqual(3UL, metrics.TotalLaunches);
            Assert.AreEqual(1, metrics.GamesActiveLast30Days);
            Assert.AreEqual("Alpha", metrics.TopGames[0].Name);
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
    }
}
