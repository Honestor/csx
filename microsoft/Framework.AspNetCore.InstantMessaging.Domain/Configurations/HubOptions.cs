using System;
using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub配置
    /// </summary>
    public class HubOptions
    {
        /// <summary>
        /// 支持的协议
        /// </summary>
        public IList<string>? SupportedProtocols { get; set; }

        /// <summary>
        /// 握手超时时间
        /// </summary>
        public TimeSpan? HandshakeTimeout { get; set; } = null;

        /// <summary>
        /// Hub心跳检测配置
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; } = null;

        /// <summary>
        /// 服务器关闭连接之前,客户端必须发送消息的超时时间设置
        /// </summary>
        public TimeSpan? ClientTimeoutInterval { get; set; } = null;

        /// <summary>
        /// 获取或设置单个传入Hub消息的最大消息大小。默认值为32KB
        /// </summary>
        public long? MaximumReceiveMessageSize { get; set; } = null;

        /// <summary>
        /// 是否向客户端发送详细的错误消息
        /// </summary>
        public bool? EnableDetailedErrors { get; set; } = null;
    }
}
