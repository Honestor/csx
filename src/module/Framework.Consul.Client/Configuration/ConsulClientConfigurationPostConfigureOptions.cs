using Consul;
using Microsoft.Extensions.Options;
using System;

namespace Framework.Consul.Client
{
    public class ConsulClientConfigurationPostConfigureOptions : IPostConfigureOptions<ConsulClientConfiguration>
    {
        private readonly ConsulClientOptions _consulClientOptions;

        public ConsulClientConfigurationPostConfigureOptions(IOptions<ConsulClientOptions> options)
        {
            _consulClientOptions = options.Value;
        }

        public void PostConfigure(string name, ConsulClientConfiguration options)
        {
            options.Address = new Uri(_consulClientOptions.ConsulAddress);
            options.WaitTime = TimeSpan.FromSeconds(10);
        }
    }
}
