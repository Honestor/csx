using System;
using Framework.AspNetCore.InstantMessaging.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Framework.AspNetCore.InstantMessaging.Protocols.Json
{
    public static class JsonProtocolDependencyInjectionExtensions
    {
        public static TBuilder AddJsonProtocol<TBuilder>(this TBuilder builder) where TBuilder : IInstantMessagingBuilder
            => AddJsonProtocol(builder, _ => { });

        public static TBuilder AddJsonProtocol<TBuilder>(this TBuilder builder, Action<JsonHubProtocolOptions> configure) where TBuilder : IInstantMessagingBuilder
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, JsonHubProtocol>());
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
