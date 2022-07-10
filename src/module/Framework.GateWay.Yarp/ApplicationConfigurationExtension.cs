using Framework.Core.Configurations;
using Framework.Core.Dependency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.GateWay.Yarp
{
    /// <summary>
    /// 链式配置
    /// </summary>
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用Yarp应用模块 注意:默认在appsettings.json文件的ReverseProxy节点配置代理信息
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseYarp(this ApplicationConfiguration configuration)
        {
            configuration
                .AddYarp()
                .ConfigYarp();
            return configuration;
        }

        public static ApplicationConfiguration AddYarp(this ApplicationConfiguration configuration)
        {
            configuration.Container
              .AddReverseProxy()
              .LoadFromConfig(configuration.Container.GetConfiguration().GetSection("ReverseProxy"));
            return configuration;
        }

        /// <summary>
        /// 配置Yarp模块
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration ConfigYarp(this ApplicationConfiguration application)
        {
            //配置路由
            application.Container.Configure<EndpointRouterOptions>(options =>
            {
                options.EndpointConfigureActions.Add(routeConext =>
                {
                    routeConext.Endpoints.MapReverseProxy();
                });
            });
            return application;
        }
    }

}
