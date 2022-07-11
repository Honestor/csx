using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class CompletionMessage : HubInvocationMessage
    {
        public string? Error { get; }

        public object? Result { get; }

        public bool HasResult { get; }

        public CompletionMessage(string invocationId, string? error, object? result, bool hasResult)
            : base(invocationId)
        {
            if (error != null && result != null)
            {
                throw new ArgumentException($"Expected either '{nameof(error)}' or '{nameof(result)}' to be provided, but not both");
            }

            Error = error;
            Result = result;
            HasResult = hasResult;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var errorStr = Error == null ? "<<null>>" : $"\"{Error}\"";
            var resultField = HasResult ? $", {nameof(Result)}: {Result ?? "<<null>>"}" : string.Empty;
            return $"Completion {{ {nameof(InvocationId)}: \"{InvocationId}\", {nameof(Error)}: {errorStr}{resultField} }}";
        }

        public static CompletionMessage WithError(string invocationId, string error)
            => new CompletionMessage(invocationId, error, result: null, hasResult: false);

        public static CompletionMessage WithResult(string invocationId, object payload)
            => new CompletionMessage(invocationId, error: null, result: payload, hasResult: true);

        public static CompletionMessage Empty(string invocationId)
            => new CompletionMessage(invocationId, error: null, result: null, hasResult: false);
    }
}
