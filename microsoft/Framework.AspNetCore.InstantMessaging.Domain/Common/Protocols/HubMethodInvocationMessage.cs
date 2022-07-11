using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public abstract class HubMethodInvocationMessage : HubInvocationMessage
    {
        public string Target { get; }

        public object?[]? Arguments { get; }

        public string[]? StreamIds { get; }

        protected HubMethodInvocationMessage(string? invocationId, string target, object?[]? arguments) : base(invocationId)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException(nameof(target));
            }

            Target = target;
            Arguments = arguments;
        }

        protected HubMethodInvocationMessage(string? invocationId, string target, object?[]? arguments, string[]? streamIds): this(invocationId, target, arguments)
        {
            StreamIds = streamIds;
        }
    }

    public class InvocationMessage : HubMethodInvocationMessage
    {
        public InvocationMessage(string target, object?[]? arguments) : this(null, target, arguments)
        {

        }

        public InvocationMessage(string? invocationId, string target, object?[]? arguments) : base(invocationId, target, arguments)
        {

        }

        public InvocationMessage(string? invocationId, string target, object?[]? arguments, string[]? streamIds)
          : base(invocationId, target, arguments, streamIds)
        {

        }
    }
}
