using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;

namespace Framework.AspNetCore.Connections
{
    public static class ConnectionsDependencyInjectionExtensions
    {
        public static IServiceCollection AddHttpConnections(this IServiceCollection services)
        {
            services.AddRouting();
            services.AddAuthorization();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ConnectionOptions>, ConnectionOptionsSetup>());
            services.TryAddSingleton<HttpConnectionDispatcher>();
            services.TryAddSingleton<HttpConnectionManager>();
            return services;
        }

        public static IServiceCollection AddHttpConnections(this IServiceCollection services, Action<ConnectionOptions> options)
        {
            return services.Configure(options)
                .AddHttpConnections();
        }
    }
}
