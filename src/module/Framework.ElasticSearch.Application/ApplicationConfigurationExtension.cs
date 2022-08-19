using Framework.Core.Configurations;
using System.Reflection;

namespace Framework.ElasticSearch.Application
{
    public static class ApplicationConfigurationExtension
    {
        public static ApplicationConfiguration UseApplication(this ApplicationConfiguration application)
        {
            application.AddModule(Assembly.GetExecutingAssembly().FullName);
            return application;
        }
    }
}
