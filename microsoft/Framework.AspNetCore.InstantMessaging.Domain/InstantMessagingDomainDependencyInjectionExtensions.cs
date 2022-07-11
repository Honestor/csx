
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
        /// ���ü�ʱͨ��ģ��Domainģ��
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
