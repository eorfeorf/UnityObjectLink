using UnityEngine;

namespace UnityObjectLink
{
    public enum UnityObjectLinkStatus
    {
        Success,
        InvalidTarget,
        InvalidUri,
        WrongProject,
        ObjectNotFound,
        InternalError
    }

    public sealed class UnityObjectLinkResult
    {
        public UnityObjectLinkStatus Status { get; private set; }
        public bool Succeeded { get { return Status == UnityObjectLinkStatus.Success; } }
        public string Message { get; private set; }
        public string Uri { get; private set; }
        public Object Target { get; private set; }

        internal UnityObjectLinkResult(UnityObjectLinkStatus status, string message, string uri, Object target)
        {
            Status = status;
            Message = message;
            Uri = uri;
            Target = target;
        }
    }
}
