using Microsoft.Extensions.DependencyInjection;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal class InstantMessagingBuilder : IInstantMessagingServerBuilder
    {
        public InstantMessagingBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
