using Framework.Core.Configurations;
using Framework.Core.Dependency;
using Framework.ElasticSearch.Nest.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Framework.ElasticSearch.Nest
{
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启动 Nest组件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseNest(this ApplicationConfiguration application)
        {
            application
                .ConfigModule()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }

        /// <summary>
        /// 配置模块
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration ConfigModule(this ApplicationConfiguration application)
        {
            application.Container.Configure<NestOptions>(application.Container.GetConfiguration().GetSection(nameof(NestOptions)));
            application.Container.TryAdd(ServiceDescriptor.Singleton(typeof(IndexingManager<>), typeof(IndexingManager<>)));
            return application;
        }
    }
}
