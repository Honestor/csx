namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public static class HubProtocolConstants
    {
        /// <summary>
        /// 调用信息类型
        /// </summary>
        public const int InvocationMessageType = 1;

        /// <summary>
        /// ping 消息类型
        /// </summary>
        public const int PingMessageType = 6;

        /// <summary>
        /// 关闭消息类型
        /// </summary>
        public const int CloseMessageType = 7;

        /// <summary>
        /// 消息完成
        /// </summary>
        public const int CompletionMessageType = 3;
    }
}
