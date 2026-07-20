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
                new GameSnapshot { Name = "Alpha", PlaytimeSeconds = 7200, PlayCount = 3, LastActivity = new DateTime(2026, 7, 19), Platforms = new List<string> { "Steam" }, Genres = new List<string> { "RPG", "Ação" } },
                new GameSnapshot { Name = "Beta", PlaytimeSeconds = 3600, PlayCount = 0, Platforms = new List<string>(), Genres = new List<string>() }
            };

            var metrics = new DashboardAnalytics().Build(games, new ActivitySession[0], new DateTime(2026, 7, 20));

            Assert.AreEqual(10800UL, metrics.TotalPlaytimeSeconds);
            Assert.AreEqual(2, metrics.GamesPlayed);
            Assert.AreEqual(3UL, metrics.TotalLaunches);
            Assert.AreEqual(1, metrics.GamesActiveLast30Days);
            Assert.AreEqual("Alpha", metrics.TopGames[0].Name);
            Assert.AreEqual(3600UL, metrics.Platforms.Single(item => item.Name == "Sem plataforma").DurationSeconds);
            Assert.AreEqual(3600UL, metrics.Genres.Single(item => item.Name == "Sem gênero").DurationSeconds);
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
    }
}

