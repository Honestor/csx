using Consul;
using Microsoft.Extensions.Options;
using System;

namespace Framework.Consul.Client
{
    public class AgentServiceRegistrationPostConfigureOptions : IPostConfigureOptions<AgentServiceRegistration>
    {
        private readonly ConsulClientOptions _consulClientOptions;

        public AgentServiceRegistrationPostConfigureOptions(IOptions<ConsulClientOptions> options)
        {
            _consulClientOptions = options.Value;
        }

        public void PostConfigure(string name, AgentServiceRegistration options)
        {
            var ip = _consulClientOptions.ServiceIp;
            var port = _consulClientOptions.ServicePort;
            options.ID = Guid.NewGuid().ToString();
            options.Address = ip;
            options.Name = _consulClientOptions.ServiceName;
            options.Port = port;
            options.Check = new AgentServiceCheck()
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),//服务启动后多久注册服务
                Interval = TimeSpan.FromSeconds(10),//健康检查时间间隔
                HTTP = $"http://{ip}:{port}/healthcheck",//健康检查地址
                Timeout = TimeSpan.FromSeconds(5)//超时时间
            };
        }
    }
}
