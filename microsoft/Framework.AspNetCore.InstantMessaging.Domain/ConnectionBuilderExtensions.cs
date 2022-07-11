using Framework.AspNetCore.Connections.Abstractions;
using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseHub<THub>(this IConnectionBuilder connectionBuilder) where THub : Hub
        {
            return connectionBuilder.UseConnectionHandler<HubConnectionHandler<THub>>();
        }
    }
}
