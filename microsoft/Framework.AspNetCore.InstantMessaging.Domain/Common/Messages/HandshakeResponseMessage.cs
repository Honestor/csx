namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub������Ϣ
    /// </summary>
    public class HandshakeResponseMessage : HubMessage
    {
        public static readonly HandshakeResponseMessage Empty = new HandshakeResponseMessage(error: null);

        public string? Error { get; }

        public HandshakeResponseMessage(string? error)
        {
            Error = error;
        }
    }
}
