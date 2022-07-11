using Framework.AspNetCore.Connections.Abstractions;
using Framework.AspNetCore.InstantMessaging.Domain;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Framework.AspNetCore.InstantMessaging.Protocols.Json
{
    public sealed class JsonHubProtocol : IHubProtocol
    {
        private static JsonEncodedText TypePropertyNameBytes = JsonEncodedText.Encode(TypePropertyName);
        private const string TypePropertyName = "type";
        private const string ErrorPropertyName = "error";
        private static JsonEncodedText ErrorPropertyNameBytes = JsonEncodedText.Encode(ErrorPropertyName);
        private const string AllowReconnectPropertyName = "allowReconnect";
        private static JsonEncodedText AllowReconnectPropertyNameBytes = JsonEncodedText.Encode(AllowReconnectPropertyName);

        public string Name =>  "json";

        public int Version => 1;

        public TransferFormat TransferFormat =>  TransferFormat.Text;

        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            return HubProtocolExtensions.GetMessageBytes(this, message);
        }

        public bool IsVersionSupported(int version)
        {
            return version == Version;
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            WriteMessageCore(message, output);
            TextMessageFormatter.WriteRecordSeparator(output);
        }

        private void WriteMessageCore(HubMessage message, IBufferWriter<byte> stream)
        {
            var reusableWriter = ReusableUtf8JsonWriter.Get(stream);

            try
            {
                var writer = reusableWriter.GetJsonWriter();
                writer.WriteStartObject();
                switch (message)
                {
                    case PingMessage _:
                        WriteMessageType(writer, HubProtocolConstants.PingMessageType);
                        break;
                    case CloseMessage m:
                        WriteMessageType(writer, HubProtocolConstants.CloseMessageType);
                        WriteCloseMessage(m, writer);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported message type: {message.GetType().FullName}");
                }
                writer.WriteEndObject();
                writer.Flush();
                Debug.Assert(writer.CurrentDepth == 0);
            }
            finally
            {
                ReusableUtf8JsonWriter.Return(reusableWriter);
            }
        }

        /// <summary>
        /// 写入类型
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="type"></param>
        private static void WriteMessageType(Utf8JsonWriter writer, int type)
        {
            writer.WriteNumber(TypePropertyNameBytes, type);
        }

        /// <summary>
        /// 默认的Json序列化配置
        /// </summary>
        /// <returns></returns>
        internal static JsonSerializerOptions CreateDefaultSerializerSettings()
        {
            return new JsonSerializerOptions()
            {
                WriteIndented = false,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                IgnoreReadOnlyProperties = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                MaxDepth = 64,
                DictionaryKeyPolicy = null,
                DefaultBufferSize = 16 * 1024,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        }

        /// <summary>
        /// 写入关闭消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="writer"></param>
        private void WriteCloseMessage(CloseMessage message, Utf8JsonWriter writer)
        {
            if (message.Error != null)
            {
                writer.WriteString(ErrorPropertyNameBytes, message.Error);
            }

            if (message.AllowReconnect)
            {
                writer.WriteBoolean(AllowReconnectPropertyNameBytes, true);
            }
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            if (!TextMessageParser.TryParseMessage(ref input, out var payload))
            {
                message = null;
                return false;
            }

            message = ParseMessage(payload, binder);

            return message != null;
        }

        private HubMessage ParseMessage(ReadOnlySequence<byte> input, IInvocationBinder binder)
        {
            throw new NotImplementedException();
        }
    }
}
