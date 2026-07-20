using System;
using System.IO;
using ActivityDashboard.Models;
using ActivityDashboard.Services;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dashboard.Tests
{
    [TestFixture]
    public class JsonActivityStoreTests
    {
        private string testDirectory;

        [SetUp]
        public void SetUp()
        {
            testDirectory = Path.Combine(Path.GetTempPath(), "activity-dashboard-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        public void Load_MissingFileReturnsEmptyData()
        {
            var data = new JsonActivityStore(testDirectory).Load();

            Assert.AreEqual(ActivityDashboardData.CurrentSchemaVersion, data.SchemaVersion);
            Assert.IsEmpty(data.Sessions);
            Assert.IsNull(data.ActiveSession);
        }

        [Test]
        public void Load_CorruptFileQuarantinesItAndStartsEmpty()
        {
            File.WriteAllText(Path.Combine(testDirectory, "activity-dashboard.json"), "not json");

            var data = new JsonActivityStore(testDirectory).Load();

            Assert.IsEmpty(data.Sessions);
            Assert.IsFalse(File.Exists(Path.Combine(testDirectory, "activity-dashboard.json")));
            Assert.AreEqual(1, Directory.GetFiles(testDirectory, "activity-dashboard.json.corrupt-*").Length);
        }

        [Test]
        public void Load_UnknownSchemaQuarantinesIt()
        {
            File.WriteAllText(Path.Combine(testDirectory, "activity-dashboard.json"), JsonConvert.SerializeObject(new ActivityDashboardData { SchemaVersion = 999 }));

            var data = new JsonActivityStore(testDirectory).Load();

            Assert.AreEqual(ActivityDashboardData.CurrentSchemaVersion, data.SchemaVersion);
            Assert.AreEqual(1, Directory.GetFiles(testDirectory, "activity-dashboard.json.corrupt-*").Length);
        }
    }
}
