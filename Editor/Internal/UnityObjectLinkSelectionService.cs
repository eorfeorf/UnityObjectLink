using UnityEditor;
using UnityEngine;

namespace UnityObjectLink
{
    internal interface IUnityObjectLinkSelectionService
    {
        void SelectAndPing(Object target);
    }

    internal sealed class UnityEditorObjectSelectionService : IUnityObjectLinkSelectionService
    {
        internal static readonly UnityEditorObjectSelectionService Instance = new UnityEditorObjectSelectionService();

        private UnityEditorObjectSelectionService()
        {
        }

        public void SelectAndPing(Object target)
        {
            Selection.activeObject = target;
            if (EditorUtility.IsPersistent(target))
            {
                EditorUtility.FocusProjectWindow();
            }
            else if (!EditorApplication.ExecuteMenuItem("Window/General/Hierarchy"))
            {
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    sceneView.Focus();
                }
            }

            EditorGUIUtility.PingObject(target);
        }
    }
}
