namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class HandshakeRequestMessage : HubMessage
    {
        public HandshakeRequestMessage(string protocol, int version)
        {
            Protocol = protocol;
            Version = version;
        }

        /// <summary>
        /// –≠“È
        /// </summary>
        public string Protocol { get; }

        /// <summary>
        /// ∞Ê±æ
        /// </summary>
        public int Version { get; }
    }
}
