using UnityEditor;
using UnityEngine;

namespace UnityObjectLink.Samples
{
    internal static class BasicUsageExample
    {
        [MenuItem("Tools/Unity Object Link Samples/Copy Link for Active Object")]
        private static void CopyLink()
        {
            string uri;
            string error;
            if (!UnityObjectLinkApi.TryCreateLink(Selection.activeObject, out uri, out error))
            {
                EditorUtility.DisplayDialog("Unity Object Link", error, "OK");
                return;
            }

            GUIUtility.systemCopyBuffer = uri;
            Debug.Log("Copied: " + uri);
        }

        [MenuItem("Tools/Unity Object Link Samples/Copy Link for Active Object", true)]
        private static bool CanCopyLink()
        {
            return Selection.activeObject != null;
        }
    }
}
