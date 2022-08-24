using Framework.Canal;
using Framework.CanalToEasyNetQ.Application.Services;
using Framework.Core.Configurations;
using Framework.Json;
using Framework.Serilog;
using Framework.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Topshelf;

namespace Framework.CanalToEasyNetQ.Application
{
    internal class Program
    {
        static Program()
        {
            new ServiceCollection()
                .UseCore()
                .UseSerilog()
                .UseJson()
                .UseTiming()
                .UseCanal()
                .UseApplication()
                .LoadModules();
        }

        static void Main(string[] args)
        {
            HostFactory.Run(configurator =>
            {
                configurator.Service<CoreService>(callBack =>
                {
                    callBack.ConstructUsing(hostSettings =>
                    {
                        return ApplicationConfiguration.Current.Provider.GetRequiredService<CoreService>();
                    });
                    callBack.WhenStarted(tc => tc.Start());
                    callBack.WhenStopped(tc => tc.Stop());
                });
                configurator.RunAsLocalSystem();
                configurator.SetDescription("CanalToEasyNetQ");
                configurator.SetDisplayName("CanalToEasyNetQ");
                configurator.SetServiceName("CanalToEasyNetQ");
                configurator.OnException(exception =>
                {
                    ApplicationConfiguration.Current.Provider.GetRequiredService<ILogger<Program>>().LogError("业务执行异常,异常信息如下:" + exception.Message + "堆栈信息如下:" + exception.StackTrace);
                });
            });
        }
    }
}