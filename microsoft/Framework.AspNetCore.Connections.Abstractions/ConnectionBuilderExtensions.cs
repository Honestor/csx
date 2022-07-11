using System;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections.Abstractions
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseConnectionHandler<TConnectionHandler>(this IConnectionBuilder connectionBuilder) where TConnectionHandler : ConnectionHandler
        {
            var handler = ActivatorUtilities.GetServiceOrCreateInstance<TConnectionHandler>(connectionBuilder.ApplicationServices);
            return connectionBuilder.Run(connection => handler.OnConnectedAsync(connection));
        }

        public static IConnectionBuilder Run(this IConnectionBuilder connectionBuilder, Func<ConnectionContext, Task> middleware)
        {
            return connectionBuilder.Use(next =>
            {
                return context =>
                {
                    return middleware(context);
                };
            });
        }
    }
}
