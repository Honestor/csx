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
        public static ApplicationConfiguration UseApplication(this ApplicationConfiguration application)
        {
            application
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }
    }
}
