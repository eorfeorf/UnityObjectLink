using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityObjectLink.Tests
{
    public sealed class UnityObjectLinkInboxProcessorTests
    {
        private const string ValidUri = "unity-object-link://select?v=1&project=sample&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-1-0";
        private string temporaryDirectory;
        private DateTime now;
        private int handled;

        private sealed class MemoryFileSystem : IUnityObjectLinkFileSystem
        {
            private readonly Dictionary<string, string> contents = new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly Dictionary<string, DateTime> timestamps = new Dictionary<string, DateTime>(StringComparer.Ordinal);

            internal readonly List<string> DeletedFiles = new List<string>();

            internal void Add(string path, string content, DateTime timestamp)
            {
                contents.Add(path, content);
                timestamps.Add(path, timestamp);
            }

            public void CreateDirectory(string path)
            {
            }

            public string[] GetRequestFiles(string directory)
            {
                var files = new List<string>();
                foreach (string path in contents.Keys)
                {
                    if (path.EndsWith(".request", StringComparison.Ordinal) && !DeletedFiles.Contains(path))
                    {
                        files.Add(path);
                    }
                }

                return files.ToArray();
            }

            public long GetFileLength(string path)
            {
                return Encoding.UTF8.GetByteCount(contents[path]);
            }

            public DateTime GetLastWriteTimeUtc(string path)
            {
                return timestamps[path];
            }

            public string ReadAllTextUtf8(string path)
            {
                return contents[path];
            }

            public void DeleteFile(string path)
            {
                DeletedFiles.Add(path);
            }
        }

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "UnityObjectLinkInboxTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            now = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            handled = 0;
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
        public void ProcessOnce_HandlesValidRequestAndDeletesIt()
        {
            WriteRequest("one.request", ValidUri, now);
            UnityObjectLinkInboxProcessor processor = CreateProcessor();

            Assert.That(processor.ProcessOnce(), Is.EqualTo(1));
            Assert.That(handled, Is.EqualTo(1));
            Assert.That(Directory.GetFiles(temporaryDirectory), Is.Empty);
        }

        [Test]
        public void ProcessOnce_RejectsDuplicateRequest()
        {
            UnityObjectLinkInboxProcessor processor = CreateProcessor();
            WriteRequest("one.request", ValidUri, now);
            Assert.That(processor.ProcessOnce(), Is.EqualTo(1));

            WriteRequest("two.request", ValidUri, now);
            LogAssert.Expect(LogType.Warning, "[UnityObjectLink] Discarded a duplicate link request.");
            Assert.That(processor.ProcessOnce(), Is.Zero);
            Assert.That(handled, Is.EqualTo(1));
        }

        [Test]
        public void ProcessOnce_RejectsExpiredAndOversizedRequests()
        {
            UnityObjectLinkInboxProcessor processor = CreateProcessor();
            WriteRequest("old.request", ValidUri, now - UnityObjectLinkInboxProcessor.RequestTimeToLive - TimeSpan.FromSeconds(1));
            LogAssert.Expect(LogType.Warning, "[UnityObjectLink] Discarded a stale, empty, or oversized link request.");
            Assert.That(processor.ProcessOnce(), Is.Zero);

            WriteRequest("large.request", new string('x', (int)UnityObjectLinkInboxProcessor.MaximumRequestBytes + 1), now);
            LogAssert.Expect(LogType.Warning, "[UnityObjectLink] Discarded a stale, empty, or oversized link request.");
            Assert.That(processor.ProcessOnce(), Is.Zero);
            Assert.That(handled, Is.Zero);
        }

        [Test]
        public void ProcessOnce_RejectsMalformedUtf8()
        {
            string path = Path.Combine(temporaryDirectory, "invalid.request");
            File.WriteAllBytes(path, new byte[] { 0xff, 0xfe, 0xff });
            File.SetLastWriteTimeUtc(path, now);
            UnityObjectLinkInboxProcessor processor = CreateProcessor();

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("^\\[UnityObjectLink\\] Discarded an unreadable link request:"));
            Assert.That(processor.ProcessOnce(), Is.Zero);
            Assert.That(File.Exists(path), Is.False);
            Assert.That(handled, Is.Zero);
        }

        [Test]
        public void ProcessOnce_RejectsFarFutureRequest()
        {
            UnityObjectLinkInboxProcessor processor = CreateProcessor();
            WriteRequest("future.request", ValidUri, now + UnityObjectLinkInboxProcessor.MaximumFutureSkew + TimeSpan.FromSeconds(1));

            LogAssert.Expect(LogType.Warning, "[UnityObjectLink] Discarded a stale, empty, or oversized link request.");
            Assert.That(processor.ProcessOnce(), Is.Zero);
            Assert.That(handled, Is.Zero);
        }

        [Test]
        public void ProcessOnce_UsesInjectedClockAndFileSystemBoundaries()
        {
            string inbox = Path.Combine("memory", "inbox");
            string request = Path.Combine(inbox, "one.request");
            var fileSystem = new MemoryFileSystem();
            fileSystem.Add(request, ValidUri, now);
            var processor = new UnityObjectLinkInboxProcessor(
                inbox,
                delegate { return now; },
                delegate(string uri)
                {
                    handled++;
                    Assert.That(uri, Is.EqualTo(ValidUri));
                    return new UnityObjectLinkResult(UnityObjectLinkStatus.Success, "handled", uri, null);
                },
                fileSystem);

            Assert.That(processor.ProcessOnce(), Is.EqualTo(1));
            Assert.That(handled, Is.EqualTo(1));
            Assert.That(fileSystem.DeletedFiles, Is.EqualTo(new[] { request }));
        }

        private UnityObjectLinkInboxProcessor CreateProcessor()
        {
            return new UnityObjectLinkInboxProcessor(
                temporaryDirectory,
                delegate { return now; },
                delegate(string uri)
                {
                    handled++;
                    return new UnityObjectLinkResult(UnityObjectLinkStatus.Success, "handled", uri, null);
                });
        }

        private void WriteRequest(string name, string contents, DateTime timestamp)
        {
            string path = Path.Combine(temporaryDirectory, name);
            File.WriteAllText(path, contents);
            File.SetLastWriteTimeUtc(path, timestamp);
        }
    }
}
