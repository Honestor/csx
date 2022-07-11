using Framework.AspNetCore.Connections.Common;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace Framework.AspNetCore.Connections
{
    public class HttpConnectionDispatcherOptions
    {
        /// <summary>
        /// 认证数据
        /// </summary>
        public IList<IAuthorizeData> AuthorizationData { get; }

        /// <summary>
        /// 最小版本号
        /// </summary>
        public int MinimumProtocolVersion { get; set; } = 0;

        /// <summary>
        /// 传输最大的buffer size
        /// </summary>
        public long TransportMaxBufferSize { get; set; }

        /// <summary>
        /// 运输的类型
        /// </summary>
        public HttpTransportType Transports { get; set; }

        /// <summary>
        /// websocket设置
        /// </summary>
        public WebSocketOptions WebSockets { get; }

        /// <summary>
        /// 应用程序最大的buffer size
        /// </summary>
        public long ApplicationMaxBufferSize { get; set; }

        private const int DefaultPipeBufferSize = 32768;
        public HttpConnectionDispatcherOptions()
        {
            AuthorizationData = new List<IAuthorizeData>();
            TransportMaxBufferSize = DefaultPipeBufferSize;
            ApplicationMaxBufferSize = DefaultPipeBufferSize;

            Transports= HttpTransportType.WebSockets;
            WebSockets = new WebSocketOptions();
        }
    }
}
