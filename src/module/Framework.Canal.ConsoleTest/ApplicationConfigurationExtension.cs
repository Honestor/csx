using Framework.Core.Configurations;
using Framework.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Framework.Canal.ConsoleTest
{
    public static class ApplicationConfigurationExtension
    {
        public static ApplicationConfiguration UseApplication(this ApplicationConfiguration application)
        {
            application
                .ConfigModule()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }

        public static ApplicationConfiguration ConfigModule(this ApplicationConfiguration application)
        {
            application.Container.Configure<JsonOptions>(config =>
            {
                config.ExtraDateTimeFormats.Add("yyyy-MM-dd HH:mm:ss.ffffff");
            });
            return application;
        }
    }
}
