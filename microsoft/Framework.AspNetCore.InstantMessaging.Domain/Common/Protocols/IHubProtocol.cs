using Framework.AspNetCore.Connections.Abstractions;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Framework.AspNetCore.InstantMessaging.Domain
{

    public interface IHubProtocol
    {
        /// <summary>
        /// 协议名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 协议版本
        /// </summary>
        int Version { get; }

        /// <summary>
        /// 传输数据格式
        /// </summary>
        TransferFormat TransferFormat { get; }

        /// <summary>
        /// 版本是否支持
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        bool IsVersionSupported(int version);

        /// <summary>
        /// 获取消息的字节
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        ReadOnlyMemory<byte> GetMessageBytes(HubMessage message);

        /// <summary>
        /// 写入消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="output"></param>
        void WriteMessage(HubMessage message, IBufferWriter<byte> output);

        /// <summary>
        /// 转换消息
        /// </summary>
        /// <param name="input"></param>
        /// <param name="binder"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage message);
    }
}
