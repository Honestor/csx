using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class HubConnectionBinder<THub> : IInvocationBinder where THub : Hub
    {
        private HubDispatcher<THub> _dispatcher;
        private HubConnectionContext _connection;

        public HubConnectionBinder(HubDispatcher<THub> dispatcher, HubConnectionContext connection)
        {
            _dispatcher = dispatcher;
            _connection = connection;
        }

        public IReadOnlyList<Type> GetParameterTypes(string methodName)
        {
            return _dispatcher.GetParameterTypes(methodName);
        }
    }
}