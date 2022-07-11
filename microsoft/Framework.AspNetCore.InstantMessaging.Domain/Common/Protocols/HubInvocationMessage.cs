using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public abstract class HubInvocationMessage : HubMessage
    {
        public IDictionary<string, string>? Headers { get; set; }

        public string? InvocationId { get; }

        protected HubInvocationMessage(string? invocationId)
        {
            InvocationId = invocationId;
        }
    }
}
