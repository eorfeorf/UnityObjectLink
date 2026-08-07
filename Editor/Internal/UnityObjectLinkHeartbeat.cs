using System;
using System.IO;

namespace UnityObjectLink
{
    internal static class UnityObjectLinkHeartbeat
    {
        internal static readonly TimeSpan MaximumAge = TimeSpan.FromSeconds(15);
        internal static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromSeconds(5);

        internal static bool IsFresh(string path, DateTime utcNow)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            DateTime timestamp = File.GetLastWriteTimeUtc(path);
            TimeSpan age = utcNow - timestamp;
            return age <= MaximumAge && age >= -MaximumFutureSkew;
        }
    }
}
