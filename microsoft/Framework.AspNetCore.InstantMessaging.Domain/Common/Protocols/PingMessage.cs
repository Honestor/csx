namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// keep alive��Ϣ�������ӵ���һ��֪�������Դ��ڻ״̬��
    /// </summary>
    public class PingMessage : HubMessage
    {
        public static readonly PingMessage Instance = new PingMessage();

        private PingMessage()
        {
        }
    }
}
