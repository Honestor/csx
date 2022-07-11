using Framework.AspNetCore.Connections.Abstractions;
using Framework.AspNetCore.InstantMessaging.Domain;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Framework.AspNetCore.InstantMessaging.Protocols.MessagePack
{
    /// <summary>
    /// MessagePack 二进制协议
    /// </summary>
    public class MessagePackHubProtocol : IHubProtocol
    {
        public int Version => 1;

        public string Name => "messagepack";

        public TransferFormat TransferFormat => TransferFormat.Binary;

        private readonly DefaultMessagePackHubProtocolWorker _worker;

        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message) => _worker.GetMessageBytes(message);

        public MessagePackHubProtocol():this(Options.Create(new MessagePackHubProtocolOptions()))
        { 
            
        }

        public MessagePackHubProtocol(IOptions<MessagePackHubProtocolOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _worker = new DefaultMessagePackHubProtocolWorker(options.Value.SerializerOptions);
        }

        /// <summary>
        /// 判断版本是否支持
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public bool IsVersionSupported(int version)
        {
            return version == Version;
        }

        /// <summary>
        /// 写入消息到客户端
        /// </summary>
        /// <param name="message"></param>
        /// <param name="output"></param>
        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
          => _worker.WriteMessage(message, output);

        internal static MessagePackSerializerOptions CreateDefaultMessagePackSerializerOptions() =>
          MessagePackSerializerOptions
              .Standard
              .WithResolver(InstantMessagingResolver.Instance)
              .WithSecurity(MessagePackSecurity.UntrustedData);

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
             => _worker.TryParseMessage(ref input, binder, out message);

        internal class InstantMessagingResolver : IFormatterResolver
        {
            public static readonly IFormatterResolver Instance = new InstantMessagingResolver();

            public static readonly IReadOnlyList<IFormatterResolver> Resolvers = new IFormatterResolver[]
            {
                DynamicEnumAsStringResolver.Instance,
                ContractlessStandardResolver.Instance,
            };

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                return Cache<T>.Formatter;
            }

            private static class Cache<T>
            {
                public static readonly IMessagePackFormatter<T> Formatter;

                static Cache()
                {
                    foreach (var resolver in Resolvers)
                    {
                        Formatter = resolver.GetFormatter<T>();
                        if (Formatter != null)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}
