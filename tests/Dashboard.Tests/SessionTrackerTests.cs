using System;
using ActivityDashboard.Models;
using ActivityDashboard.Services;
using NUnit.Framework;

namespace Dashboard.Tests
{
    [TestFixture]
    public class SessionTrackerTests
    {
        [Test]
        public void Tracker_IgnoresDuplicateStartAndInvalidStop()
        {
            var store = new MemoryStore();
            var tracker = new SessionTracker(store);
            var gameId = Guid.NewGuid();

            Assert.IsNotNull(tracker.HandleGameStarted(gameId, "Game", DateTimeOffset.Now));
            Assert.IsNull(tracker.HandleGameStarted(gameId, "Game", DateTimeOffset.Now));
            Assert.IsNull(tracker.HandleGameStopped(gameId, 0, DateTimeOffset.Now));
            Assert.AreEqual(0, store.Load().Sessions.Count);
        }

        [Test]
        public void Tracker_CompletesMatchingSessionUsingElapsedSeconds()
        {
            var store = new MemoryStore();
            var tracker = new SessionTracker(store);
            var gameId = Guid.NewGuid();
            var ended = new DateTimeOffset(2026, 7, 20, 15, 0, 0, TimeSpan.Zero);
            tracker.HandleGameStarted(gameId, "Game", ended.AddHours(-2));

            var session = tracker.HandleGameStopped(gameId, 7200, ended);

            Assert.IsNotNull(session);
            Assert.AreEqual(7200UL, session.DurationSeconds);
            Assert.AreEqual(ended.AddHours(-2), session.StartedAtLocal);
        }

        private class MemoryStore : IActivityStore
        {
            private readonly ActivityDashboardData data = new ActivityDashboardData();

            public ActivityDashboardData Load() { return data; }
            public ActivitySession StartSession(Guid gameId, string gameName, DateTimeOffset startedAtLocal)
            {
                if (data.ActiveSession != null) return null;
                data.ActiveSession = new ActivitySession { Id = Guid.NewGuid(), GameId = gameId, GameName = gameName, StartedAtLocal = startedAtLocal };
                return data.ActiveSession;
            }

            public ActivitySession EndSession(Guid gameId, ulong elapsedSeconds, DateTimeOffset endedAtLocal)
            {
                if (data.ActiveSession == null || data.ActiveSession.GameId != gameId || elapsedSeconds == 0) return null;
                var session = data.ActiveSession;
                session.DurationSeconds = elapsedSeconds;
                session.EndedAtLocal = endedAtLocal;
                session.StartedAtLocal = endedAtLocal.AddSeconds(-(double)elapsedSeconds);
                data.ActiveSession = null;
                data.Sessions.Add(session);
                return session;
            }

            public void ClearHistory() { data.Sessions.Clear(); data.ActiveSession = null; }
        }
    }
}

