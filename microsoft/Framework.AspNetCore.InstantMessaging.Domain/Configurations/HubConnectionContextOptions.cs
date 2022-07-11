using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub ��������������
    /// </summary>
    public class HubConnectionContextOptions
    {
        /// <summary>
        /// �������ʱ��
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; }

        /// <summary>
        /// �ڷ������ر�����֮ǰ,�ͻ��˱��뷢��һ����Ϣ�ĳ�ʱʱ������
        /// </summary>
        public TimeSpan ClientTimeoutInterval { get; set; }

        /// <summary>
        /// �ͻ��˷�����Ϣ�����ֵ
        /// </summary>
        public long? MaximumReceiveMessageSize { get; set; }

        /// <summary>
        /// ģ��ʱ��Provider
        /// </summary>
        internal ISystemClock SystemClock { get; set; } = default!;

        /// <summary>
        /// ��ҳ���hub ����hub���� ͬһʱ�䷢��������Ϣ,���ֵ��1��ͬ��ִ��,�������1�����첽ִ��
        /// </summary>
        public int MaximumParallelInvocations { get; set; } = 1;
    }
}
