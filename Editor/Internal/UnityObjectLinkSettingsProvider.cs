using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityObjectLink
{
    internal sealed class UnityObjectLinkSettingsProvider : SettingsProvider
    {
        private string scheme;
        private string projectId;
        private string status = "Not checked";

        private UnityObjectLinkSettingsProvider(string path, SettingsScope scope) : base(path, scope)
        {
            keywords = new[] { "Unity", "Object", "Link", "URI", "Scheme", "Project ID", "Protocol" };
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new UnityObjectLinkSettingsProvider("Project/Unity Object Link", SettingsScope.Project);
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            scheme = settings.Scheme;
            projectId = settings.ProjectId;
            RefreshStatus();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Link identity", EditorStyles.boldLabel);
            scheme = EditorGUILayout.TextField(new GUIContent("URI Scheme", "RFC 3986 scheme used by generated links."), scheme);
            projectId = EditorGUILayout.TextField(new GUIContent("Project ID", "Stable routing identifier shared by this Unity project."), projectId);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Settings", GUILayout.Width(130)))
                {
                    string error;
                    if (UnityObjectLinkSettings.instance.TryUpdate(scheme, projectId, out error))
                    {
                        UnityObjectLinkReceiverService.Restart();
                        status = "Settings saved. Register the protocol if the scheme changed.";
                    }
                    else
                    {
                        status = error;
                    }
                }

                if (GUILayout.Button("Regenerate Project ID", GUILayout.Width(160)))
                {
                    projectId = "project-" + Guid.NewGuid().ToString("N").Substring(0, 12);
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Protocol handler", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(status, MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Register"))
                {
                    RunAndShow(ProtocolHandler.Install(UnityObjectLinkSettings.instance.Scheme));
                }

                if (GUILayout.Button("Unregister"))
                {
                    RunAndShow(ProtocolHandler.Uninstall(UnityObjectLinkSettings.instance.Scheme));
                }

                if (GUILayout.Button("Refresh Status"))
                {
                    RefreshStatus();
                }
            }

            string previousScheme = UnityObjectLinkSettings.instance.PreviousScheme;
            if (!string.IsNullOrEmpty(previousScheme) && !string.Equals(previousScheme, UnityObjectLinkSettings.instance.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The previous scheme '" + previousScheme + "' may still be registered.", MessageType.Warning);
                if (GUILayout.Button("Unregister Previous Scheme"))
                {
                    ProtocolCommandResult result = ProtocolHandler.Uninstall(previousScheme);
                    RunAndShow(result);
                    if (result.Succeeded)
                    {
                        UnityObjectLinkSettings.instance.ClearPreviousScheme(previousScheme);
                    }
                }
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Local receiver", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel("Heartbeat: " + UnityObjectLinkReceiverService.HeartbeatPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel("Inbox: " + UnityObjectLinkReceiverService.InboxPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            bool fresh = UnityObjectLinkHeartbeat.IsFresh(UnityObjectLinkReceiverService.HeartbeatPath, DateTime.UtcNow);
            EditorGUILayout.LabelField("Heartbeat state", fresh ? "Active" : "Stale or missing");
            int pendingRequests = UnityObjectLinkReceiverService.PendingRequestCount;
            EditorGUILayout.LabelField("Pending requests", pendingRequests >= 0 ? pendingRequests.ToString() : "Unavailable");
            EditorGUILayout.LabelField("Last receive result", UnityObjectLinkReceiverService.LastReceiveState);
        }

        private void RefreshStatus()
        {
            RunAndShow(ProtocolHandler.Status(UnityObjectLinkSettings.instance.Scheme));
        }

        private void RunAndShow(ProtocolCommandResult result)
        {
            status = (result.Succeeded ? "Success: " : "Failed: ") + result.Output;
            Repaint();
        }
    }

    internal static class UnityObjectLinkProtocolMenus
    {
        [MenuItem("Tools/Unity Object Link/Project Settings...", false, 200)]
        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/Unity Object Link");
        }

        [MenuItem("Tools/Unity Object Link/Register Protocol Handler", false, 201)]
        private static void Register()
        {
            Show(ProtocolHandler.Install(UnityObjectLinkSettings.instance.Scheme));
        }

        [MenuItem("Tools/Unity Object Link/Unregister Protocol Handler", false, 202)]
        private static void Unregister()
        {
            Show(ProtocolHandler.Uninstall(UnityObjectLinkSettings.instance.Scheme));
        }

        private static void Show(ProtocolCommandResult result)
        {
            if (result.Succeeded)
            {
                EditorUtility.DisplayDialog("Unity Object Link", result.Output, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Unity Object Link", "Operation failed:\n" + result.Output, "OK");
            }
        }
    }
}
