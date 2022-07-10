using Consul;
using Consul.AspNetCore;
using Framework.Core.Configurations;
using Framework.Core.Dependency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Framework.Consul.Client
{
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用Consul客户端组件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseConsulClient(this ApplicationConfiguration application)
        {
            application
                .ConfigConsulClient()
                .AddConsulClient()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }

        /// <summary>
        /// 集成Consul
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration AddConsulClient(this ApplicationConfiguration application)
        {
            application.Container
                .AddConsul()
                .AddHostedService<AgentServiceRegistrationHostedService>();
            return application;
        }


        /// <summary>
        /// 配置Consule客户端
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration ConfigConsulClient(this ApplicationConfiguration application)
        {
            application.Container.Configure<ConsulClientOptions>(application.Container.GetConfiguration().GetSection("ConsulClientOptions"));
            application.Container.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<AgentServiceRegistration>, AgentServiceRegistrationPostConfigureOptions>());
            application.Container.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<ConsulClientConfiguration>, ConsulClientConfigurationPostConfigureOptions>());
            return application;
        }
    }
}
