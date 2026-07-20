using System.Collections.Generic;

namespace ActivityDashboard.Models
{
    public class ActivityDashboardData
    {
        public const int CurrentSchemaVersion = 1;

        public ActivityDashboardData()
        {
            SchemaVersion = CurrentSchemaVersion;
            Sessions = new List<ActivitySession>();
        }

        public int SchemaVersion { get; set; }
        public ActivitySession ActiveSession { get; set; }
        public List<ActivitySession> Sessions { get; set; }
    }
}

