using Framework.Core.Configurations;
using Framework.Core.Data;
using System.Reflection;

namespace Framework.Data.MySql
{
    /// <summary>
    /// 链式配置
    /// </summary>
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 启用MySql模块
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseMySql(this ApplicationConfiguration configuration)
        {
            configuration
                .UseData()
                .AddModule(Assembly.GetExecutingAssembly().FullName);
            return configuration;
        }
    }
}
