using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class HubOptionsSetup : IConfigureOptions<HubOptions>
    {
        /// <summary>
        /// 默认握手超时时间
        /// </summary>
        internal static TimeSpan DefaultHandshakeTimeout => TimeSpan.FromSeconds(15);

        /// <summary>
        /// 默认心跳检测时间
        /// </summary>
        internal static TimeSpan DefaultKeepAliveInterval => TimeSpan.FromSeconds(15);

        /// <summary>
        /// 在服务器关闭连接之前客户端必须发送消息确认,超时时间默认位30秒
        /// </summary>
        internal static TimeSpan DefaultClientTimeoutInterval => TimeSpan.FromSeconds(30);

        /// <summary>
        /// 单个Hub传入的消息最大值默认32kb
        /// </summary>
        internal const int DefaultMaximumMessageSize = 32 * 1024;

        private readonly List<string> _defaultProtocols = new List<string>();


        public HubOptionsSetup(IEnumerable<IHubProtocol> protocols)
        {
            foreach (var hubProtocol in protocols)
            {
                if (hubProtocol.GetType().CustomAttributes.Where(a => a.AttributeType.FullName == "Microsoft.AspNetCore.SignalR.Internal.NonDefaultHubProtocolAttribute").Any())
                {
                    continue;
                }
                _defaultProtocols.Add(hubProtocol.Name);
            }
        }
        public void Configure(HubOptions options)
        {
            if (options.KeepAliveInterval == null)
            {
                options.KeepAliveInterval = DefaultKeepAliveInterval;
            }

            if (options.HandshakeTimeout == null)
            {
                options.HandshakeTimeout = DefaultHandshakeTimeout;
            }

            if (options.MaximumReceiveMessageSize == null)
            {
                options.MaximumReceiveMessageSize = DefaultMaximumMessageSize;
            }

            if (options.SupportedProtocols == null)
            {
                options.SupportedProtocols = new List<string>(_defaultProtocols.Count);
            }


            foreach (var protocol in _defaultProtocols)
            {
                options.SupportedProtocols.Add(protocol);
            }
        }
    }
}

