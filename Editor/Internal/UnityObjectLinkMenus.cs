using UnityEditor;
using UnityEngine;

namespace UnityObjectLink
{
    internal static class UnityObjectLinkMenus
    {
        private const string ToolsMenu = "Tools/Unity Object Link/Copy Link for Active Selection";

        [MenuItem(ToolsMenu, false, 100)]
        [MenuItem("Assets/Copy Unity Object Link", false, 2000)]
        [MenuItem("GameObject/Copy Unity Object Link", false, 49)]
        private static void CopyActiveSelection()
        {
            string uri;
            string error;
            if (!UnityObjectLinkApi.TryCreateLink(Selection.activeObject, out uri, out error))
            {
                UnityObjectLinkNotifications.Show(new UnityObjectLinkResult(UnityObjectLinkStatus.InvalidTarget, error, null, Selection.activeObject));
                return;
            }

            GUIUtility.systemCopyBuffer = uri;
            UnityObjectLinkNotifications.Show(new UnityObjectLinkResult(UnityObjectLinkStatus.Success, "Copied Unity Object Link.", uri, Selection.activeObject));
        }

        [MenuItem(ToolsMenu, true)]
        [MenuItem("Assets/Copy Unity Object Link", true)]
        [MenuItem("GameObject/Copy Unity Object Link", true)]
        private static bool CanCopyActiveSelection()
        {
            return Selection.activeObject != null;
        }
    }
}
