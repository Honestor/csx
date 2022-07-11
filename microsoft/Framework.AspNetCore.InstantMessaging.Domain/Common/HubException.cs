using System;
using System.Runtime.Serialization;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    [Serializable]
    public class HubException : Exception
    {
        public HubException()
        {
        }

        public HubException(string? message) : base(message)
        {
        }

        public HubException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public HubException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
