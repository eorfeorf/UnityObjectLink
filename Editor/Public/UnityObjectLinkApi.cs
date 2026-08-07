using System;
using UnityEditor;
using UnityEngine;

namespace UnityObjectLink
{
    public static class UnityObjectLinkApi
    {
        public static event Action<UnityObjectLinkResult> LinkHandled;

        public static bool TryCreateLink(UnityEngine.Object target, out string uri, out string error)
        {
            uri = null;
            error = null;

            if (target == null)
            {
                error = "No Unity object is selected.";
                return false;
            }

            if (!EditorUtility.IsPersistent(target))
            {
                var component = target as Component;
                var gameObject = target as GameObject;
                GameObject sceneObject = component != null ? component.gameObject : gameObject;
                if (sceneObject == null ||
                    !sceneObject.scene.IsValid() ||
                    string.IsNullOrEmpty(sceneObject.scene.path) ||
                    sceneObject.scene.isDirty)
                {
                    error = "The selected object is temporary or belongs to an unsaved Scene or a Scene with unsaved changes.";
                    return false;
                }
            }

            GlobalObjectId globalObjectId;
            try
            {
                globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(target);
            }
            catch (Exception exception)
            {
                error = "Unity could not create a GlobalObjectId: " + exception.Message;
                return false;
            }

            if (globalObjectId.Equals(default(GlobalObjectId)))
            {
                error = "Unity did not assign a persistent GlobalObjectId to the selected object.";
                return false;
            }

            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            UnityObjectLinkUri link;
            if (!UnityObjectLinkUri.TryCreate(settings.Scheme, settings.ProjectId, globalObjectId.ToString(), out link, out error))
            {
                return false;
            }

            uri = link.ToString();
            return true;
        }

        public static UnityObjectLinkResult HandleLink(string uri)
        {
            UnityObjectLinkResult result = UnityObjectLinkResolver.Handle(uri);
            RaiseLinkHandled(result);
            return result;
        }

        internal static void RaiseLinkHandled(UnityObjectLinkResult result)
        {
            Action<UnityObjectLinkResult> handler = LinkHandled;
            if (handler == null)
            {
                return;
            }

            foreach (Action<UnityObjectLinkResult> listener in handler.GetInvocationList())
            {
                try
                {
                    listener(result);
                }
                catch (Exception exception)
                {
                    Debug.LogError(UnityObjectLinkNotifications.LogPrefix + "A LinkHandled listener failed: " + exception);
                }
            }
        }
    }
}
