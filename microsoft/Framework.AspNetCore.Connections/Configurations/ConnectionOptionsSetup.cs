using System;
using Microsoft.Extensions.Options;

namespace Framework.AspNetCore.Connections
{
    public class ConnectionOptionsSetup : IConfigureOptions<ConnectionOptions>
    {
        public static TimeSpan DefaultDisconectTimeout = TimeSpan.FromSeconds(15);

        public void Configure(ConnectionOptions options)
        {
            if (options.DisconnectTimeout == null)
            {
                options.DisconnectTimeout = DefaultDisconectTimeout;
            }
        }
    }
}
