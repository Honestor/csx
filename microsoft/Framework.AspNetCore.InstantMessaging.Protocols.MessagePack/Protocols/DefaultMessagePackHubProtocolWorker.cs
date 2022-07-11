using MessagePack;
using System;
using System.IO;

namespace Framework.AspNetCore.InstantMessaging.Protocols.MessagePack
{
    internal sealed class DefaultMessagePackHubProtocolWorker : MessagePackHubProtocolWorker
    {
        private readonly MessagePackSerializerOptions _messagePackSerializerOptions;
        public DefaultMessagePackHubProtocolWorker(MessagePackSerializerOptions messagePackSerializerOptions)
        {
            _messagePackSerializerOptions = messagePackSerializerOptions;
        }

        protected override void Serialize(ref MessagePackWriter writer, Type type, object value)
        {
            MessagePackSerializer.Serialize(type, ref writer, value, _messagePackSerializerOptions);
        }

        protected override object DeserializeObject(ref MessagePackReader reader, Type type, string field)
        {
            try
            {
                return MessagePackSerializer.Deserialize(type, ref reader, _messagePackSerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Deserializing object of the `{type.Name}` type for '{field}' failed.", ex);
            }
        }
    }
}
