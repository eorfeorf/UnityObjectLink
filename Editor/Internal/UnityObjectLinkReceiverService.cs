using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityObjectLink
{
    [InitializeOnLoad]
    internal static class UnityObjectLinkReceiverService
    {
        private const double PollIntervalSeconds = 0.25;
        private const double HeartbeatIntervalSeconds = 5.0;

        private static readonly UnityObjectLinkStorage Storage = UnityObjectLinkStorage.CreateDefault();
        private static UnityObjectLinkInboxProcessor processor;
        private static string activeScheme;
        private static string activeProjectId;
        private static string heartbeatPath;
        private static string lastReceiveState = "No link has been processed in this Editor session.";
        private static double nextPoll;
        private static double nextHeartbeat;

        static UnityObjectLinkReceiverService()
        {
            EditorApplication.update += Update;
            EditorApplication.quitting += Stop;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            Restart();
        }

        internal static string HeartbeatPath { get { return heartbeatPath; } }
        internal static string InboxPath { get { return activeScheme == null ? string.Empty : Storage.GetInboxDirectory(activeScheme, activeProjectId); } }
        internal static string LastReceiveState { get { return lastReceiveState; } }
        internal static int PendingRequestCount
        {
            get
            {
                string inbox = InboxPath;
                if (string.IsNullOrEmpty(inbox))
                {
                    return -1;
                }

                try
                {
                    return Directory.Exists(inbox) ? Directory.GetFiles(inbox, "*.request", SearchOption.TopDirectoryOnly).Length : 0;
                }
                catch (IOException)
                {
                    return -1;
                }
                catch (UnauthorizedAccessException)
                {
                    return -1;
                }
            }
        }

        internal static void Restart()
        {
            DeleteCurrentHeartbeat();
            lastReceiveState = "No link has been processed in this Editor session.";
            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            activeScheme = settings.Scheme;
            activeProjectId = settings.ProjectId;
            heartbeatPath = Storage.GetHeartbeatPath(activeScheme, activeProjectId);
            string inbox = Storage.GetInboxDirectory(activeScheme, activeProjectId);
            Directory.CreateDirectory(inbox);
            processor = new UnityObjectLinkInboxProcessor(inbox, delegate { return DateTime.UtcNow; }, HandleReceivedLink);
            nextPoll = 0;
            nextHeartbeat = 0;
            WriteHeartbeat();
        }

        private static void Update()
        {
            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            if (!string.Equals(activeScheme, settings.Scheme, StringComparison.OrdinalIgnoreCase) || activeProjectId != settings.ProjectId)
            {
                Restart();
            }

            double now = EditorApplication.timeSinceStartup;
            if (now >= nextHeartbeat)
            {
                WriteHeartbeat();
                nextHeartbeat = now + HeartbeatIntervalSeconds;
            }

            if (now >= nextPoll)
            {
                processor.ProcessOnce();
                nextPoll = now + PollIntervalSeconds;
            }
        }

        private static void WriteHeartbeat()
        {
            var payload = new HeartbeatPayload
            {
                version = 1,
                scheme = activeScheme,
                projectId = activeProjectId,
                processId = Process.GetCurrentProcess().Id,
                updatedUtc = DateTime.UtcNow.ToString("O")
            };

            try
            {
                UnityObjectLinkStorage.WriteAllTextAtomic(heartbeatPath, JsonUtility.ToJson(payload, true));
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning(UnityObjectLinkNotifications.LogPrefix + "Could not update the heartbeat: " + exception.Message);
            }
        }

        private static UnityObjectLinkResult HandleReceivedLink(string uri)
        {
            UnityObjectLinkResult result = UnityObjectLinkApi.HandleLink(uri);
            lastReceiveState = DateTime.UtcNow.ToString("u") + " UTC - " + result.Status + ": " + result.Message;
            return result;
        }

        private static void Stop()
        {
            DeleteCurrentHeartbeat();
        }

        private static void DeleteCurrentHeartbeat()
        {
            if (string.IsNullOrEmpty(heartbeatPath))
            {
                return;
            }

            try
            {
                File.Delete(heartbeatPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [Serializable]
        private sealed class HeartbeatPayload
        {
            public int version;
            public string scheme;
            public string projectId;
            public int processId;
            public string updatedUtc;
        }
    }
}
