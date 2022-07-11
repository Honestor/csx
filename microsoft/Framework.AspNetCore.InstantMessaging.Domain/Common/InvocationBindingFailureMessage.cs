using System.Runtime.ExceptionServices;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class InvocationBindingFailureMessage : HubInvocationMessage
    {
        public ExceptionDispatchInfo BindingFailure { get; }

        public string Target { get; }

        public InvocationBindingFailureMessage(string invocationId, string target, ExceptionDispatchInfo bindingFailure) : base(invocationId)
        {
            Target = target;
            BindingFailure = bindingFailure;
        }
    }
}
