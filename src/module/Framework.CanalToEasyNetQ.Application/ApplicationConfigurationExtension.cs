using Framework.Canal.EasyNetQ.Configurations;
using Framework.Core.Configurations;
using Framework.Core.Dependency;
using Framework.Json;
using Framework.Serilog;
using Framework.Timing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Framework.CanalToEasyNetQ.Application
{
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用Canal To EasyNetQ 组件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseApplication(this ApplicationConfiguration application)
        {
            application
                .ConfigModule()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }

        public static ApplicationConfiguration ConfigModule(this ApplicationConfiguration application)
        {
            //配置
            application.Container.Configure<JsonOptions>(config =>
            {
                config.ExtraDateTimeFormats.Add("yyyy-MM-dd HH:mm:ss.ffffff");
            });

            application.Container.Configure<CanalToEasyNetQOptions>(application.Container.GetConfiguration().GetSection(nameof(CanalToEasyNetQOptions)));
            return application;
        }
    }
}
