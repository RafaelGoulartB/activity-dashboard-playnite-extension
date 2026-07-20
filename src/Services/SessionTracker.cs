using System;
using ActivityDashboard.Models;

namespace ActivityDashboard.Services
{
    public class SessionTracker
    {
        private readonly IActivityStore store;

        public SessionTracker(IActivityStore store)
        {
            this.store = store;
        }

        public ActivitySession HandleGameStarted(Guid gameId, string gameName, DateTimeOffset startedAtLocal)
        {
            return store.StartSession(gameId, gameName, startedAtLocal);
        }

        public ActivitySession HandleGameStopped(Guid gameId, ulong elapsedSeconds, DateTimeOffset endedAtLocal)
        {
            return store.EndSession(gameId, elapsedSeconds, endedAtLocal);
        }
    }
}

