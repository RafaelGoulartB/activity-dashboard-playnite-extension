using System;
using System.IO;
using ActivityDashboard.Models;
using Newtonsoft.Json;

namespace ActivityDashboard.Services
{
    public class JsonActivityStore : IActivityStore
    {
        private readonly object syncRoot = new object();
        private readonly string dataPath;
        private ActivityDashboardData data;

        public JsonActivityStore(string extensionsDataPath)
        {
            dataPath = Path.Combine(extensionsDataPath, "activity-dashboard.json");
        }

        public ActivityDashboardData Load()
        {
            lock (syncRoot)
            {
                if (data != null)
                {
                    return data;
                }

                if (!File.Exists(dataPath))
                {
                    data = new ActivityDashboardData();
                    return data;
                }

                try
                {
                    var json = File.ReadAllText(dataPath);
                    data = JsonConvert.DeserializeObject<ActivityDashboardData>(json);
                    if (data == null || data.SchemaVersion != ActivityDashboardData.CurrentSchemaVersion)
                    {
                        throw new InvalidDataException("Unsupported activity dashboard data schema.");
                    }

                    if (data.Sessions == null)
                    {
                        data.Sessions = new System.Collections.Generic.List<ActivitySession>();
                    }

                    return data;
                }
                catch
                {
                    QuarantineInvalidFile();
                    data = new ActivityDashboardData();
                    return data;
                }
            }
        }

        public ActivitySession StartSession(Guid gameId, string gameName, DateTimeOffset startedAtLocal)
        {
            lock (syncRoot)
            {
                var current = Load();
                if (current.ActiveSession != null)
                {
                    return null;
                }

                current.ActiveSession = new ActivitySession
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    GameName = gameName ?? string.Empty,
                    StartedAtLocal = startedAtLocal
                };
                Save();
                return current.ActiveSession;
            }
        }

        public ActivitySession EndSession(Guid gameId, ulong elapsedSeconds, DateTimeOffset endedAtLocal)
        {
            lock (syncRoot)
            {
                var current = Load();
                if (elapsedSeconds == 0 || current.ActiveSession == null || current.ActiveSession.GameId != gameId)
                {
                    return null;
                }

                var session = current.ActiveSession;
                session.DurationSeconds = elapsedSeconds;
                session.EndedAtLocal = endedAtLocal;
                session.StartedAtLocal = endedAtLocal.AddSeconds(-(double)elapsedSeconds);
                current.Sessions.Add(session);
                current.ActiveSession = null;
                Save();
                return session;
            }
        }

        public void ClearHistory()
        {
            lock (syncRoot)
            {
                data = new ActivityDashboardData();
                Save();
            }
        }

        private void Save()
        {
            var directory = Path.GetDirectoryName(dataPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = dataPath + ".tmp";
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(dataPath))
            {
                File.Replace(temporaryPath, dataPath, null);
            }
            else
            {
                File.Move(temporaryPath, dataPath);
            }
        }

        private void QuarantineInvalidFile()
        {
            try
            {
                var corruptPath = dataPath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Move(dataPath, corruptPath);
            }
            catch
            {
                // A read-only or locked corrupt file must not prevent the dashboard from opening.
            }
        }
    }
}
