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
        /// Э��
        /// </summary>
        public string Protocol { get; }

        /// <summary>
        /// �汾
        /// </summary>
        public int Version { get; }
    }
}
