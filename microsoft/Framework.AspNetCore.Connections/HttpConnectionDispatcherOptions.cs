using Framework.AspNetCore.Connections.Common;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace Framework.AspNetCore.Connections
{
    public class HttpConnectionDispatcherOptions
    {
        /// <summary>
        /// ��֤����
        /// </summary>
        public IList<IAuthorizeData> AuthorizationData { get; }

        /// <summary>
        /// ��С�汾��
        /// </summary>
        public int MinimumProtocolVersion { get; set; } = 0;

        /// <summary>
        /// ��������buffer size
        /// </summary>
        public long TransportMaxBufferSize { get; set; }

        /// <summary>
        /// ���������
        /// </summary>
        public HttpTransportType Transports { get; set; }

        /// <summary>
        /// websocket����
        /// </summary>
        public WebSocketOptions WebSockets { get; }

        /// <summary>
        /// Ӧ�ó�������buffer size
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
