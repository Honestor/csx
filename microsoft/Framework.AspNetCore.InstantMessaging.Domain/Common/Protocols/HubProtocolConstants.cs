namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public static class HubProtocolConstants
    {
        /// <summary>
        /// ������Ϣ����
        /// </summary>
        public const int InvocationMessageType = 1;

        /// <summary>
        /// ping ��Ϣ����
        /// </summary>
        public const int PingMessageType = 6;

        /// <summary>
        /// �ر���Ϣ����
        /// </summary>
        public const int CloseMessageType = 7;

        /// <summary>
        /// ��Ϣ���
        /// </summary>
        public const int CompletionMessageType = 3;
    }
}
