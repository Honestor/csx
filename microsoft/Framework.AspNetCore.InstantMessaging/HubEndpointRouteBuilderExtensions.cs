using Framework.AspNetCore.Connections;
using Framework.AspNetCore.InstantMessaging.Domain;
using Microsoft.AspNetCore.Routing;
using System;

namespace Framework.AspNetCore.InstantMessaging.Application
{
    public static class HubEndpointRouteBuilderExtensions
    {
        public static HubEndpointConventionBuilder MapHubs<THub>(this IEndpointRouteBuilder endpoints, string pattern) where THub : Hub
        {
            return endpoints.MapHub<THub>(pattern, configureOptions: null);
        }

        public static HubEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder endpoints, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where THub : Hub
        {
            var options = new HttpConnectionDispatcherOptions();
            configureOptions?.Invoke(options);
            var conventionBuilder = endpoints.MapConnections(pattern, options, builder =>
            {
                builder.UseHub<THub>();
            });
            var attributes = typeof(THub).GetCustomAttributes(inherit: true);
            conventionBuilder.Add(e =>
            {
                foreach (var item in attributes)
                {
                    e.Metadata.Add(item);
                }

                e.Metadata.Add(new HubMetadata(typeof(THub)));
            });

            return new HubEndpointConventionBuilder(conventionBuilder);
        }
    }
}
