using System;

namespace Framework.AspNetCore.Connections.Common
{

    [Flags]
    public enum HttpTransportType
    {
        
        None = 0,
        /// <summary>
        /// ws
        /// </summary>
        WebSockets = 1,
        /// <summary>
        /// sse
        /// </summary>
        ServerSentEvents = 2,
        /// <summary>
        /// ³¤ÂÖÑ¯
        /// </summary>
        LongPolling = 4,
    }
}
