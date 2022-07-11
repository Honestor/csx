namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub�ر���Ϣ
    /// </summary>
    public class CloseMessage : HubMessage
    {
        public static readonly CloseMessage Empty = new CloseMessage(error: null, allowReconnect: false);

        /// <summary>
        /// ������Ϣ
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// �Ƿ���������
        /// </summary>
        public bool AllowReconnect { get; }

        public CloseMessage(string? error, bool allowReconnect)
        {
            Error = error;
            AllowReconnect = allowReconnect;
        }
    }
}
