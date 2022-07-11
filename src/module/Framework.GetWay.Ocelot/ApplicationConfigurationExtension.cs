using Framework.Core.Configurations;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Consul;
using System.Reflection;

namespace Framework.GetWay.Ocelot
{
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用Ocelot、Consul组件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns> vfbibu fxu[
        public static ApplicationConfiguration UseOcelotWithConsul(this ApplicationConfiguration application)
        {
            application
                .AddOcelotWithConsul()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }

        /// <summary>
        /// 集成Ocelot、Consul组件
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static ApplicationConfiguration AddOcelotWithConsul(this ApplicationConfiguration application)
        {
            application.Container
                .AddOcelot()
                .AddConsul();
            return application;
        }
    }
}
