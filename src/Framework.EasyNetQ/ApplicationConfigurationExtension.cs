using Framework.Core.Configurations;
using Framework.Core.Dependency;
using Framework.EasyNetQ.Configurations;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Framework.EasyNetQ
{
    /// <summary>
    /// 链式配置
    /// </summary>
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用EasyNetQ模块
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseEasyNetQ(this ApplicationConfiguration configuration)
        {
            configuration
                .ConfigModule()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return configuration;
        }

        /// <summary>
        /// 配置
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration ConfigModule(this ApplicationConfiguration application)
        {
            application.Container.Configure<EasyNetQOptions>(application.Container.GetConfiguration().GetSection(nameof(EasyNetQOptions)));
            return application;
        }
    }
}
