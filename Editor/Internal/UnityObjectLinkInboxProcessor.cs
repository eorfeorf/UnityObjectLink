using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UnityObjectLink
{
    internal sealed class UnityObjectLinkInboxProcessor
    {
        internal const long MaximumRequestBytes = UnityObjectLinkUri.MaximumUriLength * 4L;
        internal static readonly TimeSpan RequestTimeToLive = TimeSpan.FromSeconds(60);
        internal static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromSeconds(5);

        private readonly string inboxDirectory;
        private readonly Func<DateTime> utcNow;
        private readonly Func<string, UnityObjectLinkResult> handler;
        private readonly IUnityObjectLinkFileSystem fileSystem;
        private readonly Dictionary<string, DateTime> recentRequests = new Dictionary<string, DateTime>(StringComparer.Ordinal);

        internal UnityObjectLinkInboxProcessor(string inboxDirectory, Func<DateTime> utcNow, Func<string, UnityObjectLinkResult> handler)
            : this(inboxDirectory, utcNow, handler, PhysicalUnityObjectLinkFileSystem.Instance)
        {
        }

        internal UnityObjectLinkInboxProcessor(
            string inboxDirectory,
            Func<DateTime> utcNow,
            Func<string, UnityObjectLinkResult> handler,
            IUnityObjectLinkFileSystem fileSystem)
        {
            this.inboxDirectory = inboxDirectory;
            this.utcNow = utcNow;
            this.handler = handler;
            this.fileSystem = fileSystem;
        }

        internal int ProcessOnce()
        {
            DateTime now = utcNow();
            RemoveExpiredHashes(now);

            int handledCount = 0;
            string[] files;
            try
            {
                fileSystem.CreateDirectory(inboxDirectory);
                files = fileSystem.GetRequestFiles(inboxDirectory);
            }
            catch (Exception exception)
            {
                UnityObjectLinkNotifications.Show(new UnityObjectLinkResult(UnityObjectLinkStatus.InternalError, "Could not scan the link inbox: " + exception.Message, null, null));
                return 0;
            }

            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files)
            {
                try
                {
                    long length = fileSystem.GetFileLength(file);
                    TimeSpan age = now - fileSystem.GetLastWriteTimeUtc(file);
                    if (length <= 0 || length > MaximumRequestBytes || age > RequestTimeToLive || age < -MaximumFutureSkew)
                    {
                        UnityObjectLinkNotifications.Show(new UnityObjectLinkResult(UnityObjectLinkStatus.InvalidUri, "Discarded a stale, empty, or oversized link request.", null, null));
                        continue;
                    }

                    string rawUri = fileSystem.ReadAllTextUtf8(file);

                    string fingerprint = ComputeHash(rawUri);
                    if (recentRequests.ContainsKey(fingerprint))
                    {
                        UnityObjectLinkNotifications.Show(new UnityObjectLinkResult(UnityObjectLinkStatus.InvalidUri, "Discarded a duplicate link request.", rawUri, null));
                        continue;
                    }

                    recentRequests.Add(fingerprint, now);
                    handler(rawUri);
                    handledCount++;
                }
                catch (Exception exception)
                {
                    UnityObjectLinkNotifications.Show(new UnityObjectLinkResult(UnityObjectLinkStatus.InternalError, "Discarded an unreadable link request: " + exception.Message, null, null));
                }
                finally
                {
                    TryDelete(file);
                }
            }

            return handledCount;
        }

        private void RemoveExpiredHashes(DateTime now)
        {
            var expired = new List<string>();
            foreach (KeyValuePair<string, DateTime> pair in recentRequests)
            {
                if (now - pair.Value > RequestTimeToLive)
                {
                    expired.Add(pair.Key);
                }
            }

            foreach (string key in expired)
            {
                recentRequests.Remove(key);
            }
        }

        private static string ComputeHash(string text)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(text));
                return Convert.ToBase64String(hash);
            }
        }

        private void TryDelete(string path)
        {
            try
            {
                fileSystem.DeleteFile(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
