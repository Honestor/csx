using Framework.Core.Configurations;
using Framework.Core.Dependency;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Framework.Core.Data
{
    /// <summary>
    /// 链式配置
    /// </summary>
    public static class ApplicationConfigurationExtension
    {
        /// <summary>
        /// 配置数据库连接字符串 提供连接数据库最基本的功能
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static ApplicationConfiguration UseData(this ApplicationConfiguration configuration)
        {
            configuration.AddModule(Assembly.GetExecutingAssembly().FullName);
            configuration.Container.Configure<DbOptions>(configuration.Container.GetConfiguration().GetSection(nameof(DbOptions)));
            return configuration;
        }
    }
}
