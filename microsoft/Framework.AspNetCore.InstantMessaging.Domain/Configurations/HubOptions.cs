using System;
using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub����
    /// </summary>
    public class HubOptions
    {
        /// <summary>
        /// ֧�ֵ�Э��
        /// </summary>
        public IList<string>? SupportedProtocols { get; set; }

        /// <summary>
        /// ���ֳ�ʱʱ��
        /// </summary>
        public TimeSpan? HandshakeTimeout { get; set; } = null;

        /// <summary>
        /// Hub�����������
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; } = null;

        /// <summary>
        /// �������ر�����֮ǰ,�ͻ��˱��뷢����Ϣ�ĳ�ʱʱ������
        /// </summary>
        public TimeSpan? ClientTimeoutInterval { get; set; } = null;

        /// <summary>
        /// ��ȡ�����õ�������Hub��Ϣ�������Ϣ��С��Ĭ��ֵΪ32KB
        /// </summary>
        public long? MaximumReceiveMessageSize { get; set; } = null;

        /// <summary>
        /// �Ƿ���ͻ��˷�����ϸ�Ĵ�����Ϣ
        /// </summary>
        public bool? EnableDetailedErrors { get; set; } = null;
    }
}
