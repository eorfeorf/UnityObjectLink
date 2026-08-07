using System;
using UnityEditor;
using UnityEngine;

namespace UnityObjectLink
{
    internal static class UnityObjectLinkResolver
    {
        internal static UnityObjectLinkResult Handle(string rawUri)
        {
            return Handle(rawUri, UnityEditorObjectSelectionService.Instance, true);
        }

        internal static UnityObjectLinkResult Handle(string rawUri, IUnityObjectLinkSelectionService selectionService, bool showNotification)
        {
            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            UnityObjectLinkUri link;
            string error;
            if (!UnityObjectLinkUri.TryParse(rawUri, settings.Scheme, settings.ProjectId, out link, out error))
            {
                UnityObjectLinkStatus status = error != null && error.IndexOf("different Unity project", StringComparison.OrdinalIgnoreCase) >= 0
                    ? UnityObjectLinkStatus.WrongProject
                    : UnityObjectLinkStatus.InvalidUri;
                return Finish(new UnityObjectLinkResult(status, error, rawUri, null), showNotification);
            }

            GlobalObjectId globalObjectId;
            if (!GlobalObjectId.TryParse(link.GlobalObjectId, out globalObjectId))
            {
                return Finish(new UnityObjectLinkResult(UnityObjectLinkStatus.InvalidUri, "Unity rejected the GlobalObjectId in the link.", rawUri, null), showNotification);
            }

            UnityEngine.Object target;
            try
            {
                target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            }
            catch (Exception exception)
            {
                return Finish(new UnityObjectLinkResult(UnityObjectLinkStatus.InternalError, "Unity failed while resolving the object: " + exception.Message, rawUri, null), showNotification);
            }

            if (target == null)
            {
                return Finish(new UnityObjectLinkResult(
                    UnityObjectLinkStatus.ObjectNotFound,
                    "Object not found. A Scene object may be deleted or its saved Scene may not be loaded.",
                    rawUri,
                    null), showNotification);
            }

            selectionService.SelectAndPing(target);
            return Finish(new UnityObjectLinkResult(UnityObjectLinkStatus.Success, "Selected " + target.name + ".", rawUri, target), showNotification);
        }

        private static UnityObjectLinkResult Finish(UnityObjectLinkResult result, bool showNotification)
        {
            if (showNotification)
            {
                UnityObjectLinkNotifications.Show(result);
            }

            return result;
        }
    }
}
