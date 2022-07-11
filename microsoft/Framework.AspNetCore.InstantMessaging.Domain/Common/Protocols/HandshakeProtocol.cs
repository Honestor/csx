using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public static class HandshakeProtocol
    {
        private static readonly ReadOnlyMemory<byte> _successHandshakeData;

        private const string ErrorPropertyName = "error";

        static HandshakeProtocol()
        {
            var memoryBufferWriter = MemoryBufferWriter.Get();
            try
            {
                WriteResponseMessage(HandshakeResponseMessage.Empty, memoryBufferWriter);
                _successHandshakeData = memoryBufferWriter.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(memoryBufferWriter);
            }
        }

        /// <summary>
        /// 获取成功握手信息
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> GetSuccessfulHandshake(IHubProtocol protocol) => _successHandshakeData.Span;

        /// <summary>
        /// 异常信息
        /// </summary>
        private static JsonEncodedText ErrorPropertyNameBytes = JsonEncodedText.Encode(ErrorPropertyName);

        /// <summary>
        /// 写入响应消息
        /// </summary>
        /// <param name="responseMessage"></param>
        /// <param name="output"></param>
        public static void WriteResponseMessage(HandshakeResponseMessage responseMessage, IBufferWriter<byte> output)
        {
            var reusableWriter = ReusableUtf8JsonWriter.Get(output);

            try
            {
                var writer = reusableWriter.GetJsonWriter();

                writer.WriteStartObject();
                if (!string.IsNullOrEmpty(responseMessage.Error))
                {
                    writer.WriteString(ErrorPropertyNameBytes, responseMessage.Error);
                }

                writer.WriteEndObject();
                writer.Flush();
                Debug.Assert(writer.CurrentDepth == 0);
            }
            finally
            {
                ReusableUtf8JsonWriter.Return(reusableWriter);
            }

            TextMessageFormatter.WriteRecordSeparator(output);
        }

        private const string ProtocolPropertyName = "protocol";
        private static JsonEncodedText ProtocolPropertyNameBytes = JsonEncodedText.Encode(ProtocolPropertyName);
        private const string ProtocolVersionPropertyName = "version";
        private static JsonEncodedText ProtocolVersionPropertyNameBytes = JsonEncodedText.Encode(ProtocolVersionPropertyName);
        public static bool TryParseRequestMessage(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out HandshakeRequestMessage? requestMessage)
        {
            if (!TextMessageParser.TryParseMessage(ref buffer, out var payload))
            {
                requestMessage = null;
                return false;
            }

            var reader = new Utf8JsonReader(payload, isFinalBlock: true, state: default);

            reader.CheckRead();
            reader.EnsureObjectStart();

            string? protocol = null;
            int? protocolVersion = null;

            while (reader.CheckRead())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (reader.ValueTextEquals(ProtocolPropertyNameBytes.EncodedUtf8Bytes))
                    {
                        protocol = reader.ReadAsString(ProtocolPropertyName);
                    }
                    else if (reader.ValueTextEquals(ProtocolVersionPropertyNameBytes.EncodedUtf8Bytes))
                    {
                        protocolVersion = reader.ReadAsInt32(ProtocolVersionPropertyName);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                else
                {
                    throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading handshake request JSON. Message content: {GetPayloadAsString()}");
                }
            }

            if (protocol == null)
            {
                throw new InvalidDataException($"Missing required property '{ProtocolPropertyName}'. Message content: {GetPayloadAsString()}");
            }
            if (protocolVersion == null)
            {
                throw new InvalidDataException($"Missing required property '{ProtocolVersionPropertyName}'. Message content: {GetPayloadAsString()}");
            }

            requestMessage = new HandshakeRequestMessage(protocol, protocolVersion.Value);

            //错误消息已文本显示
            string GetPayloadAsString()
            {
                // REVIEW: Should we show hex for binary charaters?
                return Encoding.UTF8.GetString(payload.ToArray());
            }

            return true;
        }
    }
}
