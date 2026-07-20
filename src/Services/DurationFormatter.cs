using System;

namespace ActivityDashboard.Services
{
    public static class DurationFormatter
    {
        public static string Format(ulong seconds)
        {
            var duration = TimeSpan.FromSeconds(seconds);
            if (duration.TotalHours >= 1)
            {
                return string.Format("{0}h {1:D2}min", (int)duration.TotalHours, duration.Minutes);
            }

            return string.Format("{0}min", Math.Max(1, duration.Minutes));
        }
    }
}

