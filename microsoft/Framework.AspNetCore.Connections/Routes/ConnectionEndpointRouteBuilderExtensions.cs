using Framework.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Framework.AspNetCore.Connections
{
    public static class ConnectionEndpointRouteBuilderExtensions
    {
        public static ConnectionEndpointRouteBuilder MapConnections(this IEndpointRouteBuilder endpoints, string pattern, Action<IConnectionBuilder> configure) =>
          endpoints.MapConnections(pattern, new HttpConnectionDispatcherOptions(), configure);

        public static ConnectionEndpointRouteBuilder MapConnections(this IEndpointRouteBuilder endpoints, string pattern, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure)
        {
            var dispatcher = endpoints.ServiceProvider.GetRequiredService<HttpConnectionDispatcher>();
            var connectionBuilder = new ConnectionBuilder(endpoints.ServiceProvider);
            configure(connectionBuilder);
            var connectionDelegate = connectionBuilder.Build();

            var conventionBuilders = new List<IEndpointConventionBuilder>();
            var app = endpoints.CreateApplicationBuilder();
            app.UseWebSockets();
            app.Run(c => dispatcher.ExecuteNegotiateAsync(c, options));
            var negotiateHandler = app.Build();

            var negotiateBuilder = endpoints.Map(pattern + "/negotiate", negotiateHandler);
            conventionBuilders.Add(negotiateBuilder);
            negotiateBuilder.WithMetadata(new NegotiateMetadata());

            app = endpoints.CreateApplicationBuilder();
            app.UseWebSockets();
            app.Run(c => dispatcher.ExecuteAsync(c, options, connectionDelegate));
            var executehandler = app.Build();

            var executeBuilder = endpoints.Map(pattern, executehandler);
            conventionBuilders.Add(executeBuilder);

            var compositeConventionBuilder = new CompositeEndpointConventionBuilder(conventionBuilders);

            compositeConventionBuilder.Add(e =>
            {
                foreach (var data in options.AuthorizationData)
                {
                    e.Metadata.Add(data);
                }
            });

            return new ConnectionEndpointRouteBuilder(compositeConventionBuilder);
        }
    }
}
