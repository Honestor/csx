using System;
using System.Collections.Generic;

namespace Framework.AspNetCore.Connections
{
    public class WebSocketOptions
    {
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public Func<IList<string>, string>? SubProtocolSelector { get; set; }
    }
}
