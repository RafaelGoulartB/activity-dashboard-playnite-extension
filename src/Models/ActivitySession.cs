using System;

namespace ActivityDashboard.Models
{
    public class ActivitySession
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public DateTimeOffset StartedAtLocal { get; set; }
        public DateTimeOffset EndedAtLocal { get; set; }
        public ulong DurationSeconds { get; set; }
    }
}

