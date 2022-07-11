using System;
using Framework.AspNetCore.InstantMessaging.Domain;
using Framework.AspNetCore.InstantMessaging.Protocols.MessagePack;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessagePackProtocolDependencyInjectionExtensions
    {
        public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder) where TBuilder : IInstantMessagingBuilder
            => AddMessagePackProtocol(builder, _ => { });

        public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder, Action<MessagePackHubProtocolOptions> configure) where TBuilder : IInstantMessagingBuilder
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>());
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
