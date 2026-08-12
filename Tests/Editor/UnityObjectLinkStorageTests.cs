using System;
using System.IO;
using NUnit.Framework;

namespace UnityObjectLink.Tests
{
    public sealed class UnityObjectLinkStorageTests
    {
        private string temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "UnityObjectLinkTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [Test]
        public void Paths_AreContainedUnderValidatedInstanceDirectory()
        {
            var storage = new UnityObjectLinkStorage(temporaryDirectory);
            string instance = storage.GetInstanceDirectory("unity-object-link", "sample-project");
            Assert.That(instance, Is.EqualTo(Path.Combine(temporaryDirectory, "instances", "unity-object-link", "sample-project")));
            Assert.That(storage.GetInboxDirectory("unity-object-link", "sample-project"), Is.EqualTo(Path.Combine(instance, "inbox")));
            Assert.That(storage.GetHeartbeatPath("unity-object-link", "sample-project"), Is.EqualTo(Path.Combine(instance, "heartbeat.json")));
        }

        [Test]
        public void Paths_RejectTraversal()
        {
            var storage = new UnityObjectLinkStorage(temporaryDirectory);
            Assert.Throws<ArgumentException>(() => storage.GetInstanceDirectory("unity-object-link", "../outside"));
        }

        [Test]
        public void AtomicWriter_CreatesAndReplacesCompleteFile()
        {
            string path = Path.Combine(temporaryDirectory, "instance", "heartbeat.json");
            UnityObjectLinkStorage.WriteAllTextAtomic(path, "first");
            UnityObjectLinkStorage.WriteAllTextAtomic(path, "second");

            Assert.That(File.ReadAllText(path), Is.EqualTo("second"));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp"), Is.Empty);
        }

        [Test]
        public void HeartbeatFreshness_UsesInjectedCurrentTimeRules()
        {
            string path = Path.Combine(temporaryDirectory, "heartbeat.json");
            File.WriteAllText(path, "{}");
            DateTime now = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

            File.SetLastWriteTimeUtc(path, now - UnityObjectLinkHeartbeat.MaximumAge + TimeSpan.FromSeconds(1));
            Assert.That(UnityObjectLinkHeartbeat.IsFresh(path, now), Is.True);

            File.SetLastWriteTimeUtc(path, now - UnityObjectLinkHeartbeat.MaximumAge - TimeSpan.FromSeconds(1));
            Assert.That(UnityObjectLinkHeartbeat.IsFresh(path, now), Is.False);

            File.SetLastWriteTimeUtc(path, now + UnityObjectLinkHeartbeat.MaximumFutureSkew + TimeSpan.FromSeconds(1));
            Assert.That(UnityObjectLinkHeartbeat.IsFresh(path, now), Is.False);
            Assert.That(UnityObjectLinkHeartbeat.IsFresh(Path.Combine(temporaryDirectory, "missing.json"), now), Is.False);
        }
    }
}
