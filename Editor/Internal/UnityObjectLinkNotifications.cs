using UnityEditor;
using UnityEngine;

namespace UnityObjectLink
{
    internal static class UnityObjectLinkNotifications
    {
        internal const string LogPrefix = "[UnityObjectLink] ";

        internal static void Show(UnityObjectLinkResult result)
        {
            if (result.Succeeded)
            {
                Debug.Log(LogPrefix + result.Message, result.Target);
            }
            else
            {
                Debug.LogWarning(LogPrefix + result.Message);
            }

            EditorWindow window = EditorWindow.focusedWindow;
            if (window != null)
            {
                window.ShowNotification(new GUIContent(result.Message));
            }
        }
    }
}
