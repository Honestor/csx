using Framework.Core.Configurations;
using Framework.Core.Dependency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Framework.Canal
{
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用 Canal
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseCanal(this ApplicationConfiguration application)
        {
            application
                .ConfigModule()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }

        /// <summary>
        /// 配置
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration ConfigModule(this ApplicationConfiguration application)
        {
            application.Container.Configure<CanalOptions>(application.Container.GetConfiguration().GetSection(nameof(CanalOptions)));
            application.Container.TryAdd(ServiceDescriptor.Singleton(typeof(CanalConsumer<>), typeof(CanalConsumer<>)));
            return application;
        }
    }
}
