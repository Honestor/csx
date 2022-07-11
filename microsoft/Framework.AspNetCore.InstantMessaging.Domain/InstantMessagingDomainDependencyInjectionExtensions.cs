
using Framework.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.AspNetCore.InstantMessaging
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class InstantMessagingDomainDependencyInjectionExtensions
    {
        /// <summary>
        /// 启用即时通信模块Domain模块
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection UseInstantMessagingDomain(this IServiceCollection services)
        {
            services.AddConnections();
            return services;
        }
    }
}
