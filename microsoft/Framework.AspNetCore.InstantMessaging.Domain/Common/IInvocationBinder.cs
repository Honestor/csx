using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IInvocationBinder
    {
        IReadOnlyList<Type> GetParameterTypes(string methodName);
    }
}
