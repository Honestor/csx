using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public static class InstantMessagingDependencyInjectionExtensions
    {
        public static IInstantMessagingServerBuilder AddInstantMessagingCore(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.TryAddSingleton(typeof(IHubProtocolResolver), typeof(DefaultHubProtocolResolver));
            services.TryAddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.TryAddSingleton(typeof(IHubContext<,>), typeof(HubContext<,>));
            services.TryAddSingleton(typeof(HubConnectionHandler<>), typeof(HubConnectionHandler<>));
            services.TryAddSingleton(typeof(IUserIdProvider), typeof(DefaultUserIdProvider));
            services.TryAddSingleton(typeof(HubDispatcher<>), typeof(DefaultHubDispatcher<>));
            services.TryAddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));
            var builder = new InstantMessagingBuilder(services);
            return builder;
        }
    }
}
