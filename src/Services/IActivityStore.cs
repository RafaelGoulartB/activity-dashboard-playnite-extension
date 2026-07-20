using System;
using ActivityDashboard.Models;

namespace ActivityDashboard.Services
{
    public interface IActivityStore
    {
        ActivityDashboardData Load();
        ActivitySession StartSession(Guid gameId, string gameName, DateTimeOffset startedAtLocal);
        ActivitySession EndSession(Guid gameId, ulong elapsedSeconds, DateTimeOffset endedAtLocal);
        void ClearHistory();
    }
}

