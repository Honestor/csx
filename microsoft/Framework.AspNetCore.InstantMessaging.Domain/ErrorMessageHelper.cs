using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal static class ErrorMessageHelper
    {
        internal static string BuildErrorMessage(string message, Exception exception, bool includeExceptionDetails)
        {
            if (exception is HubException || includeExceptionDetails)
            {
                return $"{message} {exception.GetType().Name}: {exception.Message}";

            }
            return message;
        }
    }
}
