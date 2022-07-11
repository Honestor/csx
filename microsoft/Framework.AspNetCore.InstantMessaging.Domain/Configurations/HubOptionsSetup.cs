using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class HubOptionsSetup : IConfigureOptions<HubOptions>
    {
        /// <summary>
        /// Ĭ�����ֳ�ʱʱ��
        /// </summary>
        internal static TimeSpan DefaultHandshakeTimeout => TimeSpan.FromSeconds(15);

        /// <summary>
        /// Ĭ���������ʱ��
        /// </summary>
        internal static TimeSpan DefaultKeepAliveInterval => TimeSpan.FromSeconds(15);

        /// <summary>
        /// �ڷ������ر�����֮ǰ�ͻ��˱��뷢����Ϣȷ��,��ʱʱ��Ĭ��λ30��
        /// </summary>
        internal static TimeSpan DefaultClientTimeoutInterval => TimeSpan.FromSeconds(30);

        /// <summary>
        /// ����Hub�������Ϣ���ֵĬ��32kb
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

