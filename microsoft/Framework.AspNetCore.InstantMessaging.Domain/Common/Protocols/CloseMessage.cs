namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub关闭消息
    /// </summary>
    public class CloseMessage : HubMessage
    {
        public static readonly CloseMessage Empty = new CloseMessage(error: null, allowReconnect: false);

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// 是否允许重连
        /// </summary>
        public bool AllowReconnect { get; }

        public CloseMessage(string? error, bool allowReconnect)
        {
            Error = error;
            AllowReconnect = allowReconnect;
        }
    }
}
