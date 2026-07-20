using System;
using System.Collections.Generic;

namespace ActivityDashboard.Models
{
    public class GameSnapshot
    {
        public GameSnapshot()
        {
            Platforms = new List<string>();
            Genres = new List<string>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string CoverPath { get; set; }
        public ulong PlaytimeSeconds { get; set; }
        public ulong PlayCount { get; set; }
        public DateTime? LastActivity { get; set; }
        public List<string> Platforms { get; set; }
        public List<string> Genres { get; set; }
    }
}

