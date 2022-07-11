using Framework.Core.Configurations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Framework.AspNetCore.SignalR
{
    /// <summary>
    /// 链式配置
    /// </summary>
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用AspNetCore SignalR模块 默认开启messagepack协议 关于SignalR的扩展配置都写在当前层
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseAspNetCoreSignalR(this ApplicationConfiguration configuration)
        {
            configuration.AddModule(Assembly.GetExecutingAssembly().FullName);
            configuration.Container.AddConnections();
            configuration.Container.AddSignalR()
            .AddMessagePackProtocol();
            return configuration;
        }
    }
}
