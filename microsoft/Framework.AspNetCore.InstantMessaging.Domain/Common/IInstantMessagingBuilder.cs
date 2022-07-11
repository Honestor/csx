using Microsoft.Extensions.DependencyInjection;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public interface IInstantMessagingBuilder
    {
        IServiceCollection Services { get; }
    }
}
