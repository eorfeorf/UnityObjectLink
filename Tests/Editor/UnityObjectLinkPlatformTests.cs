using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityObjectLink.Tests
{
    public sealed class UnityObjectLinkPlatformTests
    {
#if UNITY_EDITOR_WIN
        private sealed class ProtocolE2EAsset : ScriptableObject
        {
        }

        [Test]
        public void WindowsStatusCommand_RunsFromResolvedPackage()
        {
            ProtocolCommandResult result = ProtocolHandler.Status("unity-object-link-status-test");
            Assert.That(result.Succeeded, Is.True, result.Output);
            Assert.That(result.Output, Does.Contain("STATUS=not-registered"));
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator WindowsProtocol_RoundTripsFromOsActivationToUnitySelection()
        {
            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            string originalScheme = settings.Scheme;
            string originalProjectId = settings.ProjectId;
            string originalPreviousScheme = settings.PreviousScheme;
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
            string scheme = "uol-e2e-" + suffix;
            string projectId = "e2e-" + suffix;
            string folderPath = "Assets/UnityObjectLinkProtocolE2E-" + suffix;
            string assetPath = folderPath + "/Target.asset";
            bool installAttempted = false;

            try
            {
                string error;
                Assert.That(settings.TryUpdate(scheme, projectId, out error), Is.True, error);
                UnityObjectLinkReceiverService.Restart();

                Assert.That(AssetDatabase.CreateFolder("Assets", "UnityObjectLinkProtocolE2E-" + suffix), Is.Not.Empty);
                var target = ScriptableObject.CreateInstance<ProtocolE2EAsset>();
                target.name = "Protocol E2E Target";
                AssetDatabase.CreateAsset(target, assetPath);
                AssetDatabase.SaveAssets();

                string uri;
                Assert.That(UnityObjectLinkApi.TryCreateLink(target, out uri, out error), Is.True, error);

                installAttempted = true;
                ProtocolCommandResult installResult = ProtocolHandler.Install(scheme);
                Assert.That(installResult.Succeeded, Is.True, installResult.Output);

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                };
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
                {
                }

                double deadline = EditorApplication.timeSinceStartup + 10.0;
                while (Selection.activeObject != target && EditorApplication.timeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(Selection.activeObject, Is.SameAs(target), "The running Unity Editor did not select the linked object within 10 seconds.");
                Assert.That(UnityObjectLinkReceiverService.LastReceiveState, Does.Contain("Success"));
                Assert.That(UnityObjectLinkReceiverService.PendingRequestCount, Is.Zero);
            }
            finally
            {
                if (installAttempted)
                {
                    ProtocolHandler.Uninstall(scheme);
                }

                Selection.activeObject = null;
                AssetDatabase.DeleteAsset(folderPath);
                settings.RestoreSerializedState(originalScheme, originalProjectId, originalPreviousScheme);
                UnityObjectLinkReceiverService.Restart();
                RemoveTestInstanceDirectories(scheme, projectId);
            }
        }

        private static void RemoveTestInstanceDirectories(string scheme, string projectId)
        {
            UnityObjectLinkStorage storage = UnityObjectLinkStorage.CreateDefault();
            string instance = storage.GetInstanceDirectory(scheme, projectId);
            if (Directory.Exists(instance))
            {
                Directory.Delete(instance, true);
            }

            string schemeDirectory = Path.GetDirectoryName(instance);
            if (!string.IsNullOrEmpty(schemeDirectory) &&
                Directory.Exists(schemeDirectory) &&
                Directory.GetFileSystemEntries(schemeDirectory).Length == 0)
            {
                Directory.Delete(schemeDirectory);
            }
        }
#endif
    }
}
