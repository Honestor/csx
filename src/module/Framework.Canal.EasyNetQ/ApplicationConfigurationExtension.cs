using Framework.Core.Configurations;
using Framework.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Framework.Canal.ConsoleTest
{
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用Canal with EasyNetQ 组件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseCanalEasyNetQ(this ApplicationConfiguration application)
        {
            application
                .UseCanal()
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
            return application;
        }
    }
}
