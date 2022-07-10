using System;
using System.Collections.Generic;
using Consul;
using Microsoft.Extensions.Options;

namespace Framework.Consul.Client
{
    public static class ConsulOptionsHelper
    {
        public static OptionsMonitor<ConsulClientConfiguration> CreateOptionsMonitor(
            IEnumerable<IConfigureOptions<ConsulClientConfiguration>> configureOptions)
        {
            return new OptionsMonitor<ConsulClientConfiguration>(
                new OptionsFactory<ConsulClientConfiguration>(
                    configureOptions,
                    Array.Empty<IPostConfigureOptions<ConsulClientConfiguration>>(),
                    Array.Empty<IValidateOptions<ConsulClientConfiguration>>()),
                Array.Empty<IOptionsChangeTokenSource<ConsulClientConfiguration>>(),
                new OptionsCache<ConsulClientConfiguration>());
        }
    }
}
