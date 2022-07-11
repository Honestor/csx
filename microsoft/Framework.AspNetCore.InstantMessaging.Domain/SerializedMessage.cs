using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public readonly struct SerializedMessage
    {
        /// <summary>
        /// 协议名称
        /// </summary>
        public string ProtocolName { get; }

        /// <summary>
        /// 序列化后的内容
        /// </summary>
        public ReadOnlyMemory<byte> Serialized { get; }

        public SerializedMessage(string protocolName, ReadOnlyMemory<byte> serialized)
        {
            ProtocolName = protocolName;
            Serialized = serialized;
        }
    }
}