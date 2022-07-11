namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// keep alive消息，让连接的另一方知道连接仍处于活动状态。
    /// </summary>
    public class PingMessage : HubMessage
    {
        public static readonly PingMessage Instance = new PingMessage();

        private PingMessage()
        {
        }
    }
}
