using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public readonly struct SerializedMessage
    {
        /// <summary>
        /// Э������
        /// </summary>
        public string ProtocolName { get; }

        /// <summary>
        /// ���л��������
        /// </summary>
        public ReadOnlyMemory<byte> Serialized { get; }

        public SerializedMessage(string protocolName, ReadOnlyMemory<byte> serialized)
        {
            ProtocolName = protocolName;
            Serialized = serialized;
        }
    }
}